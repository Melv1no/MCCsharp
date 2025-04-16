# MCCsharp

**MCCsharp** is a C# wrapper for the [minecraft-data](https://github.com/PrismarineJS/minecraft-data) project. It provides access to Minecraft items, names, IDs, crafting recipes (`recipes.json`) and more, directly from .NET code.

> 📦 Available on NuGet: [nuget.org/packages/MCCsharp](https://www.nuget.org/packages/MCCsharp)

---

## 🚀 Features

- Access Minecraft items by ID or name
- Automatically load recipes even when not directly present in the version
- Support for both `crafting_shaped` and `crafting_shapeless` recipes
- Strongly-typed 3x3 matrix of ingredients
- Handles Minecraft platform and version resolution via `dataPaths.json`

---

## 🛠️ Installation

```bash
dotnet add package MCCsharp
```

---

## 📦 Usage Example

```csharp
using MCCsharp;
using MCCsharp.Enums;

var mc = new MinecraftData(Platform.Pc, "1.17");

var item = mc.GetItemByIdOrName("crafting_table");

var recipe = mc.GetRecipe(item);

Console.WriteLine($"Crafting: {item.DisplayName} x{recipe.ResultCount}");

for (int y = 0; y < 3; y++)
{
    for (int x = 0; x < 3; x++)
    {
        var cell = recipe.Matrix[y, x];
        Console.Write((cell?.DisplayName ?? " ") + "\t");
    }
    Console.WriteLine();
}
```

---

## ✅ Compatibility

Platforms:
- ✅ `pc` (Java Edition)
- ✅ `bedrock` (planned or limited support)

Tested Minecraft versions:
- `1.8` to `1.21.4` (Java)
- Fully dynamic support through `dataPaths.json`

---

## 🧱 Advanced Features

- `GetAvailableVersions()` — Lists all supported versions with recipe availability
- `GetLatestVersionWithRecipes(Platform platform)` — Gets the latest recipe-compatible version

---

## 📂 Data Structure

Data files are based on [`minecraft-data`](https://github.com/PrismarineJS/minecraft-data), included as a submodule or local directory.

```
Data/
└── minecraft-data/
    ├── data/
    │   ├── pc/
    │   └── bedrock/
    └── dataPaths.json
```

---

## 🧪 Testing

Includes unit tests using [xUnit](https://xunit.net):

```bash
dotnet test
```

---

## 📖 License

MIT — use freely in personal or commercial projects.