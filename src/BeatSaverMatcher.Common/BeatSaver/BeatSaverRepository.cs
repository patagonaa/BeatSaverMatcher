using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverRepository
    {
        private readonly ILogger<BeatSaverRepository> _logger;

        public BeatSaverRepository(ILogger<BeatSaverRepository> logger)
        {
            _logger = logger;
        }

        public async Task<int> GetLatestKey()
        {
            return await DoWithRetries(async () =>
            {
                var request = WebRequest.CreateHttp($"https://beatsaver.com/api/maps/latest/0?automapper=1");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
                request.Headers.Add("sec-fetch-mode", "navigate");
                request.Headers.Add("sec-fetch-user", "?1");
                request.Headers.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

                BeatSaverSongPage page;
                var response = (HttpWebResponse)await request.GetResponseAsync();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    page = JsonConvert.DeserializeObject<BeatSaverSongPage>(sr.ReadToEnd());
                }

                return int.Parse(page.Docs[0].Key, NumberStyles.HexNumber);
            });
        }

        public async Task<BeatSaverSong> GetSong(int key)
        {
            return await DoWithRetries(async () =>
            {
                try
                {
                    var request = WebRequest.CreateHttp($"https://beatsaver.com/api/maps/detail/{key:x}");
                    request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
                    request.Headers.Add("sec-fetch-mode", "navigate");
                    request.Headers.Add("sec-fetch-user", "?1");
                    request.Headers.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

                    BeatSaverSong song;
                    var response = (HttpWebResponse)await request.GetResponseAsync();
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        song = JsonConvert.DeserializeObject<BeatSaverSong>(sr.ReadToEnd());
                    }
                    return song;
                }
                catch (WebException wex)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response == null)
                        throw;

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    throw;
                }
            });
        }

        private async Task<T> DoWithRetries<T>(Func<Task<T>> action)
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
                        await Task.Delay(timeout);
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
