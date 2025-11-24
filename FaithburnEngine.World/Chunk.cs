using System.Drawing;

namespace FaithburnEngine.World
{
    public sealed class Chunk
    {
        public const int Size = 128;
        public Tile[] Tiles = new Tile[Size * Size];
        public Point Coord; //the chunk's coordinate
    }
}
