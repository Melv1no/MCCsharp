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

    public MinecraftData(Platform platform, string version)
    {
        this.platform = platform;
        this.version = version;
        LoadItems();
        LoadRecipes();
    }

    public static Dictionary<string, List<(string version, bool hasRecipes)>> GetAvailableVersions()
    {
        if (dataPaths == null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "minecraft-data", "data", "dataPaths.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier dataPaths.json introuvable");

            var json = File.ReadAllText(path);
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
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "minecraft-data", "data", "dataPaths.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier dataPaths.json introuvable");

            var json = File.ReadAllText(path);
            dataPaths = JsonConvert.DeserializeObject<DataPaths>(json)
                        ?? throw new Exception("Impossible de parser dataPaths.json");
        }

        var platformKey = platform.ToString().ToLower(); // "pc" ou "bedrock"

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

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "minecraft-data", "data", itemsPath, "items.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"items.json introuvable à : {path}");

        var json = File.ReadAllText(path);
        var items = JsonConvert.DeserializeObject<List<Item>>(json) ?? new();

        foreach (var item in items)
        {
            itemsById[item.Id] = item;
            itemsByName[item.Name.ToLower()] = item;
        }
    }


    private void LoadRecipes()
    {
        var (recipesPath, _) = ResolvePaths(platform, version);

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "minecraft-data", "data", recipesPath, "recipes.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"recipes.json introuvable à : {path}");

        var json = File.ReadAllText(path);
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
    }

    public static string GetLatestVersionWithRecipes(Platform platform)
    {
        if (dataPaths == null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "minecraft-data", "dataPaths.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier dataPaths.json introuvable");

            var json = File.ReadAllText(path);
            dataPaths = JsonConvert.DeserializeObject<DataPaths>(json)
                        ?? throw new Exception("Impossible de parser dataPaths.json");
        }

        var platformKey = platform.ToString().ToLower(); // "pc" ou "bedrock"

        if (!dataPaths.TryGetValue(platformKey, out var versions))
            throw new ArgumentException($"Plateforme inconnue : {platform}");

        var candidates = versions
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Recipes))
            .Select(kv => kv.Key)
            .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase) // pour trier alphanum
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

   public Recipe GetRecipe(Item item)
{
    foreach (var recipe in recipesRaw)
    {
        var result = recipe["result"];
        if (result == null) continue;

        int resultId = result["id"]?.Value<int>() ?? -1;
        int resultMeta = result["metadata"]?.Value<int>() ?? 0;

        if (resultId != item.Id) continue;

        if (recipe["inShape"] is JArray shape)
        {
            var matrix = new Item?[3, 3];

            for (int y = 0; y < shape.Count && y < 3; y++)
            {
                if (shape[y] is not JArray row) continue;

                for (int x = 0; x < row.Count && x < 3; x++)
                {
                    var cell = row[x];

                    if (cell == null || cell.Type == JTokenType.Null)
                        continue;

                    int ingredientId = -1;
                    int ingredientMeta = 0;

                    if (cell.Type == JTokenType.Integer)
                    {
                        ingredientId = cell.Value<int>();
                    }
                    else if (cell.Type == JTokenType.Object)
                    {
                        ingredientId = cell["id"]?.Value<int>() ?? -1;
                        ingredientMeta = cell["metadata"]?.Value<int>() ?? 0;
                    }

                    if (itemsById.TryGetValue(ingredientId, out var ing))
                    {
                        matrix[y, x] = ing; // Option : tu peux vérifier metadata si tu veux
                    }
                }
            }

            return new Recipe
            {
                Result = item,
                ResultCount = result["count"]?.Value<int>() ?? 1,
                Matrix = matrix
            };
        }

        // Support shapeless (flat list)
        if (recipe["ingredients"] is JArray ingList)
        {
            var matrix = new Item?[3, 3];

            for (int i = 0; i < Math.Min(ingList.Count, 9); i++)
            {
                var ing = ingList[i];

                int ingredientId = -1;

                if (ing.Type == JTokenType.Integer)
                {
                    ingredientId = ing.Value<int>();
                }
                else if (ing.Type == JTokenType.Object)
                {
                    ingredientId = ing["id"]?.Value<int>() ?? -1;
                }

                if (itemsById.TryGetValue(ingredientId, out var it))
                {
                    matrix[i / 3, i % 3] = it;
                }
            }

            return new Recipe
            {
                Result = item,
                ResultCount = result["count"]?.Value<int>() ?? 1,
                Matrix = matrix
            };
        }
    }

    throw new InvalidOperationException($"Aucune recette trouvée pour l’item {item.Name} (ID: {item.Id})");
}
}
