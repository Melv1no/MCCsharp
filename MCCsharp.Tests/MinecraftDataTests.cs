
using MCCsharp;
using MCCsharp.Enums;
using Xunit;

namespace MCCsharp.Tests;

public class MinecraftDataTests
{
    [Fact]
    public void GetItemByName_ShouldReturnItem()
    {
        var mc = new MinecraftData(Platform.Pc, "1.17");
        var item = mc.GetItemByIdOrName("crafting_table");

        Assert.NotNull(item);
        Assert.Equal("crafting_table", item.Name);
    }

    [Fact]
    public void GetRecipe_ShouldReturnCraftingMatrix()
    {
        var mc = new MinecraftData(Platform.Pc, "1.17");
        var item = mc.GetItemByIdOrName("crafting_table");
        var recipe = mc.GetRecipe(item);

        Assert.NotNull(recipe);
        Assert.Equal(1, recipe.ResultCount);
    }
    
    [Fact]
    public void GetItemById_ShouldReturnCorrectItem()
    {
        var mc = new MinecraftData(Platform.Pc, "1.17");
        var item = mc.GetItemByIdOrName("59"); 

        Assert.NotNull(item);
        Assert.Equal("crafting_table", item.Name);
    }
    
    [Fact]
    public void GetRecipe_WithVersionWithoutRecipes_ShouldThrow()
    {
        var mc = new MinecraftData(Platform.Pc, "1.7");
        var item = mc.GetItemByIdOrName("crafting_table");

        var ex = Assert.Throws<InvalidOperationException>(() => mc.GetRecipe(item));
        Assert.Contains("Aucune recette trouvée", ex.Message);
    }
    
    [Fact]
    public void GetItemByName_InvalidName_ShouldThrow()
    {
        var mc = new MinecraftData(Platform.Pc, "1.17");

        var ex = Assert.Throws<ArgumentException>(() => mc.GetItemByIdOrName("potion_of_beer"));
        Assert.Contains("Aucun item trouvé", ex.Message);
    }
    [Fact]
    public void GetRecipe_ShouldContainCorrectIngredients()
    {
        var mc = new MinecraftData(Platform.Pc, "1.17");
        var item = mc.GetItemByIdOrName("crafting_table");
        var recipe = mc.GetRecipe(item);

        var matrix = recipe.Matrix;
        var wood = mc.GetItemByIdOrName("planks");

        Assert.Equal(wood.Id, matrix[0, 0]?.Id);
        Assert.Equal(wood.Id, matrix[0, 1]?.Id);
        Assert.Equal(wood.Id, matrix[1, 0]?.Id);
        Assert.Equal(wood.Id, matrix[1, 1]?.Id);
    }
    
    [Fact]
    public void GetLatestVersionWithRecipes_ShouldReturnValidVersion()
    {
        var latest = MinecraftData.GetLatestVersionWithRecipes(Platform.Pc);
        Assert.False(string.IsNullOrWhiteSpace(latest));
    }


}
