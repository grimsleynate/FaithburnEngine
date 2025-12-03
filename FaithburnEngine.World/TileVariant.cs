namespace FaithburnEngine.World
{
    /// <summary>
    /// Tracks which sprite variant a tile should display.
    /// Calculated based on adjacent solid neighbors.
    /// 
    /// TENET #3 (Efficient):
    /// Stored per-tile, cached once during generation or neighbor change.
    /// No per-frame recalculation needed. O(1) lookup during render.
    /// </summary>
    public readonly record struct TileVariant(ushort BlockId, byte VariantIndex)
    {
        /// <summary>
        /// VariantIndex encodes neighbor topology as 4-bit mask:
        /// Bit 3 (8): Top neighbor is solid
        /// Bit 2 (4): Right neighbor is solid
        /// Bit 1 (2): Bottom neighbor is solid
        /// Bit 0 (1): Left neighbor is solid
        /// 
        /// Example: Top + Left solid = 0b1001 = 9
        /// </summary>
        public static byte CalculateVariant(bool top, bool right, bool bottom, bool left)
        {
            return (byte)(
                (top ? 8 : 0) |
                (right ? 4 : 0) |
                (bottom ? 2 : 0) |
                (left ? 1 : 0)
            );
        }
    }
}