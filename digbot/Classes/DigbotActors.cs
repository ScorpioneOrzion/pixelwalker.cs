namespace digbot.Classes
{
    public class DamageReduce(float baseValue = 0.0f) : Attributes<DamageType, float>(baseValue) { }

    public class ItemLimits(int baseValue = 0) : Attributes<ItemType, int>(baseValue) { }

    public abstract class Actor
    {
        public float Power { get; set; }
    }

    public class World : Actor { }

    public abstract class Entity : Actor
    {
        public float MaxHealth;
        private float _health;
        public int Perception = 1;
        public float Luck = 0f;
        public float Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }

        protected Entity(float maxHealth = 100f)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;

            FlatDefense = new DamageReduce { };

            PercentageDefense = new DamageReduce { };

            ItemLimits = new ItemLimits { };
        }

        public ItemLimits ItemLimits { get; private set; }
        public DamageReduce FlatDefense { get; private set; }
        public DamageReduce PercentageDefense { get; private set; }
        private readonly Dictionary<DigbotItem, int> _inventory = [];
        public IReadOnlyDictionary<DigbotItem, int> Inventory => _inventory.AsReadOnly();

        public void AddItems(DigbotItem item, int amount = 1)
        {
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
            Power += item.PowerBoost * amount;
            MaxHealth += item.HealthBoost * amount;
            Luck += item.LuckBoost * amount;
            Perception += item.PerceptionBoost * amount;
        }

        public void RemoveItems(DigbotItem item, int amount = 1)
        {
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
            Power -= item.PowerBoost * amount;
            MaxHealth -= item.HealthBoost * amount;
            Luck -= item.LuckBoost * amount;
            Perception -= item.PerceptionBoost * amount;
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
            Power = 1f;
        }

        public bool Banned = false;
        public required DigbotPlayerRole Role;
        public required string Username;
    }
}
