using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Api;

public interface IMusicServiceApi
{
    Task<Playlist> GetPlaylist(string playlistId, CancellationToken cancellationToken);
    Task<IList<PlaylistSong>> GetTracksForPlaylist(string playlistId, Action<int, int> progress, CancellationToken cancellationToken);
}
