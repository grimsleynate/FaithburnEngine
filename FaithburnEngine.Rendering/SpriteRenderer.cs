using FaithburnEngine.Components;
using FaithburnEngine.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Rendering
{
    /// <summary>
    /// Renders entities and world tiles to screen.
    /// Handles both ECS sprites and smart-tiled world blocks.
    /// </summary>
    public sealed class SpriteRenderer
    {
        private readonly DefaultEcs.World _world;
        private readonly SpriteBatch _spriteBatch;
        private readonly BlockAtlas _blockAtlas;
        private readonly WorldGrid _worldGrid;
        private readonly int _tileSize;
        public SpriteRenderer(DefaultEcs.World world, SpriteBatch spriteBatch, BlockAtlas blockAtlas, WorldGrid worldGrid, int tileSize = 32)
        {
            _world = world;
            _spriteBatch = spriteBatch;
            _blockAtlas = blockAtlas;
            _worldGrid = worldGrid;
            _tileSize = tileSize;
        }

        /// <summary>
        /// Render all world tiles and entities.
        /// Call from Game.Draw().
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.Identity);

            // Draw world tiles (background layer)
            DrawWorldTiles();

            // Draw ECS entities (foreground layer)
            DrawEntities();

            _spriteBatch.End();
        }

        /// <summary>
        /// Draw all visible world tiles using smart-tiling variants.
        /// TENET #3 (Efficient): Only draw tiles in viewport (future optimization).
        /// </summary>
        private void DrawWorldTiles()
        {
            // TODO: Cull tiles outside viewport for performance
            // For now, draw all tiles in the world (PoC)
            var allCoords = new List<Point>();

            // Gather all tile coordinates from world grid
            // (This is a limitation of dict-backed storage; chunked array would be faster)
            // For PoC, assume world is reasonably small
            for (int x = -100; x < 100; x++)
            {
                for (int y = -100; y < 100; y++)
                {
                    var coord = new Point(x, y);
                    var block = _worldGrid.GetBlock(coord);
                    if (block.Id != "air")
                    {
                        var variant = _worldGrid.GetVariant(coord);
                        // Fix ambiguous call by explicitly casting variant.VariantIndex to byte
                        var spriteData = _blockAtlas.GetSpriteForVariant(block.Id, variant.VariantIndex);
                        if (spriteData.HasValue)
                        {
                            var (spritesheet, srcRect) = spriteData.Value;
                            var destRect = new Rectangle(
                                coord.X * _tileSize,
                                coord.Y * _tileSize,
                                _tileSize,
                                _tileSize
                            );
                            _spriteBatch.Draw(spritesheet, destRect.Location.ToVector2(), srcRect, Color.White);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw ECS entities (player, enemies, etc).
        /// </summary>
        private void DrawEntities()
        {
            foreach (var e in _world.GetEntities().With<Position>().With<Sprite>().AsEnumerable())
            {
                ref var pos = ref e.Get<Position>();
                ref var sprite = ref e.Get<Sprite>();

                if (sprite.Texture == null) continue;

                var origin = sprite.Origin;
                var tint = sprite.Tint == default ? Color.White : sprite.Tint;
                var scale = sprite.Scale <= 0f ? 1f : sprite.Scale;

                _spriteBatch.Draw(sprite.Texture, pos.Value, null, tint, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        public void DrawDebugRect(Texture2D whitePixel, Rectangle dest, Color color)
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(whitePixel, dest, color);
            _spriteBatch.End();
        }
    }
    // Rename one of the _tileSize fields in SpriteRenderer or BlockAtlas to resolve ambiguity.
    // Here, rename BlockAtlas._tileSize to _blockTileSize and update all references.

    public sealed class BlockAtlas
    {
        private readonly Dictionary<string, BlockAtlasEntry> _atlases = new();
        private readonly int _blockTileSize;
        private readonly int _tilePadding;

        public struct BlockAtlasEntry
        {
            public Texture2D Spritesheet;
            public int TilesWide;
            public int TileSize;
            public int Padding;
            public Rectangle[] SourceRects;
        }

        public BlockAtlas(int tileSize, int tilePadding = 0)
        {
            _blockTileSize = tileSize;
            _tilePadding = tilePadding;
        }

        public void RegisterAtlas(string blockId, Texture2D spritesheet, int tilesWide)
        {
            int effectiveTileStride = _blockTileSize + _tilePadding;
            int textureHeight = spritesheet.Height;
            int tilesTall = (textureHeight + _tilePadding) / effectiveTileStride;
            int totalTiles = tilesWide * tilesTall;

            var rects = new Rectangle[totalTiles];

            for (int i = 0; i < totalTiles; i++)
            {
                int col = i % tilesWide;
                int row = i / tilesWide;
                int x = col * effectiveTileStride;
                int y = row * effectiveTileStride;
                rects[i] = new Rectangle(x, y, _blockTileSize, _blockTileSize);
            }

            _atlases[blockId] = new BlockAtlasEntry
            {
                Spritesheet = spritesheet,
                TilesWide = tilesWide,
                TileSize = _blockTileSize,
                Padding = _tilePadding,
                SourceRects = rects
            };
        }

        public (Texture2D Spritesheet, Rectangle SourceRect)? GetSpriteForVariant(string blockId, byte variantIndex)
        {
            if (!_atlases.TryGetValue(blockId, out var entry))
                return null;
            if (variantIndex >= entry.SourceRects.Length)
                return null;
            return (entry.Spritesheet, entry.SourceRects[variantIndex]);
        }
    }
}
