using dotenv.net;
using PixelPilot.Client;

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../.env"]));

string email = Environment.GetEnvironmentVariable("DIGBOT_EMAIL")!;
string password = Environment.GetEnvironmentVariable("DIGBOT_PASS")!;

var client = PixelPilotClient.Builder()
	.SetEmail(email)
	.SetPassword(password)
	.SetAutomaticReconnect(false)
	.Build();

client.OnPacketReceived += (_, packet) =>
{
	switch (packet)
	{
		case PlayerJoinPacket join:
        client.Send(new PlayerChatOutPacket($"/givegod {join.Username}"));
        break;
	}
};

await client.Connect("");

await client.WaitForDisconnect();