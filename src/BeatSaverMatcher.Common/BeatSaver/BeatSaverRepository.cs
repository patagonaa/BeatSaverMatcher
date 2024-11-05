using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverRepository
    {
        private readonly ILogger<BeatSaverRepository> _logger;
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
        }

        public async Task<int> GetLatestKey(CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                var request = WebRequest.CreateHttp($"https://beatsaver.com/api/search/text/0?sortOrder=Latest&automapper=true");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
                request.Headers.Add("sec-fetch-mode", "navigate");
                request.Headers.Add("sec-fetch-user", "?1");
                request.Headers.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

                BeatSaverSongPage page;
                var response = (HttpWebResponse)await request.GetResponseAsync();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    page = JsonSerializer.Deserialize<BeatSaverSongPage>(sr.ReadToEnd(), _beatSaverSerializerOptions);
                }

                return int.Parse(page.Docs[0].Id, NumberStyles.HexNumber);
            }, token);
        }

        public async Task<BeatSaverSong> GetSong(int key, CancellationToken token)
        {
            return await DoWithRetries(async () =>
            {
                try
                {
                    var request = WebRequest.CreateHttp($"https://beatsaver.com/api/maps/id/{key:x}");
                    request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
                    request.Headers.Add("sec-fetch-mode", "navigate");
                    request.Headers.Add("sec-fetch-user", "?1");
                    request.Headers.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

                    BeatSaverSong song;
                    var response = (HttpWebResponse)await request.GetResponseAsync();
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        song = JsonSerializer.Deserialize<BeatSaverSong>(sr.ReadToEnd(), _beatSaverSerializerOptions);
                    }
                    return song;
                }
                catch (WebException wex)
                {
                    if (!(wex.Response is HttpWebResponse response))
                        throw;

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    throw;
                }
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
                catch (WebException wex)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response == null)
                        throw;

                    tries++;

                    if ((int)response.StatusCode == 429) // Too Many Requests
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            var allHeaders = Environment.NewLine + string.Join(Environment.NewLine, response.Headers.AllKeys.Select(headerKey => $"{headerKey}: {response.Headers[headerKey]}"));
                            _logger.LogDebug("Error 429 Too Many Requests. Headers: {Headers}", allHeaders);
                        }

                        var timeout = TimeSpan.Zero;
                        if (response.Headers.AllKeys.Contains("Rate-Limit-Reset") && int.TryParse(response.Headers["Rate-Limit-Reset"], out int epochReset))
                        {
                            var resetTime = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochReset);
                            var delayTime = resetTime - DateTime.UtcNow;
                            if (delayTime > timeout)
                            {
                                timeout = delayTime;
                            }
                        }
                        else
                        {
                            timeout = TimeSpan.FromSeconds(9);
                        }

                        timeout += TimeSpan.FromSeconds(1); // always wait a minimum of one second to account for time tolerances
                        _logger.LogInformation("Rate Limit Reached. Waiting {Milliseconds} ms", timeout.TotalMilliseconds);
                        await Task.Delay(timeout, token);
                    }
                    else
                    {
                        _logger.LogError(wex, "Error while fetching.");
                        throw;
                    }

                    if (tries > 10)
                        throw;
                }
            }
        }
    }
}
