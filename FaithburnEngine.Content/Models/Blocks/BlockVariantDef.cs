namespace FaithburnEngine.Content.Models.Blocks
{
    /// <summary>
    /// Defines how a block displays its sprite based on neighbor tiles.
    /// Uses a 4-bit mask: Top, Right, Bottom, Left (0-15 variants).
    /// </summary>
    public sealed class BlockVariantDef
    {
        /// <summary>
        /// Block ID this variant set belongs to (e.g., "dirt_grass").
        /// </summary>
        public string BlockId { get; set; } = "";

        /// <summary>
        /// Spritesheet path. Must be laid out as 16xN tiles (16 types of tiles, N variants).
        /// </summary>
        public string SpriteSheetRef { get; set; } = "";

        /// <summary>
        /// Sprite size in pixels (e.g., 32 for 32x32 tiles).
        /// </summary>
        public int SpriteSize { get; set; } = 32;
    }
}