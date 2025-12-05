namespace FaithburnEngine.Content.Models.Enums
{
    [Flags]
    public enum ItemType : int
    {
        None = 0,
        Block = 1 << 0,   // 1
        Potion = 1 << 1,   // 2
        Misc = 1 << 2,   // 4
        Tool = 1 << 3,   // 8
        Weapon = 1 << 4,   // 16
        Equipment = 1 << 5,   // 32
        Accessory = 1 << 6,   // 64
        Workbench = 1 << 7,   // 128
        Decoration = 1 << 8,   // 256
        Food = 1 << 9    // 512
    }
}