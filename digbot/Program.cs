using digbot;
using digbot.Classes;
using dotenv.net;
using PixelPilot.Client;
using PixelPilot.Client.Messages.Packets.Extensions;
using PixelPilot.Client.Players.Basic;
using PixelPilot.Client.World.Blocks;
using PixelPilot.Client.World.Blocks.Placed;
using PixelPilot.Client.World.Constants;
using PixelPilot.Structures.Converters.PilotSimple;
using PixelPilot.Structures.Extensions;
using PixelWalker.Networking.Protobuf.WorldPackets;

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../.env"]));

string email = Environment.GetEnvironmentVariable("DIGBOT_EMAIL")!;
string password = Environment.GetEnvironmentVariable("DIGBOT_PASS")!;
string[] immune = Environment.GetEnvironmentVariable("IMMUNE")!.Split("|");

var players = new Dictionary<string, DigbotPlayer> { };

(PixelPilotClient, JoinData) SetupWorld(DigbotWorld worldTemplate)
{
    var client = PixelPilotClient
        .Builder()
        .SetEmail(email)
        .SetPassword(password)
        .SetAutomaticReconnect(false)
        .Build();

    var random = new Random();

    var playerManager = new PlayerManager();
    client.OnPacketReceived += playerManager.HandlePacket;

    playerManager.OnPlayerJoined += (_, player) =>
    {
        if (players.ContainsKey(player.Username)) { }
        else
        {
            if (
                player.Username == Environment.GetEnvironmentVariable("OWNER_USER")!
                || player.Username == Environment.GetEnvironmentVariable("BOT_USER")!
            )
            {
                players.Add(
                    player.Username,
                    new DigbotPlayer() { Role = DigbotPlayerRole.Owner, Username = player.Username }
                );
            }
            else if (immune.Contains(player.Username))
            {
                players.Add(
                    player.Username,
                    new DigbotPlayer()
                    {
                        Role = DigbotPlayerRole.Immune,
                        Username = player.Username,
                    }
                );
            }
            else
            {
                players.Add(
                    player.Username,
                    new DigbotPlayer() { Role = DigbotPlayerRole.None, Username = player.Username }
                );
            }
        }
        if (players[player.Username].Banned)
        {
            client.Send(
                new PlayerChatPacket() { Message = $"/kick {player.Username} You're still banned" }
            );
            client.Send(new PlayerChatPacket() { Message = $"/unkick {player.Username}" });
        }
    };

    client.OnClientConnected += (_) =>
    {
        var spaceshipFile = File.ReadAllText("./spaceship.json");
        var spaceshipStructure = PilotSaveSerializer.Deserialize(spaceshipFile);
        var blocklist = new List<IPlacedBlock>();
        spaceshipStructure.Blocks.PasteInOrder(
            client,
            new System.Drawing.Point(worldTemplate.Width / 2 - 19, 5)
        );
        for (int x = 0; x < worldTemplate.Width; x++)
        {
            blocklist.Add(
                new PlacedBlock(x, 0, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty))
            );
        }
        for (int y = 0; y < worldTemplate.AirHeight; y++)
        {
            blocklist.Add(
                new PlacedBlock(
                    worldTemplate.Width - 1,
                    y,
                    WorldLayer.Foreground,
                    new BasicBlock(PixelBlock.Empty)
                )
            );
            blocklist.Add(
                new PlacedBlock(0, y, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty))
            );
        }
        client.SendRange(blocklist.ToChunkedPackets());
        client.Send(new PlayerFacePacket() { FaceId = 17 });
        client.Send(
            new PlayerMovedPacket()
            {
                Position = new PointDouble() { X = worldTemplate.Width * 8 - 8, Y = 320 },
                TickId = 100,
            }
        );
        spaceshipStructure.Blocks.PasteInOrder(
            client,
            new System.Drawing.Point(worldTemplate.Width / 2 - 19, 5)
        );
        worldTemplate.Reset(client);
    };

    client.OnPacketReceived += (_, packet) =>
    {
        var playerId = packet.GetPlayerId();
        if (playerId == null)
            return;

        var player = playerManager.GetPlayer(playerId.Value);
        if (player == null)
            return;

        DigbotPlayer playerObj = players[player.Username];

        switch (packet)
        {
            case PlayerChatPacket chatPacket:
                if (!chatPacket.Message.StartsWith('.'))
                    return;

                var fullText = chatPacket.Message[1..]; // Skip leading '.'
                var args = fullText.Split(' ');

                if (args.Length < 1)
                    return;
                Console.WriteLine(args[0]);

                if (worldTemplate.Commands.TryGetValue(args[0].ToLower(), out var command))
                {
                    Console.WriteLine(args[0]);
                    command.Execute(args[1..], playerObj, client, worldTemplate, random);
                }
                else
                {
                    client.Send(
                        new PlayerChatPacket()
                        {
                            Message = $"/dm ${player.Id} That command can't be used in this world",
                        }
                    );
                }
                break;

            case PlayerMovedPacket movedPacket:
                int x = (int)((movedPacket.Position.X / 16.0) + 0.5);
                int y = (int)((movedPacket.Position.Y / 16.0) + 0.5);
                if (y < worldTemplate.AirHeight - 1 || !worldTemplate.Breaking)
                    return;
                if (
                    movedPacket.SpaceJustDown
                    && Math.Abs(movedPacket.Horizontal + movedPacket.Vertical) == 1
                )
                {
                    if (movedPacket.Horizontal != 0)
                    {
                        x += movedPacket.Horizontal;
                    }
                    else
                    {
                        y += movedPacket.Vertical;
                    }

                    if (!worldTemplate.Inside(x, y - worldTemplate.AirHeight))
                        return;
                    if (
                        worldTemplate.GetBlock(x, y - worldTemplate.AirHeight).type
                        == PixelBlock.Empty
                    )
                        return;
                    if (
                        worldTemplate.GetBlock(x, y - worldTemplate.AirHeight).type
                        == PixelBlock.GenericBlackTransparent
                    )
                        return;

                    List<IPlacedBlock> blockList = [];
                    PixelBlock currentType = worldTemplate
                        .GetBlock(x, y - worldTemplate.AirHeight)
                        .type;
                    worldTemplate.MineBlock(playerObj, x, y - worldTemplate.AirHeight);
                    PixelBlock newBlock = worldTemplate
                        .GetBlock(x, y - worldTemplate.AirHeight)
                        .type;
                    if (currentType != newBlock)
                    {
                        blockList.Add(
                            new PlacedBlock(x, y, WorldLayer.Foreground, new BasicBlock(newBlock))
                        );
                    }

                    (int, int)[] offsets = ranges()[playerObj.Perception];

                    foreach (var (dx, dy) in offsets)
                    {
                        int newX = x + dx;
                        int newY = y + dy;
                        if (!worldTemplate.Inside(newX, newY - worldTemplate.AirHeight))
                            continue;
                        if (
                            worldTemplate.GetBlock(newX, newY - worldTemplate.AirHeight).type
                            != PixelBlock.GenericBlackTransparent
                        )
                            continue;
                        var list = worldTemplate.Blocks.Where(block =>
                            block.condition(playerObj, (x, y))
                        );
                        int randomWeight = random.Next(list.Sum(block => block.weight));
                        var block = list.First(block => (randomWeight -= block.weight) < 0).block;
                        worldTemplate.RevealBlock(newX, newY - worldTemplate.AirHeight, block);
                        blockList.Add(
                            new PlacedBlock(
                                newX,
                                newY,
                                WorldLayer.Foreground,
                                new BasicBlock(block)
                            )
                        );
                    }

                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        client.SendRange(blockList.ToChunkedPackets());
                    });
                }
                break;
        }
    };

    return (
        client,
        new JoinData()
        {
            WorldHeight = worldTemplate.Height,
            WorldWidth = worldTemplate.Width,
            WorldTitle = $"[Digbot] {worldTemplate.Name}",
        }
    );
}

Command CommandReset = new()
{
    Execute = (args, player, client, world, random) =>
    {
        if (player.Role == DigbotPlayerRole.Owner)
        {
            world.Reset(client);
        }
    },
    Roles = [DigbotPlayerRole.Owner],
};

Command CommandHelp = new()
{
    Execute = (args, player, client, world, random) =>
    {
        var commands = world.Commands.Where(command => command.Value.Roles.Contains(player.Role));
        client.Send(new PlayerChatPacket() { Message = "" });
    },
    Roles = [DigbotPlayerRole.Owner, DigbotPlayerRole.Immune, DigbotPlayerRole.None],
};

DigbotWorld voidWorld = new(
    (type, position) =>
    {
        if (type == PixelBlock.BasicBlack)
            return 5.0f;
        return 10.0f;
    },
    (player, type, position, health) =>
    {
        if (type != PixelBlock.BasicBlack)
        {
            float newHealth = health -= player.Power / 2;
            if (newHealth > 0f)
            {
                return (type, newHealth);
            }
            else
            {
                return (PixelBlock.Empty, 0.0f);
            }
        }
        else
        {
            float newHealth = health -= player.Power;
            if (newHealth > 0f)
            {
                return (type, newHealth);
            }
            else
                return (PixelBlock.Empty, 0.0f);
        }
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
    Commands = new() { { "reset", CommandReset }, { "help", CommandHelp } },
};

var (client, data) = SetupWorld(voidWorld);

await client.Connect($"digbot_void", data);

await client.WaitForDisconnect();

Dictionary<int, (int dx, int dy)[]> ranges()
{
    return new Dictionary<int, (int dx, int dy)[]>
    {
        {
            1,
            new (int, int)[]
            {
                (0, 1),
                (0, -1),
                (1, 0),
                (-1, 0),
                (1, 1),
                (-1, -1),
                (1, -1),
                (-1, 1),
            }
        },
        {
            2,
            new (int, int)[]
            {
                (0, 1),
                (0, -1),
                (1, 0),
                (-1, 0),
                (1, 1),
                (-1, -1),
                (1, -1),
                (-1, 1),
                (2, 0),
                (-2, 0),
                (0, 2),
                (0, -2),
                (1, 2),
                (-1, 2),
                (2, 1),
                (-2, 1),
                (2, -1),
                (-2, -1),
                (1, -2),
                (-1, -2),
            }
        },
        {
            3,
            new (int, int)[]
            {
                (0, 1),
                (0, -1),
                (1, 0),
                (-1, 0),
                (1, 1),
                (-1, -1),
                (1, -1),
                (-1, 1),
                (2, 0),
                (-2, 0),
                (0, 2),
                (0, -2),
                (1, 2),
                (-1, 2),
                (2, 1),
                (-2, 1),
                (2, -1),
                (-2, -1),
                (2, 2),
                (-2, 2),
                (2, -2),
                (-2, -2),
                (1, -2),
                (-1, -2),
                (3, 0),
                (-3, 0),
                (0, 3),
                (0, -3),
                (1, 3),
                (-1, 3),
                (3, 1),
                (-3, 1),
                (3, -1),
                (-3, -1),
                (1, -3),
                (-1, -3),
            }
        },
    };
}
