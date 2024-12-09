using digbot.Classes;
using PixelPilot.Client;

namespace digbot
{
    public class Command
    {
        public required Action<
            string[],
            DigbotPlayer,
            PixelPilotClient,
            DigbotWorld,
            Random
        > Execute;

        public required DigbotPlayerRole[] Roles;
    }

    public class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public new void Add(string key, TValue value)
        {
            base.Add(key.ToLower(), value);
        }
    }
}
