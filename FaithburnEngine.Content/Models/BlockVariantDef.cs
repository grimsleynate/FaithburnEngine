namespace FaithburnEngine.Content.Models
{
    /// <summary>
    /// Defines how a block displays its sprite based on neighbor tiles.
    /// Uses a 4-bit mask: Top, Right, Bottom, Left (0-15 variants).
    /// 
    /// TENET #2 (Terraria-like):
    /// Smart tiling gives tiles visual polish without artist effort.
    /// Grass shows on exposed surfaces, dirt on buried edges.
    /// Reference: Terraria's 16-variant tiling system.
    /// </summary>
    public sealed class BlockVariantDef
    {
        /// <summary>
        /// Block ID this variant set belongs to (e.g., "dirt_grass").
        /// </summary>
        public string BlockId { get; set; } = "";

        /// <summary>
        /// Spritesheet path. Must be laid out as 4x4 grid (16 sprites).
        /// Layout matches Terraria convention:
        /// 
        ///  0  1  2  3
        ///  4  5  6  7
        ///  8  9 10 11
        /// 12 13 14 15
        /// 
        /// Variant index = (Top ? 8) | (Right ? 4) | (Bottom ? 2) | (Left ? 1)
        /// where ? means "solid neighbor in that direction"
        /// </summary>
        public string SpriteSheetRef { get; set; } = "";

        /// <summary>
        /// Sprite size in pixels (e.g., 32 for 32x32 tiles).
        /// Must match BlockDef.TileSize.
        /// </summary>
        public int SpriteSize { get; set; } = 32;
    }
}