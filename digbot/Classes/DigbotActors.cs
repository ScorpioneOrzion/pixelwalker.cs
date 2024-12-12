namespace digbot.Classes
{
    public enum DamageType
    {
        Physical,
        Explosion,
        Fire,
        Poison,
        Electric,
    }

    public enum DigbotPlayerRole
    {
        None,
        Immune,
        Owner,
    }

    public class Defense
    {
        public float Resistance = 0.0f;
        public float Physical = 0.0f;
        public float Fire = 0.0f;
        public float Poison = 0.0f;
        public float Electric = 0.0f;
        public float Explosion = 0.0f;

        public float Type(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => Resistance + Physical,
                DamageType.Fire => Resistance + Fire,
                DamageType.Poison => Resistance + Poison,
                DamageType.Electric => Resistance + Electric,
                DamageType.Explosion => Resistance + Explosion,
                _ => Resistance,
            };
        }

        public static Defense operator +(Defense a, Defense b)
        {
            return new Defense
            {
                Resistance = a.Resistance + b.Resistance,
                Physical = a.Physical + b.Physical,
                Fire = a.Fire + b.Fire,
                Poison = a.Poison + b.Poison,
                Electric = a.Electric + b.Electric,
                Explosion = a.Explosion + b.Explosion,
            };
        }

        public static Defense operator -(Defense a, Defense b)
        {
            return new Defense
            {
                Resistance = a.Resistance - b.Resistance,
                Physical = a.Physical - b.Physical,
                Fire = a.Fire - b.Fire,
                Poison = a.Poison - b.Poison,
                Electric = a.Electric - b.Electric,
                Explosion = a.Explosion - b.Explosion,
            };
        }

        public static Defense operator *(Defense a, dynamic b)
        {
            if (b is Defense)
            {
                return new Defense
                {
                    Resistance = a.Resistance * b.Resistance,
                    Physical = a.Physical * b.Physical,
                    Fire = a.Fire * b.Fire,
                    Poison = a.Poison * b.Poison,
                    Electric = a.Electric * b.Electric,
                    Explosion = a.Explosion * b.Explosion,
                };
            }
            else if (b is int || b is float || b is double)
                return new Defense
                {
                    Resistance = a.Resistance * b,
                    Physical = a.Physical * b,
                    Fire = a.Fire * b,
                    Poison = a.Poison * b,
                    Electric = a.Electric * b,
                    Explosion = a.Explosion * b,
                };
            throw new ArgumentException("Unsupported type for multiplication");
        }
    }

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

            FlatDefense = new Defense
            {
                Resistance = 0f,
                Physical = 0f,
                Fire = 0f,
                Poison = 0f,
                Electric = 0f,
                Explosion = 0f,
            };

            PercentageDefense = new Defense
            {
                Resistance = 0f,
                Physical = 0f,
                Fire = 0f,
                Poison = 0f,
                Electric = 0f,
                Explosion = 0f,
            };
        }

        public Defense FlatDefense { get; protected set; }
        public Defense PercentageDefense { get; protected set; }
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
            FlatDefense += item.FlatDefenseBoost * amount;
            PercentageDefense += item.PercentageDefenseBoost * amount;
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
            FlatDefense -= item.FlatDefenseBoost * amount;
            PercentageDefense -= item.PercentageDefenseBoost * amount;
            Power -= item.PowerBoost * amount;
            MaxHealth -= item.HealthBoost * amount;
            Luck -= item.LuckBoost * amount;
            Perception -= item.PerceptionBoost * amount;
        }

        public void TakeDamage(float damage, DamageType type)
        {
            float flatDefense = FlatDefense.Type(type);
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
            Health -= ModifiedDamage * (1 - PercentageDefense.Type(type));
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
