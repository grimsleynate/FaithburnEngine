using Microsoft.Xna.Framework;
using FaithburnEngine.Content.Models;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Common interface for world grids (dictionary-backed or chunked).
    /// Allows systems to work with either implementation.
    /// </summary>
    public interface IWorldGrid
    {
        int TileSize { get; }
        Point WorldToTileCoord(Vector2 worldPos);
        bool IsSolidTile(Point tileCoord);
        bool IsGrounded(Vector2 footWorldPosition, float epsilon = 2f);
        int GetTopMostSolidTileY(int x);
        int GetMinX();
        int GetMaxX();
        BlockDef GetBlock(Point tileCoord);
        TileVariant GetVariant(Point tileCoord);
        
        /// <summary>
        /// Remove block at tile coordinate (set to air).
        /// WHY in interface: Mining/harvesting system needs to destroy blocks
        /// regardless of underlying world implementation.
        /// </summary>
        void RemoveBlock(Point tileCoord);
    }
}
