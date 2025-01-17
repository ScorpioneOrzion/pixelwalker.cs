using System.Text.Json;

namespace digbot.Classes
{
    public class CraftingRecipeItem
    {
        public string ItemKey = "";
        public int Quantity { get; set; }
    }

    public class Tool
    {
        // public string Name = "";
        public string ItemKey = "";
        public string Type = "";
        public int Cost { get; set; }
        public int Equip { get; set; }
        public float Strength { get; set; }
        public int Starting { get; set; }
        public int UseCost { get; set; }
        public List<CraftingRecipeItem> CraftingRecipe = [];
    }

    public class GameData
    {
        public List<Tool> Tools = [];
        public List<object> Resource = [];
    }

    public class ItemRegistry
    {
        public static GameData LoadGameData(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameData>(json) ?? new();
        }
    }
}
