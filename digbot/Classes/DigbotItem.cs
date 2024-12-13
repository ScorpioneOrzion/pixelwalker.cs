namespace digbot.Classes
{
    public class DigbotItem
    {
        public required string Name;
        public required string Description;
        public float HealthBoost = 0f;
        public float PowerBoost = 0f;
        public DamageReduce FlatDefenseBoost = new();
        public DamageReduce PercentageDefenseBoost = new();
        public ItemLimits LimitBoost = new();
        public float LuckBoost = 0f;
        public int PerceptionBoost = 0;
        public int TypeUse = 0;
        public required (ItemType, ActionType?) Type;
        public required Action Use;
        public float Time = 0f;
    }
}
