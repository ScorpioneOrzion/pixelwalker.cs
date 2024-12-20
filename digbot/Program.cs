using digbot.Classes;
using dotenv.net;
using PixelPilot.Client;
using PixelPilot.Client.Messages.Packets.Extensions;
using PixelPilot.Client.Players.Basic;
using PixelPilot.Client.World.Constants;
using PixelWalker.Networking.Protobuf.WorldPackets;

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../.env"]));

string email = Environment.GetEnvironmentVariable("DIGBOT_EMAIL")!;
string password = Environment.GetEnvironmentVariable("DIGBOT_PASS")!;

Dictionary<string, DigbotPlayerRole> SetRoles = new()
{
    { "DIGBOT", DigbotPlayerRole.Owner },
    { "SCORPIONEORZION", DigbotPlayerRole.Owner },
    { "MARTEN", DigbotPlayerRole.Immune },
    { "JOHN", DigbotPlayerRole.Immune },
    { "ANATOLY", DigbotPlayerRole.Immune },
    { "PRIDDLE", DigbotPlayerRole.Immune },
    { "REALMS", DigbotPlayerRole.Immune },
};

var players = new Dictionary<string, DigbotPlayer> { };

Registry.Initialize();
var TimeManager = new TimeManager();

(PixelPilotClient, string joinKey) SetupWorld(DigbotWorld worldTemplate)
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
            if (SetRoles.TryGetValue(player.Username.ToUpper(), out var role))
            {
                players.Add(
                    player.Username,
                    new DigbotPlayer()
                    {
                        Username = player.Username,
                        Role = role,
                        TimeManager = TimeManager,
                    }
                );
            }
            else
            {
                players.Add(
                    player.Username,
                    new DigbotPlayer()
                    {
                        Role = DigbotPlayerRole.None,
                        Username = player.Username,
                        TimeManager = TimeManager,
                    }
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

                if (worldTemplate.Commands.TryGetValue(args[0].ToLower(), out var command))
                {
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
            default:
                break;
        }
    };

    return (client, worldTemplate.Name);
}

// (PixelPilotClient, string joinKey, JoinData) SetupCustomWorld(DigbotWorld worldTemplate)
// {
//     var client = PixelPilotClient
//         .Builder()
//         .SetEmail(email)
//         .SetPassword(password)
//         .SetAutomaticReconnect(false)
//         .Build();

//     var random = new Random();

//     var playerManager = new PlayerManager();
//     client.OnPacketReceived += playerManager.HandlePacket;

//     playerManager.OnPlayerJoined += (_, player) =>
//     {
//         if (players.ContainsKey(player.Username)) { }
//         else
//         {
//             if (SetRoles.TryGetValue(player.Username.ToUpper(), out var role))
//             {
//                 players.Add(
//                     player.Username,
//                     new DigbotPlayer() { Username = player.Username, Role = role }
//                 );
//             }
//             else
//             {
//                 players.Add(
//                     player.Username,
//                     new DigbotPlayer() { Role = DigbotPlayerRole.None, Username = player.Username }
//                 );
//             }
//         }
//         if (players[player.Username].Banned)
//         {
//             client.Send(
//                 new PlayerChatPacket() { Message = $"/kick {player.Username} You're still banned" }
//             );
//             client.Send(new PlayerChatPacket() { Message = $"/unkick {player.Username}" });
//         }
//     };

//     client.OnClientConnected += (_) =>
//     {
//         var spaceshipFile = File.ReadAllText("./spaceship.json");
//         var spaceshipStructure = PilotSaveSerializer.Deserialize(spaceshipFile);
//         var blocklist = new List<IPlacedBlock>();
//         spaceshipStructure.Blocks.PasteInOrder(
//             client,
//             new System.Drawing.Point(worldTemplate.Width / 2 - 19, 5)
//         );
//         for (int x = 0; x < worldTemplate.Width; x++)
//         {
//             blocklist.Add(
//                 new PlacedBlock(x, 0, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty))
//             );
//         }
//         for (int y = 0; y < worldTemplate.AirHeight; y++)
//         {
//             blocklist.Add(
//                 new PlacedBlock(
//                     worldTemplate.Width - 1,
//                     y,
//                     WorldLayer.Foreground,
//                     new BasicBlock(PixelBlock.Empty)
//                 )
//             );
//             blocklist.Add(
//                 new PlacedBlock(0, y, WorldLayer.Foreground, new BasicBlock(PixelBlock.Empty))
//             );
//         }
//         client.SendRange(blocklist.ToChunkedPackets());
//         client.Send(new PlayerFacePacket() { FaceId = 17 });
//         client.Send(
//             new PlayerMovedPacket()
//             {
//                 Position = new PointDouble() { X = worldTemplate.Width * 8 - 8, Y = 320 },
//                 TickId = 100,
//             }
//         );
//         spaceshipStructure.Blocks.PasteInOrder(
//             client,
//             new System.Drawing.Point(worldTemplate.Width / 2 - 19, 5)
//         );
//         worldTemplate.Reset(client);
//     };

//     client.OnPacketReceived += (_, packet) =>
//     {
//         var playerId = packet.GetPlayerId();
//         if (playerId == null)
//             return;

//         var player = playerManager.GetPlayer(playerId.Value);
//         if (player == null)
//             return;

//         DigbotPlayer playerObj = players[player.Username];

//         switch (packet)
//         {
//             case PlayerChatPacket chatPacket:
//                 if (!chatPacket.Message.StartsWith('.'))
//                     return;

//                 var fullText = chatPacket.Message[1..]; // Skip leading '.'
//                 var args = fullText.Split(' ');

//                 if (args.Length < 1)
//                     return;

//                 if (worldTemplate.Commands.TryGetValue(args[0].ToLower(), out var command))
//                 {
//                     command.Execute(args[1..], playerObj, client, worldTemplate, random);
//                 }
//                 else
//                 {
//                     client.Send(
//                         new PlayerChatPacket()
//                         {
//                             Message = $"/dm ${player.Id} That command can't be used in this world",
//                         }
//                     );
//                 }
//                 break;

//             case PlayerMovedPacket movedPacket:
//                 int x = (int)((movedPacket.Position.X / 16.0) + 0.5);
//                 int y = (int)((movedPacket.Position.Y / 16.0) + 0.5);
//                 if (y < worldTemplate.AirHeight - 1 || !worldTemplate.Breaking)
//                     return;
//                 if (
//                     movedPacket.SpaceJustDown
//                     && Math.Abs(movedPacket.Horizontal + movedPacket.Vertical) == 1
//                 )
//                 {
//                     if (movedPacket.Horizontal != 0)
//                     {
//                         x += movedPacket.Horizontal;
//                     }
//                     else
//                     {
//                         y += movedPacket.Vertical;
//                     }

//                     if (!worldTemplate.Inside(x, y - worldTemplate.AirHeight))
//                         return;
//                     if (
//                         worldTemplate.GetBlock(x, y - worldTemplate.AirHeight).type
//                         == PixelBlock.Empty
//                     )
//                         return;
//                     if (
//                         worldTemplate.GetBlock(x, y - worldTemplate.AirHeight).type
//                         == PixelBlock.GenericBlackTransparent
//                     )
//                         return;

//                     List<IPlacedBlock> blockList = [];
//                     PixelBlock currentType = worldTemplate
//                         .GetBlock(x, y - worldTemplate.AirHeight)
//                         .type;
//                     worldTemplate.ActBlock(
//                         ActionType.Mine,
//                         playerObj,
//                         x,
//                         y - worldTemplate.AirHeight,
//                         PixelBlock.Empty
//                     );
//                     PixelBlock newBlock = worldTemplate
//                         .GetBlock(x, y - worldTemplate.AirHeight)
//                         .type;
//                     if (currentType != newBlock)
//                     {
//                         blockList.Add(
//                             new PlacedBlock(x, y, WorldLayer.Foreground, new BasicBlock(newBlock))
//                         );
//                     }

//                     (int, int)[] offsets = ranges()[playerObj.Perception];

//                     foreach (var (dx, dy) in offsets)
//                     {
//                         int newX = x + dx;
//                         int newY = y + dy;
//                         if (!worldTemplate.Inside(newX, newY - worldTemplate.AirHeight))
//                             continue;
//                         if (
//                             worldTemplate.GetBlock(newX, newY - worldTemplate.AirHeight).type
//                             != PixelBlock.GenericBlackTransparent
//                         )
//                             continue;
//                         var list = worldTemplate.Blocks.Where(block =>
//                             block.condition(playerObj, (x, y))
//                         );
//                         int randomWeight = random.Next(list.Sum(block => block.weight));
//                         var block = list.First(block => (randomWeight -= block.weight) < 0).block;
//                         worldTemplate.ActBlock(
//                             ActionType.Reveal,
//                             playerObj,
//                             newX,
//                             newY - worldTemplate.AirHeight,
//                             block
//                         );
//                         blockList.Add(
//                             new PlacedBlock(
//                                 newX,
//                                 newY,
//                                 WorldLayer.Foreground,
//                                 new BasicBlock(block)
//                             )
//                         );
//                     }

//                     Task.Run(async () =>
//                     {
//                         await Task.Delay(100);
//                         client.SendRange(blockList.ToChunkedPackets());
//                     });
//                 }
//                 break;
//             default:
//                 break;
//         }
//     };

//     string WorldTitle = $"[Digbot] {worldTemplate.Name}";
//     string joinKey = WorldTitle.Replace("[", "").Replace("]", "").Replace(" ", "_");

//     return (
//         client,
//         joinKey,
//         new JoinData()
//         {
//             WorldHeight = worldTemplate.Height,
//             WorldWidth = worldTemplate.Width,
//             WorldTitle = WorldTitle,
//         }
//     );
// }

var activeClients = new List<PixelPilotClient>();
CaseInsensitiveDictionary<DigbotWorld> worlds = Registry.Worlds;

DigbotWorld Lobby = new(
    (world, action, actor, oldblock, newBlock, position, health) =>
    {
        return (PixelBlock.Empty, 0f);
    }
)
{
    Name = "rc720d56548cfa1",
    Ground = PixelBlock.Empty,
    BlockState = new (PixelBlock, float health)[0, 0],
    AirHeight = 0,
    Blocks = [],
    Commands = [],
};

var (client, joinKey) = SetupWorld(Lobby);

// async Task StartWorld(string worldKey)
// {
//     if (!worlds.TryGetValue(worldKey, out var world))
//     {
//         Console.WriteLine($"World '{worldKey}' not found!");
//         return;
//     }

//     var (client, joinKey, data) = SetupCustomWorld(world);

//     try
//     {
//         await client.Connect(joinKey, data);
//         lock (activeClients)
//         {
//             activeClients.Add(client);
//         }

//         await client.WaitForDisconnect();
//     }
//     finally
//     {
//         lock (activeClients)
//         {
//             activeClients.Remove(client);
//         }
//     }
// }

await client.Connect(joinKey);
activeClients.Add(client);
async Task Init()
{
    try
    {
        await client.WaitForDisconnect();
    }
    finally
    {
        lock (activeClients)
        {
            activeClients.Remove(client);
        }
    }
}
_ = Init();
while (true)
{
    await Task.Delay(TimeSpan.FromMinutes(5));
    lock (activeClients)
    {
        if (activeClients.Count == 0)
        {
            Console.WriteLine("No active clients remaining. Exiting...");
            break;
        }
        else
        {
            Console.WriteLine(
                $"{activeClients.Count} clients still active. Checking again in 5 minutes..."
            );
        }
    }
}

// Dictionary<int, (int dx, int dy)[]> ranges()
// {
//     return new Dictionary<int, (int dx, int dy)[]>
//     {
//         {
//             1,
//             new (int, int)[]
//             {
//                 (0, 1),
//                 (0, -1),
//                 (1, 0),
//                 (-1, 0),
//                 (1, 1),
//                 (-1, -1),
//                 (1, -1),
//                 (-1, 1),
//             }
//         },
//         {
//             2,
//             new (int, int)[]
//             {
//                 (0, 1),
//                 (0, -1),
//                 (1, 0),
//                 (-1, 0),
//                 (1, 1),
//                 (-1, -1),
//                 (1, -1),
//                 (-1, 1),
//                 (2, 0),
//                 (-2, 0),
//                 (0, 2),
//                 (0, -2),
//                 (1, 2),
//                 (-1, 2),
//                 (2, 1),
//                 (-2, 1),
//                 (2, -1),
//                 (-2, -1),
//                 (1, -2),
//                 (-1, -2),
//             }
//         },
//         {
//             3,
//             new (int, int)[]
//             {
//                 (0, 1),
//                 (0, -1),
//                 (1, 0),
//                 (-1, 0),
//                 (1, 1),
//                 (-1, -1),
//                 (1, -1),
//                 (-1, 1),
//                 (2, 0),
//                 (-2, 0),
//                 (0, 2),
//                 (0, -2),
//                 (1, 2),
//                 (-1, 2),
//                 (2, 1),
//                 (-2, 1),
//                 (2, -1),
//                 (-2, -1),
//                 (2, 2),
//                 (-2, 2),
//                 (2, -2),
//                 (-2, -2),
//                 (1, -2),
//                 (-1, -2),
//                 (3, 0),
//                 (-3, 0),
//                 (0, 3),
//                 (0, -3),
//                 (1, 3),
//                 (-1, 3),
//                 (3, 1),
//                 (-3, 1),
//                 (3, -1),
//                 (-3, -1),
//                 (1, -3),
//                 (-1, -3),
//             }
//         },
//     };
// }
