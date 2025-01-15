using PixelPilot.Client;
using PixelPilot.Client.World.Blocks;
using PixelPilot.Client.World.Blocks.Placed;
using PixelPilot.Client.World.Constants;
using PixelPilot.Structures.Extensions;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class DigbotWorld(
        Func<
            DigbotWorld,
            ActionType,
            Actor,
            PixelBlock,
            (int x, int y, PixelBlock block, float health),
            (PixelBlock block, float health, int x, int y)[]
        > UpdateFunction
    ) : Actor
    {
        public required string Name;
        public int Width
        {
            get => BlockState.GetLength(0);
        }
        public int Height
        {
            get => BlockState.GetLength(1) + AirHeight;
        }
        public required int AirHeight;
        private readonly Func<
            DigbotWorld,
            ActionType,
            Actor,
            PixelBlock,
            (int x, int y, PixelBlock block, float health),
            (PixelBlock block, float health, int x, int y)[]
        > _UpdateFunction = UpdateFunction;
        public required (PixelBlock type, float health)[,] BlockState;
        public required PixelBlock Ground;
        public bool Breaking;
        public required List<(
            PixelBlock block,
            int weight,
            Func<DigbotPlayer, (int x, int y), bool> condition
        )> Blocks;
        public required CaseInsensitiveDictionary<DigbotCommand> Commands;

        public bool Inside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height - AirHeight;
        }

        public void Reset(PixelPilotClient client)
        {
            Breaking = false;
            client.Send(new PlayerChatPacket() { Message = $"/resetplayer @a[username!=DIGBOT]" });

            var blockList = new List<IPlacedBlock>();
            for (int x = 0; x < Width; x++)
            {
                blockList.Add(
                    new PlacedBlock(x, 40, WorldLayer.Foreground, new BasicBlock(Ground))
                );
                ActBlock(ActionType.Reveal, this, x, 0, Ground);
            }
            client.SendRange(blockList.ToChunkedPackets());
            for (int y = 1; y < Height - AirHeight; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    BlockState[x, y] = (PixelBlock.GenericBlackTransparent, 0.0f);
                    blockList.Add(
                        new PlacedBlock(
                            x,
                            y + AirHeight,
                            WorldLayer.Foreground,
                            new BasicBlock(PixelBlock.GenericBlackTransparent)
                        )
                    );
                }
            }
            client.SendRange(blockList.ToChunkedPackets());
            Breaking = true;
            client.SendRange(blockList.ToChunkedPackets());
        }

        public (PixelBlock block, float health, int x, int y)[] ActBlock(
            ActionType action,
            Actor actor,
            int x,
            int y,
            PixelBlock block
        )
        {
            return ActBlock(action, actor, (x, y), block);
        }

        public (PixelBlock block, float health, int x, int y)[] ActBlock(
            ActionType action,
            Actor actor,
            (int x, int y) position,
            PixelBlock block
        )
        {
            if (Inside(position.x, position.y))
            {
                var (oldBlock, health) = GetBlock(position);
                var result = _UpdateFunction(
                    this,
                    action,
                    actor,
                    block,
                    (position.x, position.y, oldBlock, health)
                );

                for (var i = 0; i < result.Length; i++)
                {
                    result[i].y += AirHeight;
                }

                return result;
            }

            return [];
        }

        public (PixelBlock type, float health) GetBlock((int x, int y) position)
        {
            return GetBlock(position.x, position.y);
        }

        public (PixelBlock type, float health) GetBlock(int x, int y)
        {
            if (Inside(x, y))
            {
                return BlockState[x, y];
            }
            return (type: PixelBlock.Empty, health: 0.0f);
        }
    }

    public class DigbotCommand
    {
        public required Action<
            string[],
            DigbotPlayer,
            (int id, (int x, int y) position),
            PixelPilotClient,
            DigbotWorld
        > Execute;

        public DigbotPlayerRole Role = DigbotPlayerRole.None;
        public bool LobbyCommand = false;
        public Action<
            string[],
            DigbotPlayer,
            (int id, (int x, int y) position),
            PixelPilotClient
        > LobbyExecute = (args, player, playerId, lobby) => { };
    }

    public class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public new void Add(string key, TValue value)
        {
            base.Add(key.ToLower(), value);
        }
    }
}
