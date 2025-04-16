namespace MCCsharp.Models;

public class Recipe
{
    public Item Result { get; set; } = default!;
    public int ResultCount { get; set; }
    public Item?[,] Matrix { get; set; } = new Item?[3, 3];
}
