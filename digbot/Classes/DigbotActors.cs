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

    public class Stats
    {
        public float AbsoluteStrength = default;
        public float RelativeStrength = default;
        public float AbsoluteLuck = default;
        public float RelativeLuck = default;
        public float AbsolutePerception = default;
        public float RelativePerception = default;
        public float Strength => AbsoluteStrength * (1 + RelativeStrength);
        public float Luck => AbsoluteLuck * (1 + RelativeLuck);
        public float Perception => AbsolutePerception * (1 + RelativePerception);

        public static Stats operator +(Stats a, Stats b)
        {
            return new Stats
            {
                AbsoluteStrength = a.AbsoluteStrength + b.AbsoluteStrength,
                RelativeStrength = a.RelativeStrength + b.RelativeStrength,
                AbsoluteLuck = a.AbsoluteLuck + b.AbsoluteLuck,
                RelativeLuck = a.RelativeLuck + b.RelativeLuck,
                AbsolutePerception = a.AbsolutePerception + b.AbsolutePerception,
                RelativePerception = a.RelativePerception + b.RelativePerception,
            };
        }

        public static Stats operator -(Stats a, Stats b)
        {
            return new Stats
            {
                AbsoluteStrength = a.AbsoluteStrength - b.AbsoluteStrength,
                RelativeStrength = a.RelativeStrength - b.RelativeStrength,
                AbsoluteLuck = a.AbsoluteLuck - b.AbsoluteLuck,
                RelativeLuck = a.RelativeLuck - b.RelativeLuck,
                AbsolutePerception = a.AbsolutePerception - b.AbsolutePerception,
                RelativePerception = a.RelativePerception - b.RelativePerception,
            };
        }

        public static Stats operator *(Stats a, Stats b)
        {
            return new Stats
            {
                AbsoluteStrength = a.AbsoluteStrength * b.AbsoluteStrength,
                RelativeStrength = a.RelativeStrength * b.RelativeStrength,
                AbsoluteLuck = a.AbsoluteLuck * b.AbsoluteLuck,
                RelativeLuck = a.RelativeLuck * b.RelativeLuck,
                AbsolutePerception = a.AbsolutePerception * b.AbsolutePerception,
                RelativePerception = a.RelativePerception * b.RelativePerception,
            };
        }

        public static Stats operator *(Stats a, int multiplier)
        {
            return new Stats
            {
                AbsoluteStrength = a.AbsoluteStrength * multiplier,
                RelativeStrength = a.RelativeStrength * multiplier,
                AbsoluteLuck = a.AbsoluteLuck * multiplier,
                RelativeLuck = a.RelativeLuck * multiplier,
                AbsolutePerception = a.AbsolutePerception * multiplier,
                RelativePerception = a.RelativePerception * multiplier,
            };
        }

        public static Stats operator *(int multiplier, Stats a) => a * multiplier;
    }

    public class DigbotItem
    {
        public string Name = "";
        public string Description = "";
        public ItemLimits ItemLimits = new();
        public int TypeUse = 0;
        public float Cost = 0f;
        public ItemType Type = default;
        public bool Hidden = false;
        public bool Buyable => Cost != 0f || Hidden == true;
        public Func<Entity, ActionType, (int x, int y)?, (Stats, List<string>)?> Use = (
            entity,
            action,
            position
        ) =>
        {
            return null;
        };

        public void Buy(Entity player, string amount)
        {
            if (!Buyable)
                return;
            if (amount == "all")
                Buy(player, (int)Math.Floor(player.Gold / Cost));
            else if (int.TryParse(amount, out int num))
                Buy(player, num);
        }

        public void Buy(Entity player, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (!Buyable)
                return;
            if (player.Gold < Cost * amount)
                return;
            player.SetItems(this, amount);
            player.Gold -= Cost * amount;
        }

        public void Sell(Entity player, string amount)
        {
            if (!Buyable)
                return;
            if (amount == "all")
                Sell(player, player.Inventory[this]);
            else if (int.TryParse(amount, out int num))
                Sell(player, num);
        }

        public void Sell(Entity player, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (!Buyable)
                return;
            if (!player.Inventory.ContainsKey(this))
                return;
            if (player.Inventory[this] < amount)
                amount = player.Inventory[this];
            player.SetItems(this, -amount);
            player.Gold += Cost * amount * 0.9f;
        }
    }

    public class HiddenDigbotItem : DigbotItem
    {
        public HiddenDigbotItem()
        {
            Cost = 0f;
            Hidden = true;
        }
    }

    public abstract class Actor { }

    public abstract class Entity : Actor
    {
        public (Stats, List<string>) Use(ActionType action, (int x, int y)? position)
        {
            Stats totalStats = new();
            List<string> messages = [];

            foreach (var (item, count) in Inventory)
            {
                var result = item.Use(this, action, position); // Get the result from the item

                // If the result is not null and contains Stats and messages
                if (result != null && result.Value is (Stats stats, List<string> message))
                {
                    totalStats += stats * count;
                    messages.AddRange(message);
                }
            }

            return (totalStats, messages);
        }

        public float Gold = default;

        public ItemLimits ItemLimits { get; private set; } = new();
        private readonly Dictionary<DigbotItem, int> _inventory = [];
        public IReadOnlyDictionary<DigbotItem, int> Inventory => _inventory.AsReadOnly();

        public void SetItems(DigbotItem item, int amount = 0)
        {
            if (_inventory.ContainsKey(item))
                _inventory[item] += amount;
            else
                _inventory[item] = amount;
        }
    }

    public class DigbotPlayer : Entity
    {
        public Random RandomGenerator { get; private set; }

        public DigbotPlayer(float initialPower = 1)
        {
            if (Registry.Items != null)
            {
                SetItems(Registry.Items["StaticPower"], (int)(initialPower * 100));
            }
            RandomGenerator = new Random((int)DateTime.Now.Ticks ^ this.GetHashCode());
        }

        public int GetRandomInt(int min, int max)
        {
            return RandomGenerator.Next(min, max);
        }

        public required DigbotPlayerRole Role;
        public required string Username;
    }
}
