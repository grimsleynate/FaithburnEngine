namespace FaithburnEngine.Content.Models.Enums
{
    public enum ItemType
    {
        None,                      //Not an item. Used very rarely.
        Block,                     //A breakable tile
        Potion,                    //A consumable. For the sake of sorting, food is also a potion.
        Misc,                      //Doesn't have a label. 
        Tool,                      //Used for resource gathering in some way
        Weapon,                    //Can cause damage to enemies
        Equipment,                 //Worn by the player to provide stats/bonuses
        Accessory,                 //Worn by the player to provide minor stats/bonuses and extra abilities
        Workbench,                 //Used to unlock crafting recipes
        Decoration,                //Purely aesthetic item
        Food,                      //A consumable that restores hunger/saturation and gives stat boosts
    }
}