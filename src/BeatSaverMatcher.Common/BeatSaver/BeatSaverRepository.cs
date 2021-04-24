using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverRepository
    {
        public async Task<int> GetLatestKey()
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
        }

        public async Task<BeatSaverSong> GetSong(int key)
        {
            var request = WebRequest.CreateHttp($"https://beatsaver.com/api/maps/detail/{key:x}");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
            request.Headers.Add("sec-fetch-mode", "navigate");
            request.Headers.Add("sec-fetch-user", "?1");
            request.Headers.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

            BeatSaverSong song;
            try
            {
                var response = (HttpWebResponse)await request.GetResponseAsync();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    song = JsonConvert.DeserializeObject<BeatSaverSong>(sr.ReadToEnd());
                }
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

            return song;
        }
    }
}
