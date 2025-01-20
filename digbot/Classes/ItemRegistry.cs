using System.Text.Json;

namespace digbot.Classes
{
    public class CraftingRecipeItem
    {
        public string ItemKey { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class Tool
    {
        // public string Name { get; set; } = "";
        public string ItemKey { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Cost { get; set; }
        public int Equip { get; set; }
        public float Strength { get; set; }
        public int Starting { get; set; }
        public int UseCost { get; set; }
        public List<CraftingRecipeItem> CraftingRecipe { get; set; } = [];
    }

    public class GameData
    {
        public List<Tool> Tools { get; set; } = [];
        public Resource Resource { get; set; } = new();
    }

    public class ResourceType
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = [];
    }

    public class ResourceShape
    {
        public string Type { get; set; } = string.Empty;
        public string ItemFormat { get; set; } = string.Empty;
        public string DesFormat { get; set; } = string.Empty;
        public string Nameformat { get; set; } = string.Empty;
        public int Cost { get; set; }
        public List<string> Ignore { get; set; } = [];
        public List<CraftingIngredient> CraftingIngredients { get; set; } = [];
    }

    public class CraftingIngredient
    {
        public string Type { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public class Resource
    {
        public List<ResourceType> Ore { get; set; } = [];
        public List<ResourceShape> Type { get; set; } = [];
    }

    public class ItemRegistry
    {
        private static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static GameData LoadGameData(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameData>(json, options)!;
        }
    }
}
