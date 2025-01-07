using PixelPilot.Client;
using PixelPilot.Client.World.Blocks;
using PixelPilot.Client.World.Blocks.Placed;
using PixelPilot.Client.World.Constants;
using PixelPilot.Structures.Extensions;
using PixelWalker.Networking.Protobuf.WorldPackets;

namespace digbot.Classes
{
    public class TimeManager
    {
        private readonly List<(DigbotItem Item, Entity Player, float Time)> _timers = [];

        public void AddTimer(DigbotItem item, Entity player, float time)
        {
            _timers.Add((item, player, time));
        }

        private int _currentBatch = 0;

        public void Update(float deltaTime, int batchSize = 100)
        {
            int end = Math.Min(_currentBatch + batchSize, _timers.Count);

            for (int i = _currentBatch; i < end; i++)
            {
                var (item, player, time) = _timers[i];
                time -= deltaTime;
                if (time <= 0)
                {
                    item.Use(player, ActionType.AutoUse);
                    _timers.RemoveAt(i);
                    i--;
                    end--;
                }
                else
                {
                    _timers[i] = (item, player, time);
                }
            }

            _currentBatch = end >= _timers.Count ? 0 : end;
        }
    }

    public class DigbotItem
    {
        public string Name = "";
        public string Description = "";
        public (float a, float r) PowerBoost = (0, 0);
        public ItemLimits LimitBoost = new();
        public (float a, float r) LuckBoost = (0, 0);
        public int PerceptionBoost = 0;
        public int TypeUse = 0;
        public (float a, float d) Gold = (0f, 0f);
        public (ItemType, ActionType) Type = (ItemType.Unknown, ActionType.Unknown);
        public Func<Entity, ActionType, (float, ActionType)[]?> Use = (player, action) =>
        {
            return null;
        };
        public bool Hidden = false;
        public float Time = 0f;

        public void Buy(Entity player, string amount)
        {
            if (Gold.a == 0f)
                return;
            if (amount == "all")
                Buy(player, (int)Math.Floor(player.Gold / Gold.a));
            else if (int.TryParse(amount, out int num))
                Buy(player, num);
        }

        public void Buy(Entity player, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (Gold.a == 0f)
                return;
            if (player.Gold < Gold.a * amount)
                return;
            player.AddItems(this, amount);
            player.Gold -= Gold.a * amount;
        }

        public void Sell(Entity player, string amount)
        {
            if (amount == "all")
                Sell(player, player.Inventory[this]);
            else if (int.TryParse(amount, out int num))
                Sell(player, num);
        }

        public void Sell(Entity player, int amount = 1)
        {
            if (amount <= 0)
                return;
            if (Gold.a == 0f)
                return;
            if (!player.Inventory.ContainsKey(this))
                return;
            if (player.Inventory[this] < amount)
                amount = player.Inventory[this];
            player.AddItems(this, -amount);
            player.Gold += (float)(Gold.a * amount * 0.9);
        }
    }

    public class HiddenDigbotItem : DigbotItem
    {
        public HiddenDigbotItem()
        {
            Name = "";
            Description = "";
            Hidden = true;
            Gold.a = 0f;
        }
    }

    public class DigbotWorld(
        Func<
            DigbotWorld,
            ActionType,
            Actor,
            PixelBlock,
            (int x, int y, PixelBlock block, float health),
            (PixelBlock, float, int, int)[]
        > HealthCalculator
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
            (PixelBlock, float, int, int)[]
        > _HealthCalculator = HealthCalculator;
        public required float Difficulty;
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

        public void ActBlock(ActionType action, Actor actor, int x, int y, PixelBlock block)
        {
            ActBlock(action, actor, (x, y), block);
        }

        public void ActBlock(
            ActionType action,
            Actor actor,
            (int x, int y) position,
            PixelBlock block
        )
        {
            if (Inside(position.x, position.y))
            {
                var (oldBlock, health) = GetBlock(position);
                var result = _HealthCalculator(
                    this,
                    action,
                    actor,
                    block,
                    (position.x, position.y, oldBlock, health)
                );
            }
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
        public required Action<string[], DigbotPlayer, int, PixelPilotClient, DigbotWorld> Execute;

        public DigbotPlayerRole[] Roles =
        [
            DigbotPlayerRole.None,
            DigbotPlayerRole.Immune,
            DigbotPlayerRole.Owner,
        ];
        public bool LobbyCommand = false;
        public Action<string[], DigbotPlayer, int, PixelPilotClient> LobbyExecute = (
            args,
            player,
            playerId,
            lobby
        ) => { };
    }

    public class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public new void Add(string key, TValue value)
        {
            base.Add(key.ToLower(), value);
        }
    }
}
