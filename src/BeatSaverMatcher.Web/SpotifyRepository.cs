using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class SpotifyRepository
    {
        private readonly SpotifyClient _spotifyClient;

        public SpotifyRepository(IOptions<SpotifyConfiguration> options)
        {
            var spotifyConfig = options.Value;

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyConfig.ClientId, spotifyConfig.ClientSecret));

            _spotifyClient = new SpotifyClient(config);
        }

        public async Task<FullPlaylist> GetPlaylist(string playlistId)
        {
            return await _spotifyClient.Playlists.Get(playlistId);
        }

        public async Task<IList<FullTrack>> GetTracksForPlaylist(string playlistId)
        {
            var toReturn = new List<FullTrack>();

            Paging<PlaylistTrack<IPlayableItem>> page;
            var offset = 0;
            do
            {
                PlaylistGetItemsRequest request = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track)
                {
                    Offset = offset
                };
                page = await _spotifyClient.Playlists.GetItems(playlistId, request);
                offset += page.Limit;
                toReturn.AddRange(page.Items.Select(x => (FullTrack)x.Track));
            } while (toReturn.Count < page.Total);

            return toReturn;
        }
    }
}
