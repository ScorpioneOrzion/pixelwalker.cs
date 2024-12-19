namespace digbot.Classes
{
    public class DigbotItem
    {
        public string Name = "";
        public string Description = "";
        public float HealthBoost = 0f;
        public float PowerBoost = 0f;
        public DamageReduce FlatDefenseBoost = new();
        public DamageReduce PercentageDefenseBoost = new();
        public ItemLimits LimitBoost = new();
        public float LuckBoost = 0f;
        public int PerceptionBoost = 0;
        public int TypeUse = 0;
        public (ItemType, ActionType?) Type;
        public Action<DigbotPlayer, DigbotItem> Use = (player, item) => { };
        public bool Hidden = false;
        public float Time = 0f;
    }

    public class HiddenDigbotItem : DigbotItem
    {
        public HiddenDigbotItem()
        {
            Name = "";
            Description = "";
            Type = (ItemType.Miscellaneous, null);
            Hidden = true;
        }
    }
}
