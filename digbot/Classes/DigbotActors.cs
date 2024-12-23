namespace digbot.Classes
{
    public class DamageReduce(float baseValue = 0) : Attributes<DamageType, float>(baseValue) { }

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
        private (float a, float r) _maxHealth;
        public float AbsoluteMaxHealth
        {
            get => _maxHealth.a;
            set => _maxHealth.a = value;
        }
        public float RelativeMaxHealth
        {
            get => _maxHealth.r;
            set => _maxHealth.r = value;
        }
        public float MaxHealth => AbsoluteMaxHealth * (1 + RelativeMaxHealth);
        private float _health;
        public float Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }

        public int Perception = 1;
        private (float a, float r) _luck = (0, 0);
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

        private (float a, float r) _gold = (0f, 0f);

        public float Gold
        {
            get => _gold.a;
            set => _gold.a = value;
        }
        public float RelativeGold
        {
            get => _gold.r;
            set => _gold.r = value;
        }

        protected Entity(float maxHealth = 100f)
        {
            AbsoluteMaxHealth = maxHealth;
            Health = maxHealth;

            FlatDefense = new() { };

            PercentageDefense = new() { };

            ItemLimits = new() { };
        }

        public ItemLimits ItemLimits { get; private set; }
        public DamageReduce FlatDefense { get; private set; }
        public DamageReduce PercentageDefense { get; private set; }
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
            FlatDefense = (DamageReduce)(FlatDefense + item.FlatDefenseBoost * amount);
            PercentageDefense = (DamageReduce)(
                PercentageDefense + item.PercentageDefenseBoost * amount
            );
            ItemLimits = (ItemLimits)(ItemLimits + item.LimitBoost * amount);
            ItemLimits[item.Type.Item1] -= item.TypeUse * amount;
            AbsolutePower += item.PowerBoost.a * amount;
            RelativePower += item.PowerBoost.r * amount;
            AbsoluteMaxHealth += item.HealthBoost.a * amount;
            RelativeMaxHealth += item.HealthBoost.r * amount;
            AbsoluteLuck += item.LuckBoost.a * amount;
            RelativeLuck += item.LuckBoost.r * amount;
            Perception += item.PerceptionBoost * amount;
            RelativeGold += item.GoldChange * amount;
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
            FlatDefense = (DamageReduce)(FlatDefense - item.FlatDefenseBoost * amount);
            PercentageDefense = (DamageReduce)(
                PercentageDefense - item.PercentageDefenseBoost * amount
            );
            ItemLimits = (ItemLimits)(ItemLimits - item.LimitBoost * amount);
            ItemLimits[item.Type.Item1] += item.TypeUse * amount;
            AbsolutePower -= item.PowerBoost.a * amount;
            RelativePower -= item.PowerBoost.r * amount;
            AbsoluteMaxHealth -= item.HealthBoost.a * amount;
            RelativeMaxHealth -= item.HealthBoost.r * amount;
            AbsoluteLuck -= item.LuckBoost.a * amount;
            RelativeLuck -= item.LuckBoost.r * amount;
            Perception -= item.PerceptionBoost * amount;
            RelativeGold -= item.GoldChange * amount;
        }

        public void TakeDamage(float damage, DamageType type)
        {
            float flatDefense = FlatDefense[type];
            float ModifiedDamage;
            if (flatDefense + 1 < damage)
            {
                ModifiedDamage = damage - flatDefense;
            }
            else if (damage > 1)
            {
                ModifiedDamage = 1 / (flatDefense - damage + 2);
            }
            else
            {
                ModifiedDamage = 1 / (flatDefense + 1);
            }
            Health -= ModifiedDamage * (1 - PercentageDefense[type]);
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
