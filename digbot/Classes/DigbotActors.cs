namespace digbot.Classes
{
    public class ItemLimits(int baseValue = 0) : Attributes<ItemType, int>(baseValue) { }

    public abstract class Actor
    {
        private (float a, float r) _power = (0, 0);

        public float AbsolutePower
        {
            get => _power.a;
            set => _power.a = value;
        }

        public float RelativePower
        {
            get => _power.r;
            set => _power.r = value;
        }

        public float GetPower => AbsolutePower * (1 + RelativePower);
    }

    public class World : Actor { }

    public abstract class Entity : Actor
    {
        public required TimeManager TimeManager;

        public int Perception = 1;
        private (float a, float r) _luck = (0, 0);
        private (float current, float loss) _gold = (0f, 0f);
        public float AbsoluteLuck
        {
            get => _luck.a;
            set => _luck.a = value;
        }
        public float RelativeLuck
        {
            get => _luck.r;
            set => _luck.r = value;
        }
        public float Luck => AbsoluteLuck * (1 + RelativeLuck);
        public float Gold
        {
            get => _gold.current;
            set => _gold.current = value;
        }

        public float GoldDrop
        {
            get => _gold.loss;
            set => _gold.loss = value;
        }

        public ItemLimits ItemLimits { get; private set; } = new();
        private readonly Dictionary<DigbotItem, int> _inventory = [];
        public IReadOnlyDictionary<DigbotItem, int> Inventory => _inventory.AsReadOnly();

        public void AddItems(DigbotItem item, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (_inventory.ContainsKey(item))
            {
                _inventory[item] += amount;
            }
            else
            {
                _inventory[item] = amount;
            }
            ItemLimits = (ItemLimits)(ItemLimits + item.LimitBoost * amount);
            ItemLimits[item.Type.Item1] -= item.TypeUse * amount;
            AbsolutePower += item.PowerBoost.a * amount;
            RelativePower += item.PowerBoost.r * amount;
            AbsoluteLuck += item.LuckBoost.a * amount;
            RelativeLuck += item.LuckBoost.r * amount;
            Perception += item.PerceptionBoost * amount;
            GoldDrop += item.Gold.d * amount;
            if (item.Time > 0)
            {
                TimeManager.AddTimer(item, this, item.Time);
            }
        }

        public void RemoveItems(DigbotItem item, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (!_inventory.TryGetValue(item, out int SetAmount))
                return;
            if (amount >= SetAmount)
            {
                amount = SetAmount;
                _inventory.Remove(item);
            }
            else
            {
                _inventory[item] -= amount;
            }
            ItemLimits = (ItemLimits)(ItemLimits - item.LimitBoost * amount);
            ItemLimits[item.Type.Item1] += item.TypeUse * amount;
            AbsolutePower -= item.PowerBoost.a * amount;
            RelativePower -= item.PowerBoost.r * amount;
            AbsoluteLuck -= item.LuckBoost.a * amount;
            RelativeLuck -= item.LuckBoost.r * amount;
            Perception -= item.PerceptionBoost * amount;
            GoldDrop -= item.Gold.d * amount;
        }
    }

    public class DigbotPlayer : Entity
    {
        public DigbotPlayer()
        {
            AbsolutePower = 1;
        }

        public bool Banned = false;
        public required DigbotPlayerRole Role;
        public required string Username;
    }
}
