using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FaithburnEngine.Content;
using FaithburnEngine.Content.Models;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Chunk-based world grid for large worlds.
    /// Implements IWorldGrid for compatibility with systems.
    /// </summary>
    public sealed class ChunkedWorldGrid : IWorldGrid
    {
        public const int ChunkSize = 128; // tiles per chunk axis
        public int TileSize { get; } = 32; // pixels per tile

        private readonly Dictionary<Point, Chunk> _chunks = new();
        private readonly ContentLoader _content;

        // Track which chunks are currently loaded for streaming
        private readonly HashSet<Point> _loadedChunks = new();

        public ChunkedWorldGrid(ContentLoader content, int tileSize = 32)
        {
            _content = content;
            TileSize = tileSize;
        }

        /// <summary>
        /// Convert world position (pixels) to tile coordinate.
        /// </summary>
        public Point WorldToTileCoord(Vector2 worldPos)
        {
            return new Point(
                (int)MathF.Floor(worldPos.X / TileSize),
                (int)MathF.Floor(worldPos.Y / TileSize));
        }

        /// <summary>
        /// Convert tile coordinate to chunk coordinate.
        /// </summary>
        public static Point TileToChunkCoord(Point tileCoord)
        {
            // Handle negative coords correctly with floor division
            int cx = tileCoord.X >= 0 ? tileCoord.X / ChunkSize : (tileCoord.X - ChunkSize + 1) / ChunkSize;
            int cy = tileCoord.Y >= 0 ? tileCoord.Y / ChunkSize : (tileCoord.Y - ChunkSize + 1) / ChunkSize;
            return new Point(cx, cy);
        }

        /// <summary>
        /// Convert tile coordinate to local index within its chunk.
        /// </summary>
        public static Point TileToLocalCoord(Point tileCoord)
        {
            int lx = ((tileCoord.X % ChunkSize) + ChunkSize) % ChunkSize;
            int ly = ((tileCoord.Y % ChunkSize) + ChunkSize) % ChunkSize;
            return new Point(lx, ly);
        }

        /// <summary>
        /// Get or create chunk at chunk coordinate.
        /// </summary>
        public Chunk GetOrCreateChunk(Point chunkCoord)
        {
            if (!_chunks.TryGetValue(chunkCoord, out var chunk))
            {
                chunk = new Chunk { Coord = chunkCoord };
                _chunks[chunkCoord] = chunk;
                _loadedChunks.Add(chunkCoord);
            }
            return chunk;
        }

        /// <summary>
        /// Try to get chunk; returns null if not loaded.
        /// </summary>
        public Chunk? GetChunk(Point chunkCoord)
        {
            return _chunks.TryGetValue(chunkCoord, out var chunk) ? chunk : null;
        }

        /// <summary>
        /// Set block at tile coordinate.
        /// </summary>
        public void SetBlock(Point tileCoord, ushort blockId, byte flags = 0)
        {
            var chunkCoord = TileToChunkCoord(tileCoord);
            var localCoord = TileToLocalCoord(tileCoord);
            var chunk = GetOrCreateChunk(chunkCoord);
            int idx = localCoord.Y * ChunkSize + localCoord.X;
            chunk.Tiles[idx] = new Tile(blockId, flags);
        }

        /// <summary>
        /// Remove block at tile coordinate (set to air).
        /// WHY separate from SetBlock: Semantic clarity for mining/destruction logic.
        /// </summary>
        public void RemoveBlock(Point tileCoord)
        {
            SetBlock(tileCoord, 0); // 0 = air
        }

        /// <summary>
        /// Set block by string ID (looks up in content).
        /// </summary>
        public void SetBlock(Point tileCoord, string blockId)
        {
            // For now, use a simple hash as the block ID
            // In production, maintain a string->ushort registry
            ushort id = (ushort)(blockId.GetHashCode() & 0xFFFF);
            if (blockId == "air") id = 0;
            else if (blockId == "grass_dirt") id = 1;
            SetBlock(tileCoord, id);
        }

        /// <summary>
        /// Get tile at tile coordinate.
        /// </summary>
        public Tile GetTile(Point tileCoord)
        {
            var chunkCoord = TileToChunkCoord(tileCoord);
            var chunk = GetChunk(chunkCoord);
            if (chunk == null) return new Tile(0, 0); // air
            var localCoord = TileToLocalCoord(tileCoord);
            int idx = localCoord.Y * ChunkSize + localCoord.X;
            return chunk.Tiles[idx];
        }

        /// <summary>
        /// Check if tile is solid.
        /// </summary>
        public bool IsSolidTile(Point tileCoord)
        {
            var tile = GetTile(tileCoord);
            return tile.Id != 0; // anything non-air is solid for now
        }

        /// <summary>
        /// Get block definition at tile coordinate.
        /// </summary>
        public BlockDef GetBlock(Point tileCoord)
        {
            var tile = GetTile(tileCoord);
            if (tile.Id == 0) return new BlockDef { Id = "air", Solid = false };
            if (tile.Id == 1) return _content.GetBlock("grass_dirt") ?? new BlockDef { Id = "grass_dirt", Solid = true };
            return new BlockDef { Id = "unknown", Solid = true };
        }

        /// <summary>
        /// Return true if a world-space feet position should be considered grounded.
        /// </summary>
        public bool IsGrounded(Vector2 footWorldPosition, float epsilon = 2f)
        {
            var checkPos = new Vector2(footWorldPosition.X, footWorldPosition.Y + epsilon);
            var tile = WorldToTileCoord(checkPos);
            return IsSolidTile(tile);
        }

        /// <summary>
        /// Update loaded chunks based on player position.
        /// Loads chunks within radius, unloads chunks outside.
        /// </summary>
        public void UpdateLoadedChunks(Vector2 playerWorldPos, int loadRadius = 2)
        {
            var playerTile = WorldToTileCoord(playerWorldPos);
            var playerChunk = TileToChunkCoord(playerTile);

            // Determine which chunks should be loaded
            var shouldBeLoaded = new HashSet<Point>();
            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            {
                for (int dx = -loadRadius; dx <= loadRadius; dx++)
                {
                    shouldBeLoaded.Add(new Point(playerChunk.X + dx, playerChunk.Y + dy));
                }
            }

            // Don't unload chunks during initial generation - only unload if we have many chunks
            // This prevents unloading the entire world before the player spawns
            if (_chunks.Count > (loadRadius * 2 + 1) * (loadRadius * 2 + 1) * 2)
            {
                // Unload chunks that are too far
                var toUnload = new List<Point>();
                foreach (var coord in _loadedChunks)
                {
                    if (!shouldBeLoaded.Contains(coord))
                    {
                        toUnload.Add(coord);
                    }
                }
                foreach (var coord in toUnload)
                {
                    // Could serialize to disk here for persistence
                    _chunks.Remove(coord);
                    _loadedChunks.Remove(coord);
                }
            }

            // Note: New chunks in shouldBeLoaded will be created on demand when accessed
        }

        /// <summary>
        /// Get the Y index of the top-most solid tile in the given column.
        /// </summary>
        public int GetTopMostSolidTileY(int x)
        {
            // Search through loaded chunks
            int? minY = null;
            foreach (var kvp in _chunks)
            {
                var chunk = kvp.Value;
                int chunkTileX = chunk.Coord.X * ChunkSize;
                int chunkTileY = chunk.Coord.Y * ChunkSize;

                // Check if this column is in this chunk
                int localX = x - chunkTileX;
                if (localX < 0 || localX >= ChunkSize) continue;

                for (int localY = 0; localY < ChunkSize; localY++)
                {
                    int idx = localY * ChunkSize + localX;
                    if (chunk.Tiles[idx].Id != 0)
                    {
                        int tileY = chunkTileY + localY;
                        if (!minY.HasValue || tileY < minY.Value)
                        {
                            minY = tileY;
                        }
                    }
                }
            }
            return minY ?? 0;
        }

        /// <summary>
        /// Get minimum and maximum X tile coordinates in loaded chunks.
        /// </summary>
        public int GetMinX()
        {
            int? min = null;
            foreach (var chunk in _chunks.Values)
            {
                int chunkMinX = chunk.Coord.X * ChunkSize;
                if (!min.HasValue || chunkMinX < min.Value) min = chunkMinX;
            }
            return min ?? 0;
        }

        public int GetMaxX()
        {
            int? max = null;
            foreach (var chunk in _chunks.Values)
            {
                int chunkMaxX = (chunk.Coord.X + 1) * ChunkSize - 1;
                if (!max.HasValue || chunkMaxX > max.Value) max = chunkMaxX;
            }
            return max ?? 0;
        }

        /// <summary>
        /// Get variant for smart tiling.
        /// </summary>
        public TileVariant GetVariant(Point tileCoord)
        {
            bool isSolid(Point p) => IsSolidTile(p);
            var topSolid = isSolid(tileCoord + new Point(0, -1));
            var rightSolid = isSolid(tileCoord + new Point(1, 0));
            var bottomSolid = isSolid(tileCoord + new Point(0, 1));
            var leftSolid = isSolid(tileCoord + new Point(-1, 0));
            var idx = TileVariant.CalculateVariant(topSolid, rightSolid, bottomSolid, leftSolid);
            return new TileVariant(0, idx);
        }

        /// <summary>
        /// Recalculate all variants in loaded chunks.
        /// </summary>
        public void RecalculateAllVariants()
        {
            // Variants are computed on-demand in GetVariant
        }

        /// <summary>
        /// Get all loaded chunk coordinates.
        /// </summary>
        public IEnumerable<Point> GetLoadedChunkCoords() => _loadedChunks;

        /// <summary>
        /// Get all tiles in a chunk for rendering.
        /// </summary>
        public IEnumerable<(Point TileCoord, Tile Tile)> GetTilesInChunk(Point chunkCoord)
        {
            var chunk = GetChunk(chunkCoord);
            if (chunk == null) yield break;

            int baseX = chunkCoord.X * ChunkSize;
            int baseY = chunkCoord.Y * ChunkSize;

            for (int ly = 0; ly < ChunkSize; ly++)
            {
                for (int lx = 0; lx < ChunkSize; lx++)
                {
                    int idx = ly * ChunkSize + lx;
                    var tile = chunk.Tiles[idx];
                    if (tile.Id != 0)
                    {
                        yield return (new Point(baseX + lx, baseY + ly), tile);
                    }
                }
            }
        }
    }
}
