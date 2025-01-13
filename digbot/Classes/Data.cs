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
                        (Stats, List<string>)? result = null;
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
                            if (messages.Any(message => message == "Error"))
                            {
                                SendDirectMessage(
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

                        if (!item.Buyable)
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

                        if (!item.Buyable)
                        {
                            SendDirectMessage(client, "You can't sell that item", playerObj.id);
                            return;
                        }

                        if (
                            !playerEntity.Inventory.ContainsKey(item)
                            || playerEntity.Inventory[item] <= 0
                        )
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
                        if (actor == world)
                        {
                            return [(block, 5f * blockData.y, blockData.x, blockData.y)];
                        }
                        if (actor is not Entity entity)
                        {
                            return [];
                        }
                        var result = entity.Use(action, (blockData.x, blockData.y));

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
                        // do stuff with result
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

        public static readonly CaseInsensitiveDictionary<DigbotItem> Items = new()
        {
            {
                "StaticPower",
                new HiddenDigbotItem()
                {
                    Use = (entity, action, position) =>
                    {
                        if (action == ActionType.Mine)
                        {
                            return (new() { AbsoluteStrength = 0.01f }, []);
                        }
                        return null;
                    },
                }
            },
            {
                "RelativePower",
                new HiddenDigbotItem()
                {
                    Use = (entity, action, position) =>
                    {
                        if (action == ActionType.Mine)
                        {
                            return (new() { RelativeStrength = 0.01f }, []);
                        }
                        return null;
                    },
                }
            },
            {
                "StaticLuck",
                new HiddenDigbotItem()
                {
                    Use = (entity, action, position) =>
                    {
                        if (action == ActionType.Mine)
                        {
                            return (new() { AbsoluteLuck = 0.01f }, []);
                        }
                        return null;
                    },
                }
            },
            {
                "RelativeLuck",
                new HiddenDigbotItem()
                {
                    Use = (entity, action, position) =>
                    {
                        if (action == ActionType.Mine)
                        {
                            return (new() { RelativeLuck = 0.01f }, []);
                        }
                        return null;
                    },
                }
            },
        };

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
                        Cost = 5f,
                    }
                );

                // Ingot
                Items.Add(
                    $"{oreNames[i]}Ingot",
                    new()
                    {
                        Name = $"{Capitalize(oreNames[i])} Ingot",
                        Description = $"A bar of {oreNames[i]} ingot",
                        Cost = 10f,
                    }
                );
            }

            Items.Add(
                "Coal",
                new()
                {
                    Name = "Coal",
                    Description = "A chunk of coal",
                    Cost = 1f,
                }
            );
        }

        private static string Capitalize(string str) => $"{str[..1].ToUpper()}{str[1..]}";

        private static void AddPickaxe(
            string unequippedName,
            string equippedName,
            ItemType type,
            float powerBoost,
            int typeUse = 1
        )
        {
            DigbotItem unequippedItem = new() { Type = type };
            DigbotItem equippedItem = new() { Type = type, TypeUse = typeUse };

            unequippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    if (player.ItemLimits[equippedItem.Type] < equippedItem.TypeUse)
                    {
                        return (new Stats(), ["Error"]);
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

            Items.Add(unequippedName, unequippedItem);
            Items.Add(equippedName, equippedItem);
        }

        private static void AddDrill(
            string unequippedName,
            string equippedName,
            ItemType type,
            float powerBoost,
            int typeUse = 1
        )
        {
            DigbotItem unequippedItem = new() { Type = type };
            DigbotItem equippedItem = new() { Type = type, TypeUse = typeUse };

            unequippedItem.Use += (player, action, position) =>
            {
                if (action == ActionType.Equip)
                {
                    if (player.ItemLimits[equippedItem.Type] < equippedItem.TypeUse)
                    {
                        return (new Stats(), ["Error"]);
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
                    if (player.Gold >= typeUse * 2)
                    {
                        player.Gold -= typeUse * 2;
                        return (new Stats() { AbsoluteStrength = powerBoost }, []);
                    }
                    else
                    {
                        return (new Stats(), ["Not enough Gold"]);
                    }
                }
                return null;
            };

            Items.Add(unequippedName, unequippedItem);
            Items.Add(equippedName, equippedItem);
        }
    }
}
