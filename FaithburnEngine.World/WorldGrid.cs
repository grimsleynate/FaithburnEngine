using Microsoft.Xna.Framework;
using System.Collections.Generic;
using FaithburnEngine.Content.Models;
using FaithburnEngine.Content;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Simple tile grid stub. Backed by dictionary for now.
    /// Replace with chunked array later.
    /// </summary>
    public sealed class WorldGrid
    {
        private readonly Dictionary<Point, string> _tiles = new();
        private readonly ContentLoader _content;
        public int TileSize { get; } = 32; // pixels per tile

        public WorldGrid(ContentLoader content)
        {
            _content = content;
        }

        public Point WorldToTileCoord(Vector2 worldPos)
        {
            return new Point(
                (int)(worldPos.X / TileSize),
                (int)(worldPos.Y / TileSize));
        }

        public BlockDef GetBlock(Point coord)
        {
            var id = _tiles.TryGetValue(coord, out var blockId) ? blockId : "air";
            var def = _content.Blocks.FirstOrDefault(b => b.Id == id);
            return def ?? new BlockDef { Id = "air", Solid = false };
        }


        public void SetBlock(Point coord, string blockId)
        {
            _tiles[coord] = blockId;
        }

        public bool PlaceBlock(Point coord, string blockId)
        {
            var current = GetBlock(coord);
            if (current.Id == "air")
            {
                _tiles[coord] = blockId;
                return true;
            }
            return false;
        }
    }
}