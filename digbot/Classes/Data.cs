using PixelPilot.Client;
using PixelPilot.Client.World.Constants;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class Registry
    {
        private static readonly Action<PixelPilotClient, string, int> SendDirectMessage = static (
            client,
            message,
            playerId
        ) =>
        {
            client.Send(
                new PlayerDirectMessagePacket() { Message = message, TargetPlayerId = playerId }
            );
        };
        public static readonly CaseInsensitiveDictionary<DigbotCommand> Commands = new()
        {
            {
                "reset",
                new()
                {
                    Execute = (args, player, playerId, client, world) =>
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
                    Execute = (args, player, playerId, client, world) =>
                    {
                        if (args.Length == 0)
                        {
                            var commands = world
                                .Commands.Where(command =>
                                    command.Value.Roles.Contains(player.Role)
                                )
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            SendDirectMessage(client, commandsList, playerId);
                        }
                    },
                    LobbyCommand = true,
                    LobbyExecute = (args, player, playerId, lobby) =>
                    {
                        if (args.Length == 0)
                        {
                            if (Commands is null)
                            {
                                return;
                            }
                            var commands = Commands
                                .Where(command =>
                                    command.Value.Roles.Contains(player.Role)
                                    && command.Value.LobbyCommand
                                )
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            SendDirectMessage(lobby, commandsList, playerId);
                        }
                    },
                }
            },
            {
                "exit",
                new()
                {
                    Execute = (args, player, playerId, client, world) =>
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
                    Execute = (args, player, playerId, client, world) =>
                    {
                        if (player is DigbotPlayer playerEntity)
                        {
                            var result = playerEntity
                                .Inventory.Where(Items => Items.Key.Name == args[0])
                                .First()
                                .Key.Use(playerEntity, ActionType.Use);

                            if (result != null)
                            {
                                // do stuff with result
                            }
                        }
                    },
                }
            },
            {
                "equip",
                new()
                {
                    Execute = (args, player, playerId, client, world) =>
                    {
                        if (player is Entity entity)
                        {
                            entity
                                .Inventory.Where(Items => Items.Key.Name == args[0])
                                .First()
                                .Key.Use(entity, ActionType.Equip);
                        }
                    },
                }
            },
            {
                "buy",
                new()
                {
                    Execute = (args, player, playerId, client, world) =>
                    {
                        if (player is DigbotPlayer playerEntity)
                        {
                            if (Items is null)
                            {
                                return;
                            }
                            DigbotItem? item = Items[args[0]];
                            if (item is not null)
                            {
                                if (item.Gold.a == 0f)
                                {
                                    SendDirectMessage(client, "You can't buy that item", playerId);
                                    return;
                                }
                                if (item.Hidden)
                                {
                                    SendDirectMessage(client, "Unknown item", playerId);
                                    return;
                                }
                                if (args.Length == 1)
                                {
                                    item.Buy(playerEntity);
                                }
                                else
                                {
                                    item.Buy(playerEntity, args[1]);
                                }
                            }
                            else
                            {
                                SendDirectMessage(client, "Unknown item", playerId);
                            }
                        }
                    },
                }
            },
            {
                "sell",
                new()
                {
                    Execute = (args, player, playerId, client, world) =>
                    {
                        if (player is DigbotPlayer playerEntity)
                        {
                            if (Items is null)
                            {
                                return;
                            }
                            DigbotItem? item = Items[args[0]];
                            if (item is not null)
                            {
                                if (item.Gold.a == 0f)
                                {
                                    SendDirectMessage(client, "You can't sell that item", playerId);
                                    return;
                                }
                                if (item.Hidden)
                                {
                                    SendDirectMessage(client, "Unknown item", playerId);
                                    return;
                                }
                                if (playerEntity.Inventory.ContainsKey(item))
                                {
                                    if (args.Length == 1)
                                    {
                                        item.Sell(playerEntity);
                                    }
                                    else
                                    {
                                        item.Sell(playerEntity, args[1]);
                                    }
                                }
                                else
                                {
                                    SendDirectMessage(client, "You don't have that item", playerId);
                                }
                            }
                            else
                            {
                                SendDirectMessage(client, "Unknown item", playerId);
                            }
                        }
                    },
                }
            },
            {
                "inventory",
                new() { Execute = (args, player, playerId, client, world) => { } }
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
                "GoldGain",
                new HiddenDigbotItem() { Gold = (0f, 1f) }
            },
            {
                "GoldLoss",
                new HiddenDigbotItem() { Gold = (0f, -1f) }
            },
            {
                "StaticPowerAdd",
                new HiddenDigbotItem() { PowerBoost = (1f, 0f) }
            },
            {
                "StaticPowerRemove",
                new HiddenDigbotItem() { PowerBoost = (-1f, 0f) }
            },
            {
                "RelativePowerAdd",
                new HiddenDigbotItem() { PowerBoost = (0f, 1f) }
            },
            {
                "RelativePowerRemove",
                new HiddenDigbotItem() { PowerBoost = (0f, -1f) }
            },
            {
                "StaticLuckAdd",
                new HiddenDigbotItem() { LuckBoost = (1f, 0f) }
            },
            {
                "StaticLuckRemove",
                new HiddenDigbotItem() { LuckBoost = (-1f, 0f) }
            },
            {
                "RelativeLuckAdd",
                new HiddenDigbotItem() { LuckBoost = (0f, 1f) }
            },
            {
                "RelativeLuckRemove",
                new HiddenDigbotItem() { LuckBoost = (0f, -1f) }
            },
            {
                "RandomBoost",
                new DigbotItem()
                {
                    Name = "Random Boost",
                    Description = "Randomizes your stats can be good or bad",
                    Type = (ItemType.Miscellaneous, ActionType.Unknown),
                    Gold = (20f, 0f),
                    Use = (player, action) =>
                    {
                        if (Items is null)
                            return null;
                        if (action == ActionType.Use)
                        {
                            player.RemoveItems(Items["RandomBoost"]);
                            if (player is DigbotPlayer digbotPlayer)
                            {
                                foreach (int x in Enumerable.Range(0, 10))
                                {
                                    int random = digbotPlayer.RandomGenerator.Next(0, 10);
                                    switch (random)
                                    {
                                        case 0:
                                            player.AddItems(Items["GoldGain"]);
                                            break;
                                        case 1:
                                            player.AddItems(Items["GoldLoss"]);
                                            break;
                                        case 2:
                                            player.AddItems(Items["StaticPowerAdd"]);
                                            break;
                                        case 3:
                                            player.AddItems(Items["StaticPowerRemove"]);
                                            break;
                                        case 4:
                                            player.AddItems(Items["RelativePowerAdd"]);
                                            break;
                                        case 5:
                                            player.AddItems(Items["RelativePowerRemove"]);
                                            break;
                                        case 6:
                                            player.AddItems(Items["StaticLuckAdd"]);
                                            break;
                                        case 7:
                                            player.AddItems(Items["StaticLuckRemove"]);
                                            break;
                                        case 8:
                                            player.AddItems(Items["RelativeLuckAdd"]);
                                            break;
                                        case 9:
                                            player.AddItems(Items["RelativeLuckRemove"]);
                                            break;
                                    }
                                }
                            }
                        }
                        return null;
                    },
                }
            },
            {
                "PowerEffect",
                new HiddenDigbotItem()
                {
                    Use = (player, action) =>
                    {
                        if (Items is null)
                            return null;
                        if (action == ActionType.AutoUse)
                        {
                            player.RemoveItems(Items["PowerPotion"]);
                        }
                        return null;
                    },
                    PowerBoost = (2f, 0.1f),
                    Time = 60f,
                }
            },
            {
                "PowerPotion",
                new DigbotItem()
                {
                    Name = "Power Potion",
                    Description = "A potion that increases power temporarily",
                    Type = (ItemType.Miscellaneous, ActionType.Unknown),
                    Use = (player, action) =>
                    {
                        if (Items is null)
                            return null;
                        if (action == ActionType.Use)
                        {
                            if (player.Inventory.ContainsKey(Items["PowerEffect"]))
                            {
                                return null;
                            }
                            player.RemoveItems(Items["PowerPotion"]);
                            player.AddItems(Items["PowerEffect"]);
                        }
                        return null;
                    },
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
                GenerateEquipment(
                    "Starter Pickaxe",
                    "A basic pickaxe",
                    1,
                    (ItemType.Tool, ActionType.Mine),
                    null,
                    new() { PowerBoost = (1f, 0) },
                    null
                )
            );

            EquippedItems.Add(
                "starterDrill",
                GenerateEquipment(
                    "Starter Drill",
                    "A basic drill",
                    1,
                    (ItemType.Tool, ActionType.Mine),
                    null,
                    new() { PowerBoost = (2f, 0) },
                    null
                )
            );

            EquippedItems.Add(
                "advancedPickaxe",
                GenerateEquipment(
                    "Advanced Pickaxe",
                    "An advanced pickaxe",
                    1,
                    (ItemType.Tool, ActionType.Mine),
                    null,
                    new() { PowerBoost = (2f, 0) },
                    null
                )
            );

            EquippedItems.Add(
                "advancedDrill",
                GenerateEquipment(
                    "Advanced Drill",
                    "An advanced drill",
                    1,
                    (ItemType.Tool, ActionType.Mine),
                    null,
                    new() { PowerBoost = (4f, 0) },
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
                        Name = $"{Capitalize(oreNames[i])} Ore",
                        Description = $"A chunk of {oreNames[i]} ore",
                        Gold = (5f, 0f),
                    }
                );

                // Ingot
                Items.Add(
                    $"{oreNames[i]}Ingot",
                    new()
                    {
                        Name = $"{Capitalize(oreNames[i])} Ingot",
                        Description = $"A bar of {oreNames[i]} ingot",
                        Gold = (10f, 0f),
                    }
                );
            }

            Items.Add(
                "Coal",
                new()
                {
                    Name = "Coal",
                    Description = "A chunk of coal",
                    Gold = (1f, 0f),
                }
            );
        }

        private static string Capitalize(string str) => $"{str[..1].ToUpper()}{str[1..]}";

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

            public static PartialDigbotItem operator +(PartialDigbotItem a, PartialDigbotItem b) =>
                new()
                {
                    PowerBoost = (a.PowerBoost.a + b.PowerBoost.a, a.PowerBoost.r + b.PowerBoost.r),
                    LuckBoost = (a.LuckBoost.a + b.LuckBoost.a, a.LuckBoost.r + b.LuckBoost.r),
                    PerceptionBoost = a.PerceptionBoost + b.PerceptionBoost,
                    Gold = (a.Gold.a + b.Gold.a, a.Gold.d + b.Gold.d),
                    LimitBoost = a.LimitBoost + b.LimitBoost,
                    Use = a.Use + b.Use,
                };
        }

        public static (DigbotItem Normal, DigbotItem Equipped) GenerateEquipment(
            string name,
            string description,
            int typeUse,
            (ItemType, ActionType) type,
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
