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
            PixelPilotClient,
            ActionType,
            (Actor entity, int playerId),
            PixelBlock,
            ((int x, int y) position, PixelBlock block, float health),
            (PixelBlock block, float health, (int x, int y) position)[]
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
            PixelPilotClient,
            ActionType,
            (Actor entity, int playerId),
            PixelBlock,
            ((int x, int y) position, PixelBlock block, float health),
            (PixelBlock block, float health, (int x, int y) position)[]
        > _UpdateFunction = UpdateFunction;
        public required (PixelBlock type, float health)[,] BlockState;
        public required PixelBlock Ground;
        public bool Breaking;
        public required List<(
            PixelBlock block,
            int weight,
            Func<DigbotPlayer, (int x, int y), bool> condition
        )> Blocks;
        public required Dictionary<string, DigbotCommand> Commands;

        public bool Inside((int x, int y) position)
        {
            return Inside(position.x, position.y);
        }

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
                ActBlock(client, ActionType.Reveal, (this, -1), x, 0, Ground);
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

        public (PixelBlock block, float health, (int x, int y) position)[] ActBlock(
            PixelPilotClient client,
            ActionType action,
            (Actor entity, int playerId) actor,
            int x,
            int y,
            PixelBlock newBlock
        )
        {
            return ActBlock(client, action, actor, (x, y), newBlock);
        }

        public (PixelBlock block, float health, (int x, int y) position)[] ActBlock(
            PixelPilotClient client,
            ActionType action,
            (Actor entity, int playerId) actorInfo,
            (int x, int y) blockCoordinates,
            PixelBlock newBlock
        )
        {
            if (Inside(blockCoordinates))
            {
                var (oldBlock, currentHealth) = BlockState[blockCoordinates.x, blockCoordinates.y];

                var updatedBlocks = _UpdateFunction(
                    client,
                    action,
                    actorInfo,
                    newBlock,
                    (blockCoordinates, oldBlock, currentHealth)
                );

                for (var i = 0; i < updatedBlocks.Length; i++)
                {
                    updatedBlocks[i].position.y += AirHeight;
                }

                return updatedBlocks;
            }

            return [];
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
}
