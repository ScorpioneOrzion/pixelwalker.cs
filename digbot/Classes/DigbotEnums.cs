namespace digbot.Classes
{
    public enum DigbotPlayerRole
    {
        None, // default role
        Immune, // for players who created the game or for bots that are used by those players
        Owner, // for the player who created the bot or the bot itself
    }

    public enum ActionType
    {
        Unknown, // for temporary effects
        Reveal, // for actions that reveal blocks
        Equip, // for actions that equip items
        Use, // for actions that use items
        AutoUse, // for actions that use items automatically
        Mine, // for actions that mine blocks
        Explode, // for actions that explode blocks
        Electrify, // for actions that electrify blocks
        Transform, // for actions that transform blocks
    }

    public enum ItemType
    {
        Unknown, // for temporary effects
        Generic, // for items that have no specific type
        Consumable, // for items that can be used once
        Armor, // for items that can be equipped
        Weapon, // for items that can be equipped
        Tool, // for items that can be equipped
        Miscellaneous, // for items that don't fit in any other category
    }
}
