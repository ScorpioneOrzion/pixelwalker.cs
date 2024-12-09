using PixelPilot.Client;
using PixelPilot.Client.World.Blocks;
using PixelPilot.Client.World.Blocks.Placed;
using PixelPilot.Client.World.Constants;
using PixelPilot.Structures.Extensions;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace Digbot.DigbotClasses
{
    public class DigbotWorld
    {
        public string Name { get; }
        public int Width { get; } = 400;
        public int AirHeight { get; } = 40;
        public int Height { get; } = 400;
        private readonly Func<PixelBlock, (int x, int y), float> _maxHealthCalculator;
        private readonly Func<
            DigbotPlayer,
            PixelBlock,
            (int x, int y),
            float,
            (PixelBlock, float)
        > _mineHealthCalculator;
        public (PixelBlock type, float health)[,] BlockState { get; private set; }
        public PixelBlock Ground { get; }
        public bool Breaking;
        public List<(
            PixelBlock block,
            int weight,
            Func<DigbotPlayer, (int x, int y), bool> condition
        )> Blocks { get; }

        public DigbotWorld(
            string name,
            PixelBlock ground,
            Func<PixelBlock, (int x, int y), float> setHealthCalculator,
            Func<
                DigbotPlayer,
                PixelBlock,
                (int x, int y),
                float,
                (PixelBlock, float)
            > mineHealthCalculator,
            List<(
                PixelBlock block,
                int weight,
                Func<DigbotPlayer, (int x, int y), bool> condition
            )> blocks
        )
        {
            Name = name;
            Ground = ground;
            Blocks = blocks;
            _maxHealthCalculator = setHealthCalculator;
            _mineHealthCalculator = mineHealthCalculator;
            Breaking = false;
            BlockState = new (PixelBlock, float)[Width, Height - AirHeight];
        }

        public bool Inside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height - AirHeight;
        }

        public void Reset(PixelPilotClient client)
        {
            Breaking = false;
            client.Send(new PlayerChatPacket() { Message = "/resetplayer @a[username!=DIGBOT]" });

            var blockList = new List<IPlacedBlock>();
            for (int x = 0; x < Width; x++)
            {
                blockList.Add(
                    new PlacedBlock(x, 40, WorldLayer.Foreground, new BasicBlock(Ground))
                );
                RevealBlock(x, 0, Ground);
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

        public void RevealBlock(int x, int y, PixelBlock setType)
        {
            if (Inside(x, y))
            {
                float health = _maxHealthCalculator(setType, (x, y));
                BlockState[x, y] = (setType, health);
            }
        }

        public void MineBlock(DigbotPlayer player, int x, int y)
        {
            if (Inside(x, y))
            {
                (PixelBlock blockType, float health) = BlockState[x, y];
                (PixelBlock newType, float newHealth) = _mineHealthCalculator(
                    player,
                    blockType,
                    (x, y),
                    health
                );
                BlockState[x, y] = (newType, newHealth);
            }
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
}
