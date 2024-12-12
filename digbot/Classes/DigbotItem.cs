namespace digbot.Classes
{
    public enum ItemType
    {
        Consumable,
        Armor,
        Weapon,
        Tool,
        KeyItem,
        Miscellaneous,
    }

    public class DigbotItem
    {
        public required string Name;
        public required string Description;
        public float HealthBoost = 0f;
        public float PowerBoost = 0f;
        public Defense FlatDefenseBoost = new();
        public Defense PercentageDefenseBoost = new();
        public float LuckBoost = 0f;
        public int PerceptionBoost = 0;
        public required ItemType Type;
        public required Action Use;
        public float Time = 0f;
    }
}
