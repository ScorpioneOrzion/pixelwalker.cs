namespace digbot.Classes
{
    public enum DigbotPlayerRole
    {
        None,
        Immune,
        Owner,
    }

    public enum ActionType
    {
        Reveal,
        Mine,
        Drill,
        Freeze,
        Burn,
        Explode,
        Poison,
        Electrify,
        Transform,
    }

    public enum DamageType
    {
        Generic,
        Physical,
        Fire,
        Poison,
        Electric,
        Explosion,
    }

    public enum ItemType
    {
        Generic,
        Consumable,
        Armor,
        Weapon,
        Tool,
        Miscellaneous,
    }
}
