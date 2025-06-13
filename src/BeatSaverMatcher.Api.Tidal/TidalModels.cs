using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BeatSaverMatcher.Api.Tidal;

internal class TidalResponse<TData>
{
    public TData? Data { get; set; }
    public Dictionary<string, string>? Links { get; set; }
    public IList<JsonNode>? Included { get; set; }
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

internal class TidalErrorResponse
{
    public IList<TidalError> Errors { get; set; }
}

internal class TidalError
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string Code { get; set; }
    public string Detail { get; set; }
    //...
}

internal class TidalData
{
    public string? Id { get; set; }
    public string? Type { get; set; }
}

internal class TidalPlaylistData : TidalData
{
    public TidalPlaylistAttributes? Attributes { get; set; }
    public TidalPlaylistRelationships? Relationships { get; set; }
}
internal class TidalArtworkData : TidalData
{
    public TidalArtworkAttributes? Attributes { get; set; }
}

internal class TidalTrackData : TidalData
{
    public TidalTrackAttributes? Attributes { get; set; }
    public TidalTrackRelationships? Relationships { get; set; }
}

internal class TidalTrackRelationships
{
    public TidalRelationship<IList<TidalTrackArtistRelationshipData>>? Artists { get; set; }
}

internal class TidalTrackArtistRelationshipData : TidalData
{
}

internal class TidalTrackAttributes
{
    public string? Title { get; set; }
}

internal class TidalArtistData : TidalData
{
    public TidalArtistAttributes? Attributes { get; set; }
}

public class TidalArtistAttributes
{
    public string? Name { get; set; }
}

internal class TidalPlaylistRelationships
{
    public TidalRelationship<TidalPlaylistCoverArtRelationshipData>? CoverArt { get; set; }
}

internal class TidalRelationship<TData>
{
    public Dictionary<string, string>? Links { get; set; }
    public TData? Data { get; set; }
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
    public string? Name { get; set; }
}

internal class TidalArtworkAttributes
{
    public string? MediaType { get; set; }
    public IList<TidalArtworkAttributesFile>? Files { get; set; }
}
internal class TidalArtworkAttributesFile
{
    public string? Href { get; set; }
    public TidalArtworkAttributesFileMeta? Meta { get; set; }
}
internal class TidalArtworkAttributesFileMeta
{
    public int? Width { get; set; }
    public int? Height { get; set; }
}