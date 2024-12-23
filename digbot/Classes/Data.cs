using PixelPilot.Client.World.Constants;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class Registry
    {
        public static readonly CaseInsensitiveDictionary<DigbotCommand> Commands = new()
        {
            {
                "reset",
                new()
                {
                    Execute = (args, player, client, world, random) =>
                    {
                        world.Reset(client);
                    },
                    Roles = [DigbotPlayerRole.Owner],
                }
            },
            {
                "help",
                new()
                {
                    Execute = (args, player, client, world, random) =>
                    {
                        if (args.Length == 0)
                        {
                            var commands = world
                                .Commands.Where(command =>
                                    command.Value.Roles.Contains(player.Role)
                                )
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            client.Send(
                                new PlayerChatPacket()
                                {
                                    Message = $"/dm {player.Username} {commandsList}",
                                }
                            );
                        }
                    },
                    Roles =
                    [
                        DigbotPlayerRole.Owner,
                        DigbotPlayerRole.Immune,
                        DigbotPlayerRole.None,
                    ],
                }
            },
            {
                "exit",
                new()
                {
                    Execute = (args, player, client, world, random) =>
                    {
                        client.Disconnect();
                    },
                    Roles = [DigbotPlayerRole.Owner],
                }
            },
            {
                "use",
                new()
                {
                    Execute = (args, player, client, world, random) => { },
                    Roles =
                    [
                        DigbotPlayerRole.Owner,
                        DigbotPlayerRole.Immune,
                        DigbotPlayerRole.None,
                    ],
                }
            },
            {
                "inventory",
                new()
                {
                    Execute = (args, player, client, world, random) => { },
                    Roles =
                    [
                        DigbotPlayerRole.Owner,
                        DigbotPlayerRole.Immune,
                        DigbotPlayerRole.None,
                    ],
                }
            },
        };
        public static readonly CaseInsensitiveDictionary<DigbotWorld> Worlds = new()
        {
            {
                "core",
                new(
                    (world, action, actor, oldblock, newBlock, position, health) =>
                    {
                        return (newBlock, 0f);
                    }
                )
                {
                    Name = "The Core",
                    Ground = PixelBlock.BasicBlack,
                    BlockState = new (PixelBlock, float health)[400, 360],
                    AirHeight = 40,
                    Blocks =
                    [
                        (PixelBlock.LavaYellow, 1, (player, position) => true),
                        (PixelBlock.LavaOrange, 1, (player, position) => true),
                        (PixelBlock.LavaDarkOrange, 1, (player, position) => true),
                        (PixelBlock.GemstoneGreen, 1, (player, position) => true),
                        (PixelBlock.GemstonePurple, 1, (player, position) => true),
                        (PixelBlock.GemstoneYellow, 1, (player, position) => true),
                        (PixelBlock.GemstoneBlue, 1, (player, position) => true),
                        (PixelBlock.GemstoneRed, 1, (player, position) => true),
                        (PixelBlock.GemstoneCyan, 1, (player, position) => true),
                        (PixelBlock.GemstoneWhite, 1, (player, position) => true),
                        (PixelBlock.GemstoneBlack, 1, (player, position) => true),
                    ],
                    Commands = Commands,
                }
            },
            {
                "void",
                new(
                    (world, action, actor, oldBlock, newBlock, position, health) =>
                    {
                        static (PixelBlock newBlock, float health) HandleDamage(
                            Actor actor,
                            PixelBlock oldBlock,
                            float health,
                            float power
                        )
                        {
                            health -= power;
                            if (health <= 0)
                            {
                                return (PixelBlock.Empty, 0.0f);
                            }
                            return (oldBlock, health);
                        }

                        return action switch
                        {
                            ActionType.Reveal => (
                                newBlock == PixelBlock.BasicBlack ? PixelBlock.Empty : newBlock,
                                5.0f
                            ),
                            ActionType.Mine => HandleDamage(
                                actor,
                                oldBlock,
                                health,
                                actor.GetPower / 2
                            ),
                            ActionType.Drill => HandleDamage(
                                actor,
                                oldBlock,
                                health,
                                actor.GetPower * 2
                            ),
                            _ => (oldBlock, health),
                        };
                    }
                )
                {
                    Name = "Void",
                    Ground = PixelBlock.BasicBlack,
                    BlockState = new (PixelBlock, float health)[400, 360],
                    AirHeight = 40,
                    Blocks =
                    [
                        (PixelBlock.BasicBlack, 1000, (player, position) => true),
                        (PixelBlock.BasicRed, 5, (player, position) => true),
                        (PixelBlock.BasicBlue, 5, (player, position) => true),
                        (PixelBlock.BasicGreen, 5, (player, position) => true),
                        (PixelBlock.BasicYellow, 5, (player, position) => true),
                        (PixelBlock.BasicCyan, 5, (player, position) => true),
                    ],
                    Commands = Commands,
                }
            },
        };

        public static readonly CaseInsensitiveDictionary<DigbotItem> Items = new()
        {
            {
                "StaticPower",
                new HiddenDigbotItem() { PowerBoost = (1f, 0f) }
            },
            {
                "RelativePower",
                new HiddenDigbotItem() { PowerBoost = (0f, 1f) }
            },
            {
                "StaticHealth",
                new HiddenDigbotItem() { HealthBoost = (1f, 0f) }
            },
            {
                "RelativeHealth",
                new HiddenDigbotItem() { HealthBoost = (0f, 1f) }
            },
            {
                "StaticLuck",
                new HiddenDigbotItem() { LuckBoost = (1f, 0f) }
            },
            {
                "RelativeLuck",
                new HiddenDigbotItem() { LuckBoost = (0f, 1f) }
            },
            {
                "StaticPerception",
                new HiddenDigbotItem() { PerceptionBoost = 1 }
            },
            {
                "GoldGain",
                new HiddenDigbotItem() { GoldChange = 1f }
            },
            {
                "healthPotion0",
                new()
                {
                    Name = "Small Health Potion",
                    Description = "Restores 20 Hp",
                    Use = player =>
                    {
                        if (Items != null)
                        {
                            player.RemoveItems(Items["healthPotion0"]);
                            player.Health += 20f;
                        }
                    },
                    Gold = 10f,
                }
            },
            {
                "healthPotion1",
                new()
                {
                    Name = "Health Potion",
                    Description = "Restores 50 Hp",
                    Use = player =>
                    {
                        if (Items != null)
                        {
                            player.RemoveItems(Items["healthPotion1"]);
                            player.Health += 50f;
                        }
                    },
                    Gold = 25f,
                }
            },
            {
                "healthPotion2",
                new()
                {
                    Name = "Large Health Potion",
                    Description = "Restores 100 Hp",
                    Use = player =>
                    {
                        if (Items != null)
                        {
                            player.RemoveItems(Items["healthPotion2"]);
                            player.Health += 100f;
                        }
                    },
                    Gold = 50f,
                }
            },
            {
                "saphiriteOre",
                new()
                {
                    Name = "Saphirite Ore",
                    Description = "A chunk of saphirite ore",
                    Gold = 5f,
                }
            },
            {
                "stiratiteOre",
                new()
                {
                    Name = "Stiratite Ore",
                    Description = "A chunk of stiratite ore",
                    Gold = 5f,
                }
            },
            {
                "crotinniumOre",
                new()
                {
                    Name = "Crotinnium Ore",
                    Description = "A chunk of crotinnium ore",
                    Gold = 5f,
                }
            },
            {
                "jivoliteOre",
                new()
                {
                    Name = "Jivolite Ore",
                    Description = "A chunk of jivolite ore",
                    Gold = 5f,
                }
            },
            {
                "crushedSaphirite",
                new()
                {
                    Name = "Crushed Saphirite",
                    Description = "Crushed saphirite ore",
                    Gold = 10f,
                }
            },
            {
                "crushedStiratite",
                new()
                {
                    Name = "Crushed Stiratite",
                    Description = "Crushed stiratite ore",
                    Gold = 10f,
                }
            },
            {
                "crushedCrotinnium",
                new()
                {
                    Name = "Crushed Crotinnium",
                    Description = "Crushed crotinnium ore",
                    Gold = 10f,
                }
            },
            {
                "crushedJivolite",
                new()
                {
                    Name = "Crushed Jivolite",
                    Description = "Crushed jivolite ore",
                    Gold = 10f,
                }
            },
        };
        public static readonly CaseInsensitiveDictionary<(
            DigbotItem Normal,
            DigbotItem Equipped
        )> EquippedItems = [];

        public static void Initialize()
        {
            EquippedItems.Add(
                "starterPickaxe",
                GenerateTool(
                    "Starter Pickaxe",
                    "A basic pickaxe",
                    1,
                    (ItemType.Tool, ActionType.Drill),
                    null,
                    new() { PowerBoost = (1f, 0) },
                    null
                )
            );

            EquippedItems.Add(
                "starterDrill",
                GenerateTool(
                    "Starter Drill",
                    "A basic drill",
                    1,
                    (ItemType.Tool, ActionType.Drill),
                    null,
                    new() { PowerBoost = (2f, 0) },
                    null
                )
            );
        }

        public class PartialDigbotItem
        {
            public (float a, float r) PowerBoost;
            public (float a, float r) HealthBoost;
            public (float a, float r) LuckBoost;
            public float GoldChange;
            public float Gold;
            public int PerceptionBoost;
            public DamageReduce FlatDefenseBoost = new();
            public DamageReduce PercentageDefenseBoost = new();
            public ItemLimits LimitBoost = new();

            public static PartialDigbotItem operator +(PartialDigbotItem a, PartialDigbotItem b)
            {
                return new()
                {
                    PowerBoost = (a.PowerBoost.a + b.PowerBoost.a, a.PowerBoost.r + b.PowerBoost.r),
                    HealthBoost = (
                        a.HealthBoost.a + b.HealthBoost.a,
                        a.HealthBoost.r + b.HealthBoost.r
                    ),
                    LuckBoost = (a.LuckBoost.a + b.LuckBoost.a, a.LuckBoost.r + b.LuckBoost.r),
                    PerceptionBoost = a.PerceptionBoost + b.PerceptionBoost,
                    FlatDefenseBoost = (DamageReduce)(a.FlatDefenseBoost + b.FlatDefenseBoost),
                    PercentageDefenseBoost = (DamageReduce)(
                        a.PercentageDefenseBoost + b.PercentageDefenseBoost
                    ),
                    GoldChange = a.GoldChange + b.GoldChange,
                    Gold = a.Gold + b.Gold,
                    LimitBoost = (ItemLimits)(a.LimitBoost + b.LimitBoost),
                };
            }
        }

        public static (DigbotItem Normal, DigbotItem Equipped) GenerateTool(
            string name,
            string description,
            int typeUse,
            (ItemType, ActionType?) type,
            PartialDigbotItem? passiveBoost,
            PartialDigbotItem? activeBoost,
            float? time
        )
        {
            passiveBoost ??= new();
            activeBoost ??= new();
            activeBoost += passiveBoost;
            DigbotItem normal = new()
            {
                Name = name,
                Description = description,
                Type = type,
                PowerBoost = passiveBoost.PowerBoost,
                HealthBoost = passiveBoost.HealthBoost,
                LuckBoost = passiveBoost.LuckBoost,
                PerceptionBoost = passiveBoost.PerceptionBoost,
                FlatDefenseBoost = passiveBoost.FlatDefenseBoost,
                PercentageDefenseBoost = passiveBoost.PercentageDefenseBoost,
                LimitBoost = passiveBoost.LimitBoost,
                GoldChange = passiveBoost.GoldChange,
                Gold = passiveBoost.Gold,
            };
            DigbotItem equipped = new()
            {
                Name = name,
                Description = description,
                TypeUse = typeUse,
                Type = type,
                Time = time ?? 0f,
                PowerBoost = activeBoost.PowerBoost,
                HealthBoost = activeBoost.HealthBoost,
                LuckBoost = activeBoost.LuckBoost,
                PerceptionBoost = activeBoost.PerceptionBoost,
                FlatDefenseBoost = activeBoost.FlatDefenseBoost,
                PercentageDefenseBoost = activeBoost.PercentageDefenseBoost,
                LimitBoost = activeBoost.LimitBoost,
                GoldChange = activeBoost.GoldChange,
                Gold = 0f,
            };
            normal.Use = player =>
            {
                if (player.ItemLimits[type.Item1] < typeUse)
                {
                    return;
                }
                player.RemoveItems(normal);
                player.AddItems(equipped);
            };
            equipped.Use = player =>
            {
                player.RemoveItems(equipped);
                player.AddItems(normal);
            };
            return (normal, equipped);
        }
    }
}
