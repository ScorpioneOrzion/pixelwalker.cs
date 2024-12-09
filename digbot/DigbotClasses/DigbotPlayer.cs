namespace Digbot.DigbotClasses
{
    public enum DamageType
    {
        Physical,
        Explosion,
        Fire,
        Poison,
        Electric,
    }

    public class DigbotPlayer
    {
        public bool Banned = false;
        public int Perception = 1;
        public float Power = 1f;
        public float MaxHealth = 100f;
        private float _health = 100f;
        public float Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }
        public Defense FlatDefense = new()
        {
            Resistance = 0f,
            Physical = 0f,
            Fire = 0f,
            Poison = 0f,
            Electric = 0f,
            Explosion = 0f,
        };
        public Defense PercentageDefense = new()
        {
            Resistance = 0f,
            Physical = 0f,
            Fire = 0f,
            Poison = 0f,
            Electric = 0f,
            Explosion = 0f,
        };
        public float Luck = 0f;
        public DigbotPlayerInventory Inventory = new DigbotPlayerInventory();

        public void TakeDamage(float damage, DamageType type)
        {
            float FlatDefense = this.FlatDefense.Type(type);
            float ModifiedDamage;
            if (FlatDefense + 1 < damage)
            {
                ModifiedDamage = damage - FlatDefense;
            }
            else if (damage > 1)
            {
                ModifiedDamage = 1 / (FlatDefense - damage + 2);
            }
            else
            {
                ModifiedDamage = 1 / (FlatDefense + 1);
            }
            Health -= ModifiedDamage * (1 - PercentageDefense.Type(type));
        }
    }
}
