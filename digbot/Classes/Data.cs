using PixelPilot.Client.World.Constants;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class Registry
    {
        public static readonly CaseInsensitiveDictionary<Command> Commands = new()
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
                                actor.Power / 2
                            ),
                            ActionType.Drill => HandleDamage(
                                actor,
                                oldBlock,
                                health,
                                actor.Power * 2
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
                "power",
                new HiddenDigbotItem() { PowerBoost = 1f }
            },
            {
                "health",
                new HiddenDigbotItem() { HealthBoost = 1f }
            },
            {
                "luck",
                new HiddenDigbotItem() { LuckBoost = 1f }
            },
            {
                "healthPotion1",
                new()
                {
                    Name = "Small Health Potion",
                    Description = "Restores 20 Hp",
                    Use = (player, item) =>
                    {
                        player.RemoveItems(item);
                        player.Health += 20f;
                    },
                }
            },
        };
    }
}
