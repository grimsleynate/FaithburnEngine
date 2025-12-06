using Microsoft.Xna.Framework;
using System.Collections.Generic;
using FaithburnEngine.Content.Models;
using FaithburnEngine.Content;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Simple tile grid backed by dictionary.
    /// Implements IWorldGrid for compatibility with systems.
    /// </summary>
    public sealed class WorldGrid : IWorldGrid
    {
        private readonly Dictionary<Point, string> _blockIds = new();
        private readonly Dictionary<Point, TileVariant> _variants = new();
        private readonly ContentLoader _content;
        public int TileSize { get; } = 32; // pixels per tile
        // WHY 32px tiles:
        // - Matches common 2D platformer standards and content pipeline.
        // - Reasonable for memory/perf; easy atlas packing.
        // - Can be parameterized later if modders want different scales.

        public WorldGrid(ContentLoader content)
        {
            _content = content;
        }

        /// <summary>
        /// Convert world position (pixels) to tile coordinate.
        /// </summary>
        public Point WorldToTileCoord(Vector2 worldPos)
        {
            return new Point(
                (int)(worldPos.X / TileSize),
                (int)(worldPos.Y / TileSize));
        }

        /// <summary>
        /// Get block definition at tile coordinate.
        /// Returns air if no tile exists.
        /// </summary>
        public BlockDef GetBlock(Point coord)
        {
            var id = _blockIds.TryGetValue(coord, out var blockId) ? blockId : "air";
            var def = _content.GetBlock(id);
            return def ?? new BlockDef { Id = "air", Solid = false };
        }

        /// <summary>
        /// Return true if the tile at coord is considered solid. This treats tiles with an id
        /// present in the grid as solid even if their BlockDef could not be loaded, to avoid
        /// physics falling through when content lookup fails.
        /// </summary>
        public bool IsSolidTile(Point coord)
        {
            if (!_blockIds.TryGetValue(coord, out var id)) return false;
            if (string.IsNullOrEmpty(id) || id == "air") return false;
            var def = _content.GetBlock(id);
            if (def != null) return def.Solid;
            // If we have an id but no BlockDef, assume it's solid (safer fallback)
            return true;
        }

        /// <summary>
        /// Get the variant (sprite selection) for a tile.
        /// Used by renderer to pick correct sprite from atlas.
        /// </summary>
        public TileVariant GetVariant(Point coord)
        {
            return _variants.TryGetValue(coord, out var variant) ? variant : default;
        }

        /// <summary>
        /// Set block at coordinate and recalculate its variant + neighbors.
        /// </summary>
        public void SetBlock(Point coord, string blockId)
        {
            _blockIds[coord] = blockId;
            RecalculateVariant(coord);

            // Neighbor changes affect their variants too
            RecalculateVariant(coord + new Point(0, -1)); // top
            RecalculateVariant(coord + new Point(1, 0));  // right
            RecalculateVariant(coord + new Point(0, 1));  // bottom
            RecalculateVariant(coord + new Point(-1, 0)); // left
        }

        /// <summary>
        /// Try to place a block. Returns false if tile already occupied.
        /// </summary>
        public bool PlaceBlock(Point coord, string blockId)
        {
            var current = GetBlock(coord);
            if (current.Id == "air")
            {
                SetBlock(coord, blockId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Recalculate variant for a single tile based on neighbors.
        /// Called when tile or neighbors change.
        /// </summary>
        private void RecalculateVariant(Point coord)
        {
            var blockId = _blockIds.TryGetValue(coord, out var id) ? id : "air";
            var blockDef = _content.GetBlock(blockId);

            if (blockDef?.Id == "air")
            {
                // Air tiles don't need variants
                _variants.Remove(coord);
                return;
            }

            if (blockDef == null) return;

            // Check if neighbors are solid
            bool isSolid(Point p) => _content.GetBlock(_blockIds.TryGetValue(p, out var bid) ? bid : "air")?.Solid ?? false;

            var topSolid = isSolid(coord + new Point(0, -1));
            var rightSolid = isSolid(coord + new Point(1, 0));
            var bottomSolid = isSolid(coord + new Point(0, 1));
            var leftSolid = isSolid(coord + new Point(-1, 0));

            var variantIdx = TileVariant.CalculateVariant(topSolid, rightSolid, bottomSolid, leftSolid);
            _variants[coord] = new TileVariant(0, variantIdx); // BlockId=0 for now, could store actual ID
        }

        /// <summary>
        /// Recalculate all tile variants. Call after bulk generation.
        /// TENET #3 (Efficient): Only call once during init, not per frame.
        /// </summary>
        public void RecalculateAllVariants()
        {
            var coordsCopy = new List<Point>(_blockIds.Keys);
            foreach (var coord in coordsCopy)
            {
                RecalculateVariant(coord);
            }
        }

        /// <summary>
        /// Get the Y index of the top-most solid tile in the given column (X).
        /// Returns 0 if no tile is present. Used for spawning entities on the surface.
        /// </summary>
        public int GetTopMostSolidTileY(int x)
        {
            int? minY = null;
            foreach (var kvp in _blockIds)
            {
                if (kvp.Key.X != x) continue;
                var def = _content.GetBlock(kvp.Value);
                if (def == null || !def.Solid) continue;
                if (!minY.HasValue || kvp.Key.Y < minY.Value) minY = kvp.Key.Y;
            }
            return minY ?? 0;
        }

        /// <summary>
        /// Get minimum X and maximum X coordinates currently stored in the world grid.
        /// Returns 0 for min/max if world is empty.
        /// </summary>
        public int GetMinX()
        {
            int? min = null;
            foreach (var p in _blockIds.Keys)
            {
                if (!min.HasValue || p.X < min.Value) min = p.X;
            }
            return min ?? 0;
        }

        public int GetMaxX()
        {
            int? max = null;
            foreach (var p in _blockIds.Keys)
            {
                if (!max.HasValue || p.X > max.Value) max = p.X;
            }
            return max ?? 0;
        }

        /// <summary>
        /// Return true if a world-space feet position should be considered grounded.
        /// Uses a small epsilon downward to account for floating point and integration.
        /// </summary>
        public bool IsGrounded(Vector2 footWorldPosition, float epsilon = 2f)
        {
            var checkPos = new Vector2(footWorldPosition.X, footWorldPosition.Y + epsilon);
            var tile = WorldToTileCoord(checkPos);
            return IsSolidTile(tile);
        }
    }
}