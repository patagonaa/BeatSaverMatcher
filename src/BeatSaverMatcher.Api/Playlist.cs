using System.Collections.Generic;

namespace BeatSaverMatcher.Api;

public record Playlist(string Id, string? Name, string? OwnerName, IReadOnlyList<PlaylistImage> Images);

public record PlaylistImage(int Width, int Height, string ImageUrl);