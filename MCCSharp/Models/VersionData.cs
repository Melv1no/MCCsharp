using Newtonsoft.Json;

namespace MCCsharp.Models;

public class VersionData
{
    [JsonProperty("items")]
    public List<Item> Items { get; set; }

    [JsonProperty("recipes")]
    public List<Recipe> Recipes { get; set; }

}