using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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

        public async Task<IList<FullTrack>> GetTracksForPlaylist(string playlistId, Action<int, int> progress, CancellationToken cancellationToken)
        {
            var toReturn = new List<FullTrack>();

            var request = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);

            var currentPage = await _spotifyClient.Playlists.GetItems(playlistId, request);
            while (currentPage != null)
            {
                if (currentPage.Offset.HasValue && currentPage.Total.HasValue)
                {
                    progress(currentPage.Offset.Value + currentPage.Items.Count, currentPage.Total.Value);
                }
                cancellationToken.ThrowIfCancellationRequested();
                toReturn.AddRange(currentPage.Items.Select(x => x.Track).Cast<FullTrack>());
                currentPage = currentPage.Next != null ? await _spotifyClient.NextPage(currentPage) : null;
            }

            return toReturn;
        }
    }
}
