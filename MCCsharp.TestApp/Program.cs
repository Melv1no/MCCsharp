using MCCsharp;
using MCCsharp.Enums;

var mc = new MinecraftData(Platform.Pc, "1.9");

Console.Write("Recherche : ");



var search = Console.ReadLine() ?? "";

var matches = mc.SearchItems(search, 10);

if (!matches.Any())
{
    Console.WriteLine("❌ Aucun item correspondant trouvé.");
    return;
}

Console.WriteLine($"\n🔎 Résultats pour \"{search}\" :");
foreach (var item in matches)
{
    Console.WriteLine($"- {item.DisplayName} (id: {item.Id}, name: {item.Name})");
}