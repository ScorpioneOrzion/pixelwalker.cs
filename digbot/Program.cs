using Digbot.DigbotClasses;
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

void HandlePlayerChatPacket(
    PlayerChatPacket chatPacket,
    Player player,
    PixelPilotClient client,
    DigbotWorld world,
    Random random
)
{
    if (!chatPacket.Message.StartsWith('.'))
        return;
    var fullText = chatPacket.Message[1..]; // Skip leading '.'
    var args = fullText.Split(' ');

    if (args.Length == 0)
        return;

    switch (args[0])
    {
        case "reset":
            if (player.Username == Environment.GetEnvironmentVariable("OWNER_USER")!)
            {
                world.Reset(client);
                Console.WriteLine("World reset.");
            }
            break;

        case "ban":
            if (args.Length < 2)
                return;
            if (immune.Contains(args[1]) && !immune.Contains(player.Username))
            {
                client.Send(
                    new PlayerChatPacket()
                    {
                        Message =
                            $"/kick {args[1]} That person is immune, here have a timeout instead.",
                    }
                );
                break;
            }
            if (
                player.Username == Environment.GetEnvironmentVariable("OWNER_USER")!
                && !immune.Contains(args[1])
            )
            {
                client.Send(new PlayerChatPacket() { Message = $"/kick {args[1]} You're banned" });
                client.Send(new PlayerChatPacket() { Message = $"/unkick {args[1]}" });
                players[args[1]].Banned = true;
            }
            break;
        case "unban":
            if (args.Length < 2)
                return;
            if (player.Username == Environment.GetEnvironmentVariable("OWNER_USER")!)
            {
                players[args[1]].Banned = false;
            }
            break;
        default:
            Console.WriteLine($"Unknown command: {args[0]}");
            break;
    }
}

void HandlePlayerMovedPacket(
    PlayerMovedPacket movedPacket,
    Player player,
    PixelPilotClient client,
    DigbotWorld world,
    Random random
)
{
    int x = (int)((movedPacket.Position.X / 16.0) + 0.5);
    int y = (int)((movedPacket.Position.Y / 16.0) + 0.5);
    if (y < world.AirHeight - 1 || !world.Breaking)
        return;
    if (movedPacket.SpaceJustDown && Math.Abs(movedPacket.Horizontal + movedPacket.Vertical) == 1)
    {
        if (movedPacket.Horizontal != 0)
        {
            x += movedPacket.Horizontal;
        }
        else
        {
            y += movedPacket.Vertical;
        }

        if (!world.Inside(x, y - world.AirHeight))
            return;
        if (world.GetBlock(x, y - world.AirHeight).type == PixelBlock.Empty)
            return;
        if (world.GetBlock(x, y - world.AirHeight).type == PixelBlock.GenericBlackTransparent)
            return;

        DigbotPlayer playerObj = players[player.Username];

        List<IPlacedBlock> blockList = [];
        PixelBlock currentType = world.GetBlock(x, y - world.AirHeight).type;
        world.MineBlock(playerObj, x, y - world.AirHeight);
        PixelBlock newBlock = world.GetBlock(x, y - world.AirHeight).type;
        if (currentType != newBlock)
        {
            blockList.Add(new PlacedBlock(x, y, WorldLayer.Foreground, new BasicBlock(newBlock)));
        }

        (int, int)[] offsets = ranges()[playerObj.Perception];
        // reveal blocks here

        foreach (var (dx, dy) in offsets)
        {
            int newX = x + dx;
            int newY = y + dy;
            if (!world.Inside(newX, newY - world.AirHeight))
                continue;
            if (
                world.GetBlock(newX, newY - world.AirHeight).type
                != PixelBlock.GenericBlackTransparent
            )
                continue;
            var list = world.Blocks.Where(block => block.condition(playerObj, (x, y)));
            int randomWeight = random.Next(list.Sum(block => block.weight));
            var block = list.First(block => (randomWeight -= block.weight) < 0).block;
            world.RevealBlock(newX, newY - world.AirHeight, block);
            blockList.Add(
                new PlacedBlock(newX, newY, WorldLayer.Foreground, new BasicBlock(block))
            );
        }

        Task.Run(async () =>
        {
            await Task.Delay(100);
            client.SendRange(blockList.ToChunkedPackets());
        });
    }
}

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
            players.Add(player.Username, new DigbotPlayer() { });
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
        spaceshipStructure.Blocks.PasteInOrder(
            client,
            new System.Drawing.Point(worldTemplate.Width / 2 - 19, 5)
        );
        for (int x = 0; x < worldTemplate.Width; x++) { 
            blocklist.Add(new PlacedBlock(x, 0, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty)));
		}
        for (int y = 0; y < worldTemplate.AirHeight; y++)
        {
            blocklist.Add(new PlacedBlock(worldTemplate.Width - 1, y, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty)));
            blocklist.Add(new PlacedBlock(0, y, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty)));
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

        switch (packet)
        {
            case PlayerChatPacket chatPacket:
                HandlePlayerChatPacket(chatPacket, player, client, worldTemplate, random);
                break;
            case PlayerMovedPacket movedPacket:
                HandlePlayerMovedPacket(movedPacket, player, client, worldTemplate, random);
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

DigbotWorld voidWorld = new(
    "Void",
    PixelBlock.BasicBlack,
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
                return (PixelBlock.Empty, 0.0f);
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
    },
    [
        (PixelBlock.BasicBlack, 1000, (player, position) => true),
        (PixelBlock.BasicRed, 5, (player, position) => true),
        (PixelBlock.BasicBlue, 5, (player, position) => true),
        (PixelBlock.BasicGreen, 5, (player, position) => true),
        (PixelBlock.BasicYellow, 5, (player, position) => true),
        (PixelBlock.BasicCyan, 5, (player, position) => true),
    ]
);

var (client, data) = SetupWorld(voidWorld);

Console.WriteLine(data.WorldHeight);
Console.WriteLine(data.WorldWidth);

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
