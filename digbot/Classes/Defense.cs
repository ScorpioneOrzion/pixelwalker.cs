namespace digbot.Classes
{
    public class Defense
    {
        public required float Resistance = 0.0f;
        public required float Physical = 0.0f;
        public required float Fire = 0.0f;
        public required float Poison = 0.0f;
        public required float Electric = 0.0f;
        public required float Explosion = 0.0f;

        public float Type(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => Resistance + Physical,
                DamageType.Fire => Resistance + Fire,
                DamageType.Poison => Resistance + Poison,
                DamageType.Electric => Resistance + Electric,
                DamageType.Explosion => Resistance + Explosion,
                _ => Resistance,
            };
        }
    }
}
