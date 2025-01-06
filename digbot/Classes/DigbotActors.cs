namespace digbot.Classes
{
    public class ItemLimits(int baseValue = 0)
    {
        public int Generic = baseValue;
        public int Consumable = 0;
        public int Armor = 0;
        public int Weapon = 0;
        public int Tool = 0;
        public int Miscellaneous = 0;

        public int this[string name]
        {
            get
            {
                return name switch
                {
                    "Generic" => Generic,
                    "Consumable" => Consumable + Generic,
                    "Armor" => Armor + Generic,
                    "Weapon" => Weapon + Generic,
                    "Tool" => Tool + Generic,
                    "Miscellaneous" => Miscellaneous + Generic,
                    _ => throw new ArgumentException("Unsupported type"),
                };
            }
            set
            {
                _ = name switch
                {
                    "Generic" => Generic = value,
                    "Consumable" => Consumable = value,
                    "Armor" => Armor = value,
                    "Weapon" => Weapon = value,
                    "Tool" => Tool = value,
                    "Miscellaneous" => Miscellaneous = value,
                    _ => throw new ArgumentException("Unsupported type"),
                };
            }
        }

        public int this[ItemType type]
        {
            get
            {
                return type switch
                {
                    ItemType.Generic => Generic,
                    ItemType.Consumable => Consumable + Generic,
                    ItemType.Armor => Armor + Generic,
                    ItemType.Weapon => Weapon + Generic,
                    ItemType.Tool => Tool + Generic,
                    ItemType.Miscellaneous => Miscellaneous + Generic,
                    _ => throw new ArgumentException("Unsupported type"),
                };
            }
            set
            {
                _ = type switch
                {
                    ItemType.Generic => Generic = value,
                    ItemType.Consumable => Consumable = value,
                    ItemType.Armor => Armor = value,
                    ItemType.Weapon => Weapon = value,
                    ItemType.Tool => Tool = value,
                    ItemType.Miscellaneous => Miscellaneous = value,
                    _ => throw new ArgumentException("Unsupported type"),
                };
            }
        }

        public static ItemLimits operator +(ItemLimits a, ItemLimits b)
        {
            return new ItemLimits
            {
                Generic = a.Generic + b.Generic,
                Consumable = a.Consumable + b.Consumable,
                Armor = a.Armor + b.Armor,
                Weapon = a.Weapon + b.Weapon,
                Tool = a.Tool + b.Tool,
                Miscellaneous = a.Miscellaneous + b.Miscellaneous,
            };
        }

        public static ItemLimits operator -(ItemLimits a, ItemLimits b)
        {
            return new ItemLimits
            {
                Generic = a.Generic - b.Generic,
                Consumable = a.Consumable - b.Consumable,
                Armor = a.Armor - b.Armor,
                Weapon = a.Weapon - b.Weapon,
                Tool = a.Tool - b.Tool,
                Miscellaneous = a.Miscellaneous - b.Miscellaneous,
            };
        }

        public static ItemLimits operator *(ItemLimits a, dynamic b)
        {
            if (b is ItemLimits)
            {
                return new ItemLimits
                {
                    Generic = a.Generic * b.Generic,
                    Consumable = a.Consumable * b.Consumable,
                    Armor = a.Armor * b.Armor,
                    Weapon = a.Weapon * b.Weapon,
                    Tool = a.Tool * b.Tool,
                    Miscellaneous = a.Miscellaneous * b.Miscellaneous,
                };
            }
            else if (b is int || b is float || b is double)
            {
                return new ItemLimits
                {
                    Generic = a.Generic * b,
                    Consumable = a.Consumable * b,
                    Armor = a.Armor * b,
                    Weapon = a.Weapon * b,
                    Tool = a.Tool * b,
                    Miscellaneous = a.Miscellaneous * b,
                };
            }
            throw new ArgumentException("Unsupported type for multiplication");
        }

        public static ItemLimits operator *(dynamic a, ItemLimits b)
        {
            return b * a;
        }
    }

    public abstract class Actor { }

    public abstract class Entity : Actor
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

        public required DigbotPlayerRole Role;
        public required string Username;
    }
}
