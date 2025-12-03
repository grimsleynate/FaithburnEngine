//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework;

//namespace FaithburnEngine.Rendering
//{
//    /// <summary>
//    /// Manages block sprite atlases and variant lookups.
//    /// Maps BlockId → spritesheet → variant index → source rectangle.
//    /// 
//    /// TENET #3 (Efficient):
//    /// Pre-calculates all source rectangles at load time.
//    /// Rendering is O(1) lookup: variant.BlockId → variantIndex → Rectangle.
//    /// </summary>
//    public sealed class BlockAtlas
//    {
//        private readonly Dictionary<string, BlockAtlasEntry> _atlases = new();
//        private readonly int _tileSize;

//        public struct BlockAtlasEntry
//        {
//            public Texture2D Spritesheet;
//            public Rectangle[] SourceRects; // 16 rects, indexed by variant (0-15)
//        }

//        public BlockAtlas(int tileSize)
//        {
//            _tileSize = tileSize;
//        }

//        /// <summary>
//        /// Register a block's spritesheet and precalculate source rectangles.
//        /// Spritesheet must be 4x4 grid of tileSize×tileSize sprites.
//        /// </summary>
//        public void RegisterAtlas(string blockId, Texture2D spritesheet)
//        {
//            var rects = new Rectangle[16];
//            for (int i = 0; i < 16; i++)
//            {
//                int row = i / 4;
//                int col = i % 4;
//                rects[i] = new Rectangle(col * _tileSize, row * _tileSize, _tileSize, _tileSize);
//            }

//            _atlases[blockId] = new BlockAtlasEntry { Spritesheet = spritesheet, SourceRects = rects };
//        }

//        /// <summary>
//        /// Get source rectangle for a block variant.
//        /// Returns null if block or variant not found.
//        /// </summary>
//        public (Texture2D Spritesheet, Rectangle SourceRect)? GetSpriteForVariant(string blockId, byte variantIndex)
//        {
//            if (!_atlases.TryGetValue(blockId, out var entry))
//                return null;

//            if (variantIndex >= 16)
//                return null;

//            return (entry.Spritesheet, entry.SourceRects[variantIndex]);
//        }
//    }
//}