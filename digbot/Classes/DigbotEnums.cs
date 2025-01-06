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
        Equip,
        Use,
        AutoUse,
        Mine,
        Explode,
        Electrify,
        Transform,
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
