using System.Drawing;
using dotenv.net;
using PixelPilot.Client;
using PixelPilot.Client.Messages.Packets.Extensions;
using PixelPilot.Client.Players;
using PixelPilot.Client.Players.Basic;
using PixelPilot.Client.World;
using PixelPilot.Structures;
using PixelPilot.Structures.Converters.PilotSimple;
using PixelPilot.Structures.Extensions;
using PixelWalker.Networking.Protobuf.WorldPackets;

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../.env"]));

string email = Environment.GetEnvironmentVariable("DIGBOT_EMAIL")!;
string password = Environment.GetEnvironmentVariable("DIGBOT_PASS")!;

Structure? currentStructure = null;
var point1 = new Point(0, 0);
var point2 = new Point(0, 0);

var client = PixelPilotClient.Builder()
	.SetEmail(email)
	.SetPassword(password)
	.SetAutomaticReconnect(false)
	.Build();

var world = new PixelWorld(400, 636);
client.OnPacketReceived += world.HandlePacket;

var playerManager = new PlayerManager();
client.OnPacketReceived += playerManager.HandlePacket;

playerManager.OnPlayerJoined += (_, player) =>
{
	Console.WriteLine($"Player {player.Id} joined.");
	Console.WriteLine($"Player {player.Username} joined.");
	client.Send(new PlayerChatPacket()
	{
		Message = $"/givegod {player.Username}"
	});
	if (player.Username == Environment.GetEnvironmentVariable("OWNER_USER")!)
	{
		client.Send(new PlayerChatPacket()
		{
			Message = $"/giveedit {player.Username}"
		});
	};
};

client.OnPacketReceived += (_, packet) =>
{
	var playerId = packet.GetPlayerId();
	if (playerId == null) return;
	IPixelPlayer? player = playerManager.GetPlayer(playerId.Value);
	if (player == null) return;
	switch (packet)
	{
		case PlayerChatPacket chat:
			{
				if (!chat.Message.StartsWith('.') || player.Username != Environment.GetEnvironmentVariable("OWNER_USER")!) return;

				var fullText = chat.Message[1..];
				var args = fullText.Split(' ');

				switch (args[0])
				{
					case "p1":
						point1 = new Point(player.BlockX, player.BlockY);
						client.SendChat($"Point 1 has been set. {point1}");
						break;
					case "p2":
						point2 = new Point(player.BlockX, player.BlockY);
						client.SendChat($"Point 2 has been set. {point2}");
						break;
					case "select":
					case "copy":
						currentStructure = world.GetStructure(point1, point2, false);
						client.SendChat("Current structure has been set.");
						break;
					case "save":
						if (currentStructure == null)
						{
							client.SendChat("Please copy a structure using .copy");
							return;
						}

						if (args.Length < 2 || currentStructure == null)
						{
							client.SendChat("Please provide a file name.");
							return;
						}

						var rawSave = PilotSaveSerializer.Serialize(currentStructure);
						File.WriteAllText($"./{args[1]}.json", rawSave);

						client.SendChat("Structure saved to file.");
						break;
					case "load":
						if (args.Length < 2)
						{
							client.SendChat("Please provide a file name.");
							return;
						}

						var rawLoad = File.ReadAllText($"./{args[1]}.json");
						currentStructure = PilotSaveSerializer.Deserialize(rawLoad);

						client.SendChat("Structure loaded from file.");
						break;
					case "paste":
						{
							if (currentStructure == null) return;

							var pasteX = player.BlockX;
							var pasteY = player.BlockY;

							var packets = world.GetDifference(currentStructure, pasteX, pasteY).ToChunkedPackets();

							client.SendChat($"Pasting structure... {packets.Count}");
							client.SendRange(packets);

							break;
						}
					case "exit":
						{
							client.Disconnect();
							break;
						}
				}
				return;
			}
	}
};

client.OnClientConnected += (_) =>
{
	var spaceshipFile = File.ReadAllText("./spaceship.json");
	var spaceshipStructure = PilotSaveSerializer.Deserialize(spaceshipFile);
	var spaceshipPackets = world.GetDifference(spaceshipStructure, 299, 5).ToChunkedPackets();
	client.SendRange(spaceshipPackets);
};

await client.Connect($"digbot_test", new JoinData()
{
	WorldHeight = 400,
	WorldWidth = 636,
	WorldTitle = "---"
});

await client.WaitForDisconnect();