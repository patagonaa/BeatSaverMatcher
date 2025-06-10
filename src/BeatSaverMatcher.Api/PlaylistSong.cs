using System.Collections.Generic;

namespace BeatSaverMatcher.Api;
public record PlaylistSong(string Name, IList<string> Artists);
