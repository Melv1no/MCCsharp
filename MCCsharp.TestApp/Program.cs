using MCCsharp;
using MCCsharp.Enums;

var mc = new MinecraftData(Platform.Pc, "1.17");
var item = mc.GetItemByIdOrName("crafting_table");
var recipe = mc.GetRecipe(item);

Console.WriteLine($"Crafting {recipe.Result.DisplayName} x{recipe.ResultCount}");