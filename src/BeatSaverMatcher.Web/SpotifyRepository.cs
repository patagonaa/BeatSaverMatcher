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

            var request = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);
            var firstPage = await _spotifyClient.Playlists.GetItems(playlistId, request);
            await foreach (var playlistTrack in _spotifyClient.Paginate(firstPage))
            {
                toReturn.Add((FullTrack)playlistTrack.Track);
            }
            return toReturn;
        }
    }
}
