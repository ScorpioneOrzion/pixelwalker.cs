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
            playerObj
        ) =>
        {
            client.Send(
                new PlayerDirectMessagePacket() { Message = message, TargetPlayerId = playerObj }
            );
        };
        public static readonly CaseInsensitiveDictionary<DigbotCommand> Commands = new()
        {
            {
                "reset",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
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
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        if (args.Length == 0)
                        {
                            var commands = world
                                .Commands.Where(command =>
                                    command.Value.Roles.Contains(player.Role)
                                )
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            SendDirectMessage(client, commandsList, playerObj.id);
                        }
                    },
                    LobbyCommand = true,
                    LobbyExecute = (args, player, playerObj, lobby) =>
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
                            SendDirectMessage(lobby, commandsList, playerObj.id);
                        }
                    },
                }
            },
            {
                "exit",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
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
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        if (
                            player is not DigbotPlayer entity
                            || Items is null
                            || args.Length == 0
                            || string.IsNullOrWhiteSpace(args[0])
                        )
                        {
                            return;
                        }
                        (float, ActionType)[]? result = null;
                        if (
                            Items.TryGetValue(args[0], out DigbotItem? item)
                            && item is not null
                            && entity.Inventory.ContainsKey(item)
                        )
                        {
                            result = item.Use(entity, ActionType.Use, playerObj.position);
                        }
                        if (result != null) { }
                    },
                }
            },
            {
                "equip",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        if (
                            player is not DigbotPlayer entity
                            || Items is null
                            || args.Length == 0
                            || string.IsNullOrWhiteSpace(args[0])
                        )
                        {
                            return;
                        }
                        (float, ActionType)[]? result = null;
                        if (
                            Items.TryGetValue(args[0], out DigbotItem? item)
                            && item is not null
                            && entity.Inventory.ContainsKey(item)
                        )
                        {
                            result = item.Use(entity, ActionType.Equip, playerObj.position);
                        }
                        if (result != null) { }
                    },
                }
            },
            {
                "buy",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        if (
                            player is not DigbotPlayer playerEntity
                            || Items is null
                            || args.Length == 0
                            || string.IsNullOrWhiteSpace(args[0])
                        )
                        {
                            return;
                        }

                        if (
                            !Items.TryGetValue(args[0], out DigbotItem? item)
                            || item is null
                            || item.Hidden
                        )
                        {
                            SendDirectMessage(client, "Unknown item", playerObj.id);
                            return;
                        }

                        if (item.Gold.a == 0f)
                        {
                            SendDirectMessage(client, "You can't sell that item", playerObj.id);
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
                    },
                }
            },
            {
                "sell",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        if (
                            player is not DigbotPlayer playerEntity
                            || Items is null
                            || args.Length == 0
                            || string.IsNullOrWhiteSpace(args[0])
                        )
                        {
                            return;
                        }

                        if (
                            !Items.TryGetValue(args[0], out DigbotItem? item)
                            || item is null
                            || item.Hidden
                        )
                        {
                            SendDirectMessage(client, "Unknown item", playerObj.id);
                            return;
                        }

                        if (item.Gold.a == 0f)
                        {
                            SendDirectMessage(client, "You can't sell that item", playerObj.id);
                            return;
                        }

                        if (!playerEntity.Inventory.ContainsKey(item))
                        {
                            SendDirectMessage(client, "You don't have that item", playerObj.id);
                            return;
                        }

                        if (args.Length == 1)
                        {
                            item.Sell(playerEntity);
                        }
                        else
                        {
                            item.Sell(playerEntity, args[1]);
                        }
                    },
                }
            },
            {
                "inventory",
                new() { Execute = (args, player, playerObj, client, world) => { } }
            },
        };
        public static readonly CaseInsensitiveDictionary<DigbotWorld> Worlds = new()
        {
            {
                "core",
                new(
                    (world, action, actor, block, blockData) =>
                    {
                        var defaultChanges = new[] { (block, 0f, blockData.x, blockData.y) };
                        if (actor is not Entity entity)
                        {
                            return defaultChanges;
                        }
                        var result = entity
                            .Inventory.AsParallel()
                            .SelectMany(item =>
                                item.Key.Use(entity, action, (blockData.x, blockData.y))
                                ?? Enumerable.Empty<(float, ActionType)>()
                            )
                            .ToArray();
                        // do stuff with result

                        return defaultChanges;
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
                        var defaultChanges = new[] { (block, 0f, blockData.x, blockData.y) };
                        if (actor is not Entity entity)
                        {
                            return defaultChanges;
                        }
                        var result = entity
                            .Inventory.AsParallel()
                            .SelectMany(item =>
                                item.Key.Use(entity, action, (blockData.x, blockData.y))
                                ?? Enumerable.Empty<(float, ActionType)>()
                            )
                            .ToArray();
                        // do stuff with result

                        return defaultChanges;
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
                "Gold",
                new HiddenDigbotItem() { Gold = (0f, 1f) }
            },
            {
                "StaticPower",
                new HiddenDigbotItem() { PowerBoost = (1f, 0f) }
            },
            {
                "RelativePower",
                new HiddenDigbotItem() { PowerBoost = (0f, 1f) }
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
                "RandomBoost",
                new DigbotItem()
                {
                    Name = "Random Boost",
                    Description = "Randomizes your stats can be good or bad",
                    Type = (ItemType.Miscellaneous, ActionType.Unknown),
                    Gold = (20f, 0f),
                    Use = (player, action, position) =>
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
                                            player.SetItems(Items["Gold"], 1);
                                            break;
                                        case 1:
                                            player.SetItems(Items["Gold"], -1);
                                            break;
                                        case 2:
                                            player.SetItems(Items["StaticPower"], 1);
                                            break;
                                        case 3:
                                            player.SetItems(Items["StaticPower"], -1);
                                            break;
                                        case 4:
                                            player.SetItems(Items["RelativePower"], 1);
                                            break;
                                        case 5:
                                            player.SetItems(Items["RelativePower"], -1);
                                            break;
                                        case 6:
                                            player.SetItems(Items["StaticLuck"], 1);
                                            break;
                                        case 7:
                                            player.SetItems(Items["StaticLuck"], -1);
                                            break;
                                        case 8:
                                            player.SetItems(Items["RelativeLuck"], 1);
                                            break;
                                        case 9:
                                            player.SetItems(Items["RelativeLuck"], -1);
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
                    Use = (player, action, position) =>
                    {
                        if (Items is null)
                            return null;
                        if (action == ActionType.AutoUse)
                        {
                            DigbotItem item = Items["PowerPotion"];
                            player.SetItems(item, -1);
                            int count = player.Inventory[item];
                            if (count > 0)
                            {
                                player.AddTimer(item, 60f);
                            }
                            else if (count < 0)
                            {
                                player.SetItems(item, -count);
                            }
                        }
                        return null;
                    },
                    PowerBoost = (2f, 0.1f),
                }
            },
            {
                "PowerPotion",
                new DigbotItem()
                {
                    Name = "Power Potion",
                    Description = "A potion that increases power temporarily",
                    Type = (ItemType.Miscellaneous, ActionType.Unknown),
                    Use = (player, action, position) =>
                    {
                        if (Items is null)
                            return null;
                        if (action == ActionType.Use)
                        {
                            if (player.Inventory.ContainsKey(Items["PowerEffect"]))
                            {
                                return null;
                            }
                            player.SetItems(Items["PowerPotion"], -1);
                            player.SetItems(Items["PowerEffect"], 1);

                            player.AddTimer(Items["PowerEffect"], 60f);
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
                    new() { PowerBoost = (1f, 0) }
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
                    new() { PowerBoost = (2f, 0) }
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
                    new() { PowerBoost = (2f, 0) }
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
                    new() { PowerBoost = (4f, 0) }
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
            public Func<Entity, ActionType, (int x, int y)?, (float, ActionType)[]?> Use = (
                player,
                action,
                position
            ) =>
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
            PartialDigbotItem? activeBoost
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
                PowerBoost = activeBoost.PowerBoost,
                LuckBoost = activeBoost.LuckBoost,
                PerceptionBoost = activeBoost.PerceptionBoost,
                LimitBoost = activeBoost.LimitBoost,
                Gold = (0f, activeBoost.Gold.d),
                Use = activeBoost.Use,
            };
            normal.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip && player.ItemLimits[type.Item1] >= typeUse)
                {
                    player.SetItems(normal, -1);
                    player.SetItems(equipped, 1);
                }
                return null;
            };
            equipped.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    player.SetItems(equipped, -1);
                    player.SetItems(normal, 1);
                }
                return null;
            };
            return (normal, equipped);
        }
    }
}
