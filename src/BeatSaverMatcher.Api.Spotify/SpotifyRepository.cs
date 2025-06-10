using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Api.Spotify
{
    public class SpotifyRepository : IMusicServiceApi
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

        public async Task<Playlist> GetPlaylist(string playlistId, CancellationToken cancellationToken)
        {
            var spotifyPlaylist = await _spotifyClient.Playlists.Get(playlistId, cancellationToken);
            return new Playlist(
                spotifyPlaylist.Id ?? throw new Exception("Missing ID of playlist reponse"),
                spotifyPlaylist.Name,
                spotifyPlaylist.Owner?.DisplayName,
                spotifyPlaylist.Images?.LastOrDefault(x => Math.Max(x.Width, x.Height) >= 256)?.Url);
        }

        public async Task<IList<PlaylistSong>> GetTracksForPlaylist(string playlistId, Action<int, int> progress, CancellationToken cancellationToken)
        {
            var toReturn = new List<PlaylistSong>();

            var request = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);

            try
            {
                var currentPage = await _spotifyClient.Playlists.GetItems(playlistId, request, cancellationToken);
                while (currentPage?.Items != null)
                {
                    if (currentPage.Offset.HasValue && currentPage.Total.HasValue)
                    {
                        progress(currentPage.Offset.Value + currentPage.Items.Count, currentPage.Total.Value);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    toReturn.AddRange(currentPage.Items.Select(x => x.Track).OfType<FullTrack>().Select(MapSong)); // OfType instead of Cast because there may be Episodes, etc.
                    currentPage = currentPage.Next != null ? await _spotifyClient.NextPage(currentPage) : null;
                }
            }
            catch (SpotifyAPI.Web.APIException aex)
            {
                throw new APIException(aex.Message, aex);
            }

            return toReturn;
        }

        private PlaylistSong MapSong(FullTrack track)
        {
            return new PlaylistSong(track.Name, track.Artists.Select(x => x.Name).ToList());
        }
    }
}
