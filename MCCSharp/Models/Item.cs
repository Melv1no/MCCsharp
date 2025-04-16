namespace MCCsharp.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int StackSize { get; set; }
    public string? Texture { get; set; }
}
