namespace FaithburnEngine.Content.Models.Enums
{
    [Flags]
    public enum ToolType : int
    {
        None = 0,
        Pickaxe = 1 << 0,   // 1
        Axe = 1 << 1,   // 2
        Sickle = 1 << 2    // 4
    }
}