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
                    LobbyCommand = true,
                    LobbyExecute = (args, player, lobby) =>
                    {
                        if (args.Length == 0)
                        {
                            if (Commands is null)
                            {
                                return;
                            }
                            var commands = Commands
                                .Where(command => command.Value.Roles.Contains(player.Role))
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            lobby.Send(
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
                    (world, action, actor, block, blockData) =>
                    {
                        if (actor is Entity entity)
                        {
                            var result = entity
                                .Inventory.ToArray()
                                .AsParallel()
                                .SelectMany(item =>
                                {
                                    // Assuming item.Key.Use returns (float, ActionType)[]?
                                    var useResult = item.Key.Use(entity, action);
                                    if (useResult != null)
                                    {
                                        return useResult;
                                    }
                                    return [];
                                })
                                .ToArray();
                            // do stuff with result
                        }
                        return [(block, 0f, blockData.x, blockData.y)];
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
                    Difficulty = 1,
                }
            },
            {
                "void",
                new(
                    (world, action, actor, block, blockData) =>
                    {
                        if (actor is Entity entity)
                        {
                            var result = entity
                                .Inventory.ToArray()
                                .AsParallel()
                                .SelectMany(item =>
                                {
                                    // Assuming item.Key.Use returns (float, ActionType)[]?
                                    var useResult = item.Key.Use(entity, action);
                                    if (useResult != null)
                                    {
                                        return useResult;
                                    }
                                    return [];
                                })
                                .ToArray();
                            // do stuff with result
                        }
                        return [(block, 0f, blockData.x, blockData.y)];
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
                    Difficulty = 1,
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

            Dictionary<string, object> ores = new()
            {
                ["saphirite"] = new() { },
                ["stiratite"] = new() { },
                ["crotinnium"] = new() { },
                ["jivolite"] = new() { },
                ["karnite"] = new() { },
                ["xenotite"] = new() { },
                ["zorium"] = new() { },
            };

            string[] oreNames = [.. ores.Keys];
            for (int i = 0; i < oreNames.Length; i++)
            {
                // Ore
                Items.Add(
                    $"{oreNames[i]}Ore",
                    new()
                    {
                        Name = $"{oreNames[i][..1].ToUpper()}{oreNames[i][1..]} Ore",
                        Description = $"A chunk of {oreNames[i]} ore",
                    }
                );

                // Ingot
                Items.Add(
                    $"{oreNames[i]}Ingot",
                    new()
                    {
                        Name = $"{oreNames[i][..1].ToUpper()}{oreNames[i][1..]} Ingot",
                        Description = $"A bar of {oreNames[i]} ingot",
                    }
                );

                // Pickaxe
                EquippedItems.Add(
                    $"{oreNames[i]}Pickaxe",
                    GenerateTool(
                        $"{oreNames[i][..1].ToUpper()}{oreNames[i][1..]} Pickaxe",
                        $"A pickaxe made of {oreNames[i]}",
                        1,
                        (ItemType.Tool, ActionType.Mine),
                        null,
                        new() { PowerBoost = (5f, 0) },
                        null
                    )
                );

                // Drill
                EquippedItems.Add(
                    $"{oreNames[i]}Drill",
                    GenerateTool(
                        $"{oreNames[i][..1].ToUpper()}{oreNames[i][1..]} Drill",
                        $"A drill made of {oreNames[i]}",
                        1,
                        (ItemType.Tool, ActionType.Drill),
                        null,
                        new() { PowerBoost = (10f, 0) },
                        null
                    )
                );

                // ore should cost 5
                Items[$"{oreNames[i]}Ore"].Gold.a = 5f;
                // ingot should cost 10
                Items[$"{oreNames[i]}Ingot"].Gold.a = 10f;
            }
        }

        public class PartialDigbotItem
        {
            public (float a, float r) PowerBoost;
            public (float a, float r) LuckBoost;
            public (float a, float d) Gold = (0f, 0f);
            public int PerceptionBoost;
            public ItemLimits LimitBoost = new();
            public Func<Entity, ActionType, (float, ActionType)[]?> Use = (player, action) =>
            {
                return null;
            };

            public static PartialDigbotItem operator +(PartialDigbotItem a, PartialDigbotItem b)
            {
                return new()
                {
                    PowerBoost = (a.PowerBoost.a + b.PowerBoost.a, a.PowerBoost.r + b.PowerBoost.r),
                    LuckBoost = (a.LuckBoost.a + b.LuckBoost.a, a.LuckBoost.r + b.LuckBoost.r),
                    PerceptionBoost = a.PerceptionBoost + b.PerceptionBoost,
                    Gold = (a.Gold.a + b.Gold.a, a.Gold.d + b.Gold.d),
                    LimitBoost = (ItemLimits)(a.LimitBoost + b.LimitBoost),
                    Use = a.Use + b.Use,
                };
            }

            public static PartialDigbotItem operator *(PartialDigbotItem a, float b)
            {
                return new()
                {
                    PowerBoost = (a.PowerBoost.a * b, a.PowerBoost.r * b),
                    LuckBoost = (a.LuckBoost.a * b, a.LuckBoost.r * b),
                    PerceptionBoost = (int)(a.PerceptionBoost * b),
                    Gold = (a.Gold.a * b, a.Gold.d * b),
                    LimitBoost = (ItemLimits)(a.LimitBoost * b),
                    Use = a.Use,
                };
            }

            public static PartialDigbotItem operator *(float a, PartialDigbotItem b)
            {
                return b * a;
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
                LuckBoost = passiveBoost.LuckBoost,
                PerceptionBoost = passiveBoost.PerceptionBoost,
                LimitBoost = passiveBoost.LimitBoost,
                Gold = passiveBoost.Gold,
                Use = passiveBoost.Use,
            };
            DigbotItem equipped = new()
            {
                Name = name,
                Description = description,
                TypeUse = typeUse,
                Type = type,
                Time = time ?? 0f,
                PowerBoost = activeBoost.PowerBoost,
                LuckBoost = activeBoost.LuckBoost,
                PerceptionBoost = activeBoost.PerceptionBoost,
                LimitBoost = activeBoost.LimitBoost,
                Gold = (0f, activeBoost.Gold.d),
                Use = activeBoost.Use,
            };
            normal.Use += (player, action) =>
            {
                if (action == ActionType.Equip && player.ItemLimits[type.Item1] >= typeUse)
                {
                    player.RemoveItems(normal);
                    player.AddItems(equipped);
                }
                return null;
            };
            equipped.Use += (player, action) =>
            {
                if (action == ActionType.Equip)
                {
                    player.RemoveItems(equipped);
                    player.AddItems(normal);
                }
                return null;
            };
            return (normal, equipped);
        }
    }
}
