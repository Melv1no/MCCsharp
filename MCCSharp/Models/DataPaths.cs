using Newtonsoft.Json;

namespace MCCsharp.Models;

public class DataPaths : Dictionary<string, Dictionary<string, VersionPath>> { }

public class VersionPath
{
    [JsonProperty("recipes")]
    public string? Recipes { get; set; }

    [JsonProperty("items")]
    public string? Items { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }
}
