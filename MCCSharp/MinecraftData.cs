using MCCsharp.Enums;
using MCCsharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCCsharp;

public class MinecraftData
{
    private readonly string version;
    private readonly Platform platform;

    private readonly Dictionary<int, Item> itemsById = new();
    private readonly Dictionary<string, Item> itemsByName = new();
    private List<JObject> recipesRaw = new();
    private static DataPaths? dataPaths;
    private List<Item> allItems = new();
    private List<Recipe> allRecipes = new();

    public MinecraftData(Platform platform, string version)
    {
        this.platform = platform;
        this.version = version;
        LoadItems();
        LoadRecipes();
    }

    public List<Item> Items => allItems;
    public List<Recipe> Recipes => allRecipes;

    public List<Item> SearchItems(string query, int limit = 10)
    {
        return allItems
            .Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    public string GetFormattedRecipe(Recipe recipe)
    {
        var counts = new Dictionary<string, int>();
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var item = recipe.Matrix[y, x];
                if (item == null) continue;
                if (!counts.ContainsKey(item.Name))
                    counts[item.Name] = 0;
                counts[item.Name]++;
            }
        }
        return string.Join(" ", counts.Select(kv => $"{kv.Value}x{kv.Key}"));
    }

    public static Dictionary<string, List<(string version, bool hasRecipes)>> GetAvailableVersions()
    {
        if (dataPaths == null)
        {
            var json = ReadEmbeddedResource("data.dataPaths.json");
            dataPaths = JsonConvert.DeserializeObject<DataPaths>(json)
                        ?? throw new Exception("Impossible de parser dataPaths.json");
        }

        var result = new Dictionary<string, List<(string version, bool hasRecipes)>>();

        foreach (var platform in dataPaths)
        {
            var versionsList = new List<(string version, bool hasRecipes)>();

            foreach (var kv in platform.Value)
            {
                var versionName = kv.Key;
                var hasRecipes = !string.IsNullOrWhiteSpace(kv.Value.Recipes);
                versionsList.Add((versionName, hasRecipes));
            }

            result[platform.Key] = versionsList
                .OrderBy(v => v.version)
                .ToList();
        }

        return result;
    }

    private static (string recipePath, string itemPath) ResolvePaths(Platform platform, string version)
    {
        if (dataPaths == null)
        {
            var json = ReadEmbeddedResource("data.dataPaths.json");
            dataPaths = JsonConvert.DeserializeObject<DataPaths>(json)
                        ?? throw new Exception("Impossible de parser dataPaths.json");
        }

        var platformKey = platform.ToString().ToLower();

        if (!dataPaths.TryGetValue(platformKey, out var versions))
            throw new ArgumentException($"Plateforme inconnue : {platform}");

        if (!versions.TryGetValue(version, out var data))
            throw new ArgumentException($"Version {version} inconnue pour la plateforme {platform}");

        if (string.IsNullOrWhiteSpace(data.Recipes))
            throw new NotSupportedException($"Pas de recipes.json pour la version {version}");

        if (string.IsNullOrWhiteSpace(data.Items))
            throw new NotSupportedException($"Pas de items.json pour la version {version}");

        return (data.Recipes, data.Items);
    }

    private void LoadItems()
    {
        var (_, itemsPath) = ResolvePaths(platform, version);
        var json = ReadEmbeddedResource($"data.{itemsPath.Replace("/", ".")}.items.json");

        allItems = JsonConvert.DeserializeObject<List<Item>>(json) ?? new();
        foreach (var item in allItems)
        {
            itemsById[item.Id] = item;
            itemsByName[item.Name.ToLower()] = item;
        }
    }

    private void LoadRecipes()
    {
        var (recipesPath, _) = ResolvePaths(platform, version);
        var json = ReadEmbeddedResource($"data.{recipesPath.Replace("/", ".")}.recipes.json");

        var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json) ?? new();
        foreach (var entry in dict.Values)
        {
            if (entry.Type == JTokenType.Array)
            {
                foreach (var obj in entry.Children<JObject>())
                    recipesRaw.Add(obj);
            }
            else if (entry.Type == JTokenType.Object)
            {
                recipesRaw.Add((JObject)entry);
            }
        }

        allRecipes = recipesRaw.Select(r => TryParseRecipe(r)).Where(r => r != null).ToList()!;
    }

    public static string GetLatestVersionWithRecipes(Platform platform)
    {
        if (dataPaths == null)
        {
            var json = ReadEmbeddedResource("data.dataPaths.json");
            dataPaths = JsonConvert.DeserializeObject<DataPaths>(json)
                        ?? throw new Exception("Impossible de parser dataPaths.json");
        }

        var platformKey = platform.ToString().ToLower();

        if (!dataPaths.TryGetValue(platformKey, out var versions))
            throw new ArgumentException($"Plateforme inconnue : {platform}");

        var candidates = versions
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Recipes))
            .Select(kv => kv.Key)
            .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!candidates.Any())
            throw new InvalidOperationException($"Aucune version avec recipes.json pour la plateforme {platform}");

        return candidates.First();
    }

    public Item GetItemByIdOrName(string idOrName)
    {
        if (int.TryParse(idOrName, out var id) && itemsById.TryGetValue(id, out var itemById))
            return itemById;

        if (itemsByName.TryGetValue(idOrName.ToLower(), out var itemByName))
            return itemByName;

        throw new ArgumentException($"Aucun item trouvé avec '{idOrName}'");
    }

    public Recipe? GetRecipe(Item item)
    {
        return allRecipes.FirstOrDefault(r => r.Result?.Name == item.Name);
    }

    private Recipe? TryParseRecipe(JObject recipe)
    {
        var result = recipe["result"];
        if (result == null) return null;

        int resultId = result["id"]?.Value<int>() ?? -1;
        int resultMeta = result["metadata"]?.Value<int>() ?? 0;
        int resultCount = result["count"]?.Value<int>() ?? 1;

        if (!itemsById.TryGetValue(resultId, out var resultItem))
            return null;

        var matrix = new Item?[3, 3];

        if (recipe["inShape"] is JArray shape)
        {
            for (int y = 0; y < shape.Count && y < 3; y++)
            {
                if (shape[y] is not JArray row) continue;

                for (int x = 0; x < row.Count && x < 3; x++)
                {
                    var cell = row[x];
                    if (cell == null || cell.Type == JTokenType.Null)
                        continue;

                    int ingredientId = -1;
                    if (cell.Type == JTokenType.Integer)
                        ingredientId = cell.Value<int>();
                    else if (cell.Type == JTokenType.Object)
                        ingredientId = cell["id"]?.Value<int>() ?? -1;

                    if (itemsById.TryGetValue(ingredientId, out var ing))
                        matrix[y, x] = ing;
                }
            }
        }
        else if (recipe["ingredients"] is JArray ingList)
        {
            for (int i = 0; i < Math.Min(ingList.Count, 9); i++)
            {
                var ing = ingList[i];
                int ingredientId = -1;

                if (ing.Type == JTokenType.Integer)
                    ingredientId = ing.Value<int>();
                else if (ing.Type == JTokenType.Object)
                    ingredientId = ing["id"]?.Value<int>() ?? -1;

                if (itemsById.TryGetValue(ingredientId, out var it))
                    matrix[i / 3, i % 3] = it;
            }
        }

        return new Recipe
        {
            Result = resultItem,
            ResultCount = resultCount,
            Matrix = matrix
        };
    }

 private static string ReadEmbeddedResource(string relativePath)
 {
     var cleanedPath = relativePath
         .Replace("\\", ".")
         .Replace("/", ".");
 
     cleanedPath = System.Text.RegularExpressions.Regex.Replace(
         cleanedPath,
         @"(?<=\.)((\d+)(\.\d+)+)(?=\.)",
         m => "_" + m.Value.Replace(".", "._")
     );
 
     var resourceName = $"MCCsharp.Data.minecraft_data.{cleanedPath}";
 
     var assembly = typeof(MinecraftData).Assembly;
     using var stream = assembly.GetManifestResourceStream(resourceName);
 
     if (stream == null)
     {
         var all = string.Join("\n - ", assembly.GetManifestResourceNames());
         throw new FileNotFoundException($"❌ Resource '{resourceName}' not found in assembly.\n\n📦 Ressources disponibles :\n - {all}");
     }
 
     using var reader = new StreamReader(stream);
     return reader.ReadToEnd();
 }


}
