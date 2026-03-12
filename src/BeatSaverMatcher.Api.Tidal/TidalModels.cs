using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BeatSaverMatcher.Api.Tidal;

internal class TidalResponse<TData>
{
    public TData? Data { get; init; }
    public TidalLinks? Links { get; init; }
    public IList<JsonNode>? Included { get; init; }
    public Dictionary<string, T> GetIncluded<T>(string type)
        where T : class
    {
        return Included?
            .Where(x => x["type"]?.GetValue<string>() == type && x["id"] != null)
            .ToDictionary(
                x => x["id"]!.GetValue<string>(),
                x => x.Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!) ?? [];
    }
}

internal class TidalLinks
{
    public string? Self { get; init; }
    public string? Next { get; init; }
    // Meta
}

internal class TidalErrorResponse
{
    public IList<TidalError>? Errors { get; init; }
}

internal class TidalError
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public string? Code { get; init; }
    public string? Detail { get; init; }
    //...
}

internal class TidalData
{
    public string? Id { get; init; }
    public string? Type { get; init; }
}

internal class TidalPlaylistData : TidalData
{
    public TidalPlaylistAttributes? Attributes { get; init; }
    public TidalPlaylistRelationships? Relationships { get; init; }
}
internal class TidalArtworkData : TidalData
{
    public TidalArtworkAttributes? Attributes { get; init; }
}

internal class TidalTrackData : TidalData
{
    public TidalTrackAttributes? Attributes { get; init; }
    public TidalTrackRelationships? Relationships { get; init; }
}

internal class TidalTrackRelationships
{
    public TidalRelationship<IList<TidalTrackArtistRelationshipData>>? Artists { get; init; }
}

internal class TidalTrackArtistRelationshipData : TidalData
{
}

internal class TidalTrackAttributes
{
    public string? Title { get; init; }
}

internal class TidalArtistData : TidalData
{
    public TidalArtistAttributes? Attributes { get; init; }
}

public class TidalArtistAttributes
{
    public string? Name { get; init; }
}

internal class TidalPlaylistRelationships
{
    public TidalRelationship<TidalPlaylistCoverArtRelationshipData>? CoverArt { get; init; }
}

internal class TidalRelationship<TData>
{
    public TidalLinks? Links { get; init; }
    public TData? Data { get; init; }
}
internal class TidalPlaylistCoverArtRelationshipData : TidalData
{
}

internal class TidalPlaylistItemRelationshipData : TidalData
{
    // Meta
}

internal class TidalPlaylistAttributes
{
    public string? Name { get; init; }
}

internal class TidalArtworkAttributes
{
    public string? MediaType { get; init; }
    public IList<TidalArtworkAttributesFile>? Files { get; init; }
}
internal class TidalArtworkAttributesFile
{
    public string? Href { get; init; }
    public TidalArtworkAttributesFileMeta? Meta { get; init; }
}
internal class TidalArtworkAttributesFileMeta
{
    public int? Width { get; init; }
    public int? Height { get; init; }
}