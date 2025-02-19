namespace digbot.Classes
{
    public enum DigbotPlayerRole
    {
        None,
        GameBot,
        GameDeveloper,
        GameAdmin,
        DIGBOT,
        Owner,
    }

    public enum ActionType
    {
        Unknown, // for temporary effects (or void type)
        Reveal, // for actions that reveal blocks
        Equip, // for actions that equip items
        Use, // for actions that use items
        AutoUse, // for actions that use items automatically
        Mine, // for actions that mine blocks
        Drill, // for actions that mine blocks
        Explode, // for actions that explode blocks
        Electrify, // for actions that electrify blocks
        Transform, // for actions that transform blocks
    }

    public enum ItemType
    {
        Unknown, // for temporary effects (or void type)
        Generic, // for items that have no specific type
        Consumable, // for items that can be used once
        Armor, // for items that can be equipped
        Weapon, // for items that can be equipped
        Tool, // for items that can be equipped
        Miscellaneous, // for items that don't fit in any other category
    }
}
