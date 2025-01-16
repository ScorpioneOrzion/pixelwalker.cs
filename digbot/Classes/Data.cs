using System.Text.Json;
using PixelPilot.Client;
using PixelPilot.Client.World.Constants;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class Registry
    {
        private static readonly Dictionary<string, DigbotPlayerRole> setRoles = new()
        {
            { "DIGBOT", DigbotPlayerRole.DIGBOT },
            { "SCORPIONEORZION", DigbotPlayerRole.Owner },
            { "MARTEN", DigbotPlayerRole.GameDeveloper },
            { "JOHN", DigbotPlayerRole.GameAdmin },
            { "ANATOLY", DigbotPlayerRole.GameDeveloper },
            { "PRIDDLE", DigbotPlayerRole.GameAdmin },
            { "REALMS", DigbotPlayerRole.GameBot },
        };

        public static void SaveFileJson(Dictionary<string, DigbotPlayer> players)
        {
            var usedItemKeys = players
                .Values.SelectMany(p => p.Inventory.Keys)
                .Select(p => p.ItemKey)
                .Distinct()
                .ToList();

            var playersToSerialize = players.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    kvp.Value.Gold,
                    Inventory = kvp.Value.Inventory.ToDictionary(
                        itemKvp => usedItemKeys.IndexOf(itemKvp.Key.ItemKey),
                        itemKvp => itemKvp.Value
                    ),
                }
            );

            var saveData = new { UsedItemKeys = usedItemKeys, Players = playersToSerialize };

            string timestampedFile = $"digbot_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            string json = JsonSerializer.Serialize(saveData);
            File.WriteAllText(timestampedFile, json);
            File.Replace(timestampedFile, "digbot.json", null);
        }

        public static Dictionary<string, DigbotPlayer> LoadFileJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var saveData = JsonSerializer.Deserialize<SaveData>(json);

            var players = new Dictionary<string, DigbotPlayer>();

            if (saveData != null)
                foreach (var kvp in saveData.Players)
                {
                    var inventory = kvp.Value.Inventory.ToDictionary(
                        itemKvp => Items[saveData.UsedItemKeys[itemKvp.Key]], // Map index to item key
                        itemKvp => itemKvp.Value
                    );

                    DigbotPlayer player;

                    if (SetRoles.TryGetValue(kvp.Key, out var role))
                    {
                        player = new DigbotPlayer(inventory, kvp.Value.Gold) { Role = role };
                    }
                    else
                    {
                        player = new DigbotPlayer(inventory, kvp.Value.Gold)
                        {
                            Role = DigbotPlayerRole.None,
                        };
                    }

                    players[kvp.Key] = player;
                }

            return players;
        }

        public class SaveData
        {
            public required List<string> UsedItemKeys { get; set; }
            public required Dictionary<string, PlayerData> Players { get; set; }
        }

        public class PlayerData
        {
            public float Gold { get; set; }
            public required Dictionary<int, int> Inventory { get; set; } // Inventory using indices
        }

        public static readonly Action<PixelPilotClient, string, int> SDM = (c, m, p) =>
            c.Send(new PlayerChatPacket() { Message = $"/dm #{p} {m}" });
        public static readonly Dictionary<string, DigbotCommand> Commands = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            {
                "reset",
                new()
                {
                    Execute = (args, player, playerObj, client, world) =>
                    {
                        world.Reset(client);
                    },
                    Role = DigbotPlayerRole.Owner,
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
                                .Commands.Where(command => command.Value.Role <= player.Role)
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            SDM(client, commandsList, playerObj.id);
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
                                    command.Value.Role <= player.Role && command.Value.LobbyCommand
                                )
                                .Select(command => command.Key);
                            var commandsList = string.Join(", ", commands);
                            SDM(lobby, commandsList, playerObj.id);
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
                    LobbyCommand = true,
                    LobbyExecute = (args, player, playerObj, lobby) =>
                    {
                        lobby.Disconnect();
                    },
                    Role = DigbotPlayerRole.Owner,
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
                        (Stats, List<string>)? result = null;
                        if (
                            Items.TryGetValue(args[0], out DigbotItem? item)
                            && item is not null
                            && entity.Inventory.ContainsKey(item)
                        )
                        {
                            result = item.Use(entity, ActionType.Use, playerObj.position);
                        }
                        if (result is (Stats stats, List<string> messages)) { }
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
                        (Stats, List<string>)? result = null;
                        if (
                            Items.TryGetValue(args[0], out DigbotItem? item)
                            && item is not null
                            && entity.Inventory.ContainsKey(item)
                        )
                        {
                            result = item.Use(entity, ActionType.Equip, playerObj.position);
                        }
                        if (result is (Stats stats, List<string> messages))
                        {
                            if (messages.Any(message => message == "ERROR: Can't equip"))
                            {
                                SDM(
                                    client,
                                    "You can't equip that, unequip something first",
                                    playerObj.id
                                );
                            }
                        }
                    },
                }
            },
            {
                "craft",
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

                        var item = Items.Values.FirstOrDefault(i =>
                            i.Name.Equals(args[0], StringComparison.CurrentCultureIgnoreCase)
                        );
                        if (item is null || item.Hidden)
                        {
                            SDM(client, "Unknown item", playerObj.id);
                            return;
                        }

                        if (!item.CraftAble)
                        {
                            SDM(client, "You can't craft that item", playerObj.id);
                            return;
                        }

                        bool canCraft = true;
                        List<(DigbotItem Item, int MissingAmount)> missingItems = [];

                        (DigbotItem, int)[] costs = item.Craft;

                        foreach (var (itemPart, amount) in costs)
                        {
                            if (
                                !playerEntity.Inventory.TryGetValue(itemPart, out int currentAmount)
                                || currentAmount < amount
                            )
                            {
                                int missingAmount =
                                    amount - (currentAmount >= 0 ? currentAmount : 0); // Calculate missing amount
                                missingItems.Add((itemPart, missingAmount));
                                canCraft = false;
                            }
                        }

                        if (canCraft)
                        {
                            foreach (var (itemPart, amount) in costs)
                            {
                                playerEntity.Inventory[itemPart] -= amount;
                            }
                            SDM(
                                client,
                                $"Crafting successful! You crafted: {item.Name}",
                                playerObj.id
                            );
                            playerEntity.Inventory[item]++;
                        }
                        else
                        {
                            SDM(
                                client,
                                "Crafting failed. You are missing the following items:",
                                playerObj.id
                            );
                            foreach (var (missingItem, missingAmount) in missingItems)
                            {
                                SDM(
                                    client,
                                    $"{missingAmount} x {Items.Values.FirstOrDefault(i => i.Name.Equals(missingItem.Name, StringComparison.CurrentCultureIgnoreCase))}",
                                    playerObj.id
                                );
                            }
                        }
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

                        var item = Items.Values.FirstOrDefault(i => i.Name == args[0]);
                        if (item is null || item.Hidden)
                        {
                            SDM(client, "Unknown item", playerObj.id);
                            return;
                        }

                        if (!item.BuyAble)
                        {
                            SDM(client, "You can't sell that item", playerObj.id);
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

                        var item = Items.Values.FirstOrDefault(i => i.Name == args[0]);
                        if (item is null || item.Hidden)
                        {
                            SDM(client, "Unknown item", playerObj.id);
                            return;
                        }

                        if (!item.BuyAble)
                        {
                            SDM(client, "You can't sell that item", playerObj.id);
                            return;
                        }

                        if (!playerEntity.Inventory.TryGetValue(item, out int value) || value <= 0)
                        {
                            SDM(client, "You don't have that item", playerObj.id);
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
        public static readonly Dictionary<string, DigbotWorld> Worlds = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            {
                "core",
                new(
                    (world, action, actor, block, blockData) =>
                    {
                        if (actor == world)
                        {
                            return [(block, 5f * blockData.y, blockData.x, blockData.y)];
                        }
                        if (actor is not Entity entity)
                        {
                            return [];
                        }
                        var result = entity.Use(action, (blockData.x, blockData.y));

                        if (result is (Stats stats, List<string> messages)) { }

                        return [];
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
                    (world, action, actor, block, blockData) =>
                    {
                        if (actor == world)
                        {
                            return [(block, 5f * blockData.y, blockData.x, blockData.y)];
                        }
                        if (actor is not Entity entity)
                        {
                            return [];
                        }
                        var result = entity.Use(action, (blockData.x, blockData.y));

                        if (result is (Stats stats, List<string> messages)) { }

                        return [];
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

        public static readonly Dictionary<string, DigbotItem> Items = new(
            StringComparer.OrdinalIgnoreCase
        );

        public static Dictionary<string, DigbotPlayerRole> SetRoles
        {
            get => setRoles;
        }

        public static void Initialize()
        {
            AddPickaxe("pickaxe0Unequiped", "pickaxe0Equiped", ItemType.Tool, 1f, 1);
            AddPickaxe("pickaxe1Unequiped", "pickaxe1Equiped", ItemType.Tool, 2.25f, 2);
            AddPickaxe("pickaxe2Unequiped", "pickaxe2Equiped", ItemType.Tool, 3.5f, 3);
            AddPickaxe("pickaxe3Unequiped", "pickaxe3Equiped", ItemType.Tool, 6.0f, 5);
            AddPickaxe("pickaxe4Unequiped", "pickaxe4Equiped", ItemType.Tool, 9.75f, 8);
            AddPickaxe("pickaxe5Unequiped", "pickaxe5Equiped", ItemType.Tool, 16.0f, 13);
            AddDrill("drill0Unequiped", "drill0Equiped", ItemType.Tool, 2f, 2);
            AddDrill("drill1Unequiped", "drill1Equiped", ItemType.Tool, 4.5f, 4);
            AddDrill("drill2Unequiped", "drill2Equiped", ItemType.Tool, 7f, 6);
            AddDrill("drill3Unequiped", "drill3Equiped", ItemType.Tool, 12f, 10);
            AddDrill("drill4Unequiped", "drill4Equiped", ItemType.Tool, 19.5f, 16);
            AddDrill("drill5Unequiped", "drill5Equiped", ItemType.Tool, 32f, 26);

            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "AbsoluteStrength",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { AbsoluteStrength = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
            );
            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "RelativeStrength",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { RelativeStrength = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
            );
            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "AbsoluteLuck",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { AbsoluteLuck = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
            );
            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "RelativeLuck",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { RelativeLuck = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
            );
            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "AbsolutePerception",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { AbsolutePerception = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
            );
            AddItem(
                new HiddenDigbotItem()
                {
                    ItemKey = "RelativePerception",
                    Use = (entity, action, position) =>
                    {
                        return action switch
                        {
                            ActionType.Mine or ActionType.Drill => (
                                new() { RelativePerception = 0.01f },
                                []
                            ),
                            _ => null,
                        };
                    },
                }
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
                AddItem(
                    new()
                    {
                        ItemKey = $"{oreNames[i]}Ore",
                        Name = $"{Capitalize(oreNames[i])} Ore",
                        Description = $"A chunk of {oreNames[i]} ore",
                        Cost = 5f,
                    }
                );

                // Ingot
                AddItem(
                    new()
                    {
                        ItemKey = $"{oreNames[i]}Ingot",
                        Name = $"{Capitalize(oreNames[i])} Ingot",
                        Description = $"A bar of {oreNames[i]} ingot",
                        Cost = 10f,
                    }
                );
            }

            AddItem(
                new()
                {
                    ItemKey = "Coal",
                    Name = "Coal",
                    Description = "A chunk of coal",
                    Cost = 1f,
                }
            );
        }

        private static string Capitalize(string str) => $"{str[..1].ToUpper()}{str[1..]}";

        private static void AddItem(DigbotItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemKey))
                throw new ArgumentException("Invalid item or ItemKey");

            if (!Items.ContainsKey(item.ItemKey))
            {
                Items.Add(item.ItemKey, item);
            }
        }

        private static void AddPickaxe(
            string unequippedName,
            string equippedName,
            ItemType type,
            float powerBoost,
            int typeUse = 1
        )
        {
            DigbotItem unequippedItem = new() { Type = type, ItemKey = unequippedName };
            DigbotItem equippedItem = new()
            {
                Type = type,
                TypeUse = typeUse,
                ItemKey = equippedName,
            };

            unequippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    if (player.ItemLimits[equippedItem.Type] < equippedItem.TypeUse)
                    {
                        return (new Stats(), ["ERROR: Can't equip"]);
                    }
                    player.SetItems(equippedItem, 1);
                    player.SetItems(unequippedItem, -1);
                }
                return null;
            };

            equippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    player.SetItems(unequippedItem, 1);
                    player.SetItems(equippedItem, -1);
                }
                else if (action == ActionType.Mine)
                {
                    return (new Stats() { AbsoluteStrength = powerBoost }, []);
                }
                return null;
            };

            AddItem(unequippedItem);
            AddItem(equippedItem);
        }

        private static void AddDrill(
            string unequippedName,
            string equippedName,
            ItemType type,
            float powerBoost,
            int typeUse = 1
        )
        {
            DigbotItem unequippedItem = new() { Type = type, ItemKey = unequippedName };
            DigbotItem equippedItem = new()
            {
                Type = type,
                TypeUse = typeUse,
                ItemKey = equippedName,
            };

            unequippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    if (player.ItemLimits[equippedItem.Type] < equippedItem.TypeUse)
                    {
                        return (new Stats(), ["ERROR: Can't equip"]);
                    }
                    player.SetItems(equippedItem, 1);
                    player.SetItems(unequippedItem, -1);
                }
                return null;
            };

            equippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    player.SetItems(unequippedItem, 1);
                    player.SetItems(equippedItem, -1);
                }
                else if (action == ActionType.Drill)
                {
                    if (player.Gold >= typeUse * 2)
                    {
                        player.Gold -= typeUse * 2;
                        return (new Stats() { AbsoluteStrength = powerBoost }, []);
                    }
                    else
                    {
                        return (new Stats(), ["ERROR: Not enough Gold"]);
                    }
                }
                return null;
            };

            AddItem(unequippedItem);
            AddItem(equippedItem);
        }
    }
}
