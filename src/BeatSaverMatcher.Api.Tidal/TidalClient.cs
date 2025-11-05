using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeatSaverMatcher.Api.Tidal;

public class TidalClient : IMusicServiceApi, IDisposable
{
    private readonly TidalRateLimitHandler _rateLimitHandler;
    private readonly ILogger<TidalClient> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _httpClient;

    private DateTime _tokenExpiry = DateTime.MinValue;

    public TidalClient(TidalRateLimitHandler rateLimitHandler, ILogger<TidalClient> logger, IOptions<TidalConfiguration> options)
    {
        _rateLimitHandler = rateLimitHandler;
        _logger = logger;
        var config = options.Value;
        _clientId = config.ClientId;
        _clientSecret = config.ClientSecret;

        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://openapi.tidal.com/v2/")
        };
    }

    private async Task Authenticate()
    {
        if (_tokenExpiry >= DateTime.UtcNow)
        {
            return;
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.tidal.com/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "grant_type", "client_credentials" } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>();

        if (responseJson == null)
            throw new Exception("Empty auth response");

        var tokenType = responseJson["token_type"]?.GetValue<string>();
        var expiresIn = responseJson["expires_in"]?.GetValue<int>();
        var token = responseJson["access_token"]?.GetValue<string>();

        if (tokenType != "Bearer" || expiresIn == null || token == null)
            throw new Exception("Invalid auth response");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn.Value);
    }

    public async Task<Playlist> GetPlaylist(string playlistId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(playlistId, out _))
            throw new APIException(message: "Invalid playlist ID!");

        await Authenticate();

        var elem = await GetWithRateLimit<TidalResponse<TidalPlaylistData>>(_httpClient, $"playlists/{playlistId}?countryCode=US&include=coverArt", cancellationToken);

        if (elem?.Data?.Attributes == null)
            throw new Exception("missing data from response!");

        var attributes = elem.Data.Attributes;
        var artworks = elem.GetIncluded<TidalArtworkData>("artworks");

        var coverArtRelationship = elem.Data.Relationships?.CoverArt?.Data;
        string? coverUrl = null;
        if (coverArtRelationship?.Id != null)
        {
            var includedArt = artworks.GetValueOrDefault(coverArtRelationship.Id)?.Attributes;
            coverUrl = includedArt?.Files?.LastOrDefault(x => Math.Max(x.Meta?.Width ?? 0, x.Meta?.Height ?? 0) >= 256)?.Href;
        }

        return new Playlist(elem.Data.Id ?? throw new Exception("missing ID"), attributes.Name, "???", coverUrl);
    }

    public async Task<IList<PlaylistSong>> GetTracksForPlaylist(string playlistId, Action<int, int?> progress, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(playlistId, out _))
            throw new APIException(message: "Invalid playlist ID!");

        await Authenticate();

        var toReturn = new List<PlaylistSong>();

        var nextUri = $"playlists/{playlistId}/relationships/items?countryCode=US&include=items.artists";

        while (nextUri != null)
        {
            // including items here is useless because those don't include the artist relationship data which we need.
            var itemsResponse = await GetWithRateLimit<TidalResponse<IList<TidalPlaylistItemRelationshipData>>>(_httpClient, nextUri, cancellationToken);

            if (itemsResponse?.Data == null)
                throw new Exception("missing data from response!");

            nextUri = itemsResponse.Links?.Next?.TrimStart('/');

            var tracks = itemsResponse.GetIncluded<TidalTrackData>("tracks");
            var artists = itemsResponse.GetIncluded<TidalArtistData>("artists");

            foreach (var item in itemsResponse.Data)
            {
                var track = tracks.GetValueOrDefault(item.Id ?? throw new Exception("missing ID"));
                if (track == null)
                    continue; // missing / deleted track
                if (track?.Attributes?.Title == null || track?.Relationships?.Artists?.Data == null)
                    throw new Exception("missing data from track!");
                var trackArtists = new List<string>();
                foreach (var artistRel in track.Relationships.Artists.Data)
                {
                    var artist = artists?.GetValueOrDefault(artistRel.Id ?? throw new Exception("missing ID"));
                    if (artist == null)
                        continue; // missing artist
                    if (artist.Attributes?.Name == null)
                        throw new Exception("missing data from artist!");
                    trackArtists.Add(artist.Attributes.Name);
                }
                toReturn.Add(new PlaylistSong(track.Attributes.Title, trackArtists));
            }
            progress(toReturn.Count, null);
        }

        return toReturn;
    }

    private async Task<TResult?> GetWithRateLimit<TResult>(HttpClient httpClient, string uri, CancellationToken token)
    {
        int tries = 0;
        int? requiredTokens = null;
        while (true)
        {
            tries++;
            if (tries > 10)
                throw new Exception("Too Many Retries");

            await _rateLimitHandler.WaitForTokens(requiredTokens, token);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            foreach (var header in httpClient.DefaultRequestHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
            var response = await httpClient.SendAsync(request, token);

            if (
                response.Headers.TryGetValues("X-RateLimit-Remaining", out var limitRemaining) &&
                response.Headers.TryGetValues("X-RateLimit-Burst-Capacity", out var burstCapacity) &&
                response.Headers.TryGetValues("X-RateLimit-Replenish-Rate", out var replenishRate) &&
                response.Headers.TryGetValues("X-RateLimit-Requested-Tokens", out var requestedTokens)
            )
            {
                requiredTokens = int.Parse(requestedTokens.First());
                _rateLimitHandler.Update(int.Parse(limitRemaining.First()), int.Parse(burstCapacity.First()), int.Parse(replenishRate.First()), requiredTokens.Value);
            }

            if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.MultipleChoices)
            {
#if DEBUG
                var content = await response.Content.ReadAsStringAsync(token);

                return JsonSerializer.Deserialize<TResult>(content, JsonSerializerOptions.Web);
#else
                return await response.Content.ReadFromJsonAsync<TResult>(token)!;
#endif
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogInformation("Hit Tidal rate limit");
                continue;
            }
            else if (response.StatusCode >= HttpStatusCode.InternalServerError)
            {
                _logger.LogWarning("Status 500 while fetching from Tidal");
                await Task.Delay(10000, token);
            }
            else if (response.StatusCode >= HttpStatusCode.BadRequest)
            {
                string? message = null;
                Exception? exception = null;
                try
                {
                    var error = await response.Content.ReadFromJsonAsync<TidalErrorResponse>(token);
                    if (error?.Errors?.Any(x => x.Detail != null) ?? false)
                    {
                        message = string.Join(", ", error.Errors.Select(x => x.Detail));
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                throw new APIException(response.StatusCode, message, exception);
            }
            else
            {
                response.EnsureSuccessStatusCode(); // should throw
                throw new Exception($"Unknown Status {response.StatusCode}");
            }
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class TidalRateLimitHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<TidalRateLimitHandler> _logger;
    private (DateTime LastUpdate, int LastTokenCount, int MaxTokenCount, int ReplenishRate)? _state = null;
    private int? _minimumRequestTokens;

    public TidalRateLimitHandler(ILogger<TidalRateLimitHandler> logger)
    {
        _logger = logger;
    }

    public void Update(int remainingTokens, int burstCapacity, int replenishRate, int requestedTokens)
    {
        if (replenishRate <= 0)
            throw new ArgumentException("ReplenishRate must be greater than 0", nameof(replenishRate));
        _semaphore.Wait();
        try
        {
            _state = (DateTime.UtcNow, remainingTokens, burstCapacity, replenishRate);
            if (_minimumRequestTokens == null || requestedTokens < _minimumRequestTokens)
            {
                _minimumRequestTokens = requestedTokens;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WaitForTokens(int? requestedTokensHint, CancellationToken token)
    {
        var requestedTokens = requestedTokensHint ?? _minimumRequestTokens ?? 0;

        if (_state.HasValue && requestedTokens > _state.Value.MaxTokenCount)
            throw new Exception("may not request more tokens than maximum");
        while (true)
        {
            TimeSpan waitTime;
            await _semaphore.WaitAsync(token);
            try
            {
                if (_state == null)
                    return;

                var state = _state.Value;
                var now = DateTime.UtcNow;

                var elapsedSeconds = (int)(now - state.LastUpdate).TotalSeconds;
                var currentTokens = Math.Min(state.LastTokenCount + elapsedSeconds * state.ReplenishRate, state.MaxTokenCount);

                if (requestedTokens <= currentTokens)
                {
                    _state = (now, currentTokens - requestedTokens, state.MaxTokenCount, state.ReplenishRate);
                    return;
                }
                else
                {
                    waitTime = TimeSpan.FromSeconds(Math.Ceiling((requestedTokens - currentTokens) / (double)state.ReplenishRate));
                    _logger.LogInformation("Waiting for Tidal rate limit, waiting {WaitTime}s to replenish ({CurrentTokens} / {RequestedTokens} tokens)", (int)waitTime.TotalSeconds, currentTokens, requestedTokensHint?.ToString() ?? "?");
                }
            }
            finally
            {
                _semaphore.Release();
            }
            await Task.Delay(waitTime, token);
        }

    }
}
