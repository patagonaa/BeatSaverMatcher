using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public sealed class BeatSaverRepository : IDisposable
    {
        private readonly ILogger<BeatSaverRepository> _logger;
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _beatSaverSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public BeatSaverRepository(ILogger<BeatSaverRepository> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://beatsaver.com/api")
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeatSaverMatcher/1.0 (https://github.com/patagonaa/BeatSaverMatcher)");
        }

        public async Task<BeatSaverSong> GetSong(int key, CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                using var response = await _httpClient.GetAsync($"maps/id/{key:x}", token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var song = await response.Content.ReadFromJsonAsync<BeatSaverSong>(_beatSaverSerializerOptions, token);
                return song;
            }, token);
        }

        public async Task<IList<BeatSaverSong>> GetSongsUpdatedAfter(DateTime lastUpdatedAt, CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                var url = $"maps/latest?sort=UPDATED&automapper=true&pageSize=100&after={HttpUtility.UrlEncode(DateTime.SpecifyKind(lastUpdatedAt, DateTimeKind.Utc).ToString("o"))}";
                
                using var response = await _httpClient.GetAsync(url, token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var page = await response.Content.ReadFromJsonAsync<BeatSaverSongSearchResponse>(_beatSaverSerializerOptions, token);
                return page.Docs;
            }, token);
        }

        public async Task<IList<BeatSaverDeletedSong>> GetSongsDeletedAfter(DateTime lastUpdatedAt, CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                var url = $"maps/deleted?pageSize=100&after={HttpUtility.UrlEncode(DateTime.SpecifyKind(lastUpdatedAt, DateTimeKind.Utc).ToString("o"))}";
                using var response = await _httpClient.GetAsync(url, token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var page = await response.Content.ReadFromJsonAsync<BeatSaverSongDeletedResponse>(_beatSaverSerializerOptions, token);
                return page.Docs;
            }, token);
        }

        public async Task<IList<BeatSaverScore>> GetScoresAfter(DateTime lastUpdatedAt, CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                var url = $"vote?since={HttpUtility.UrlEncode(DateTime.SpecifyKind(lastUpdatedAt, DateTimeKind.Utc).ToString("o"))}";
                
                using var response = await _httpClient.GetAsync(url, token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<IList<BeatSaverScore>>(_beatSaverSerializerOptions, token);
                return result;
            }, token);
        }

        private async Task<T> DoWithRetries<T>(Func<Task<T>> action, CancellationToken token)
        {
            int tries = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (HttpRequestException hex)
                {
                    tries++;

                    if (hex.StatusCode == HttpStatusCode.TooManyRequests) // Too Many Requests
                    {
                        var timeout = TimeSpan.FromSeconds(1);
                        _logger.LogInformation("Rate Limit Reached. Waiting {Milliseconds} ms", timeout.TotalMilliseconds);
                        await Task.Delay(timeout, token);
                    }
                    else
                    {
                        _logger.LogError(hex, "Error while fetching.");
                        throw;
                    }

                    if (tries > 10)
                        throw;
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
