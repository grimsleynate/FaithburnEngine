using FaithburnEngine.Components;
using FaithburnEngine.Core;
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
        private readonly Camera2D _camera;
        private readonly System.Func<string, FaithburnEngine.Content.Models.ItemDef?>? _itemLookup;

        // itemLookup is an optional function that maps an ItemId to its ItemDef. If not provided,
        // renderer will fall back to simple heuristics (previous hardcoded behavior).
        public SpriteRenderer(DefaultEcs.World world, SpriteBatch spriteBatch, BlockAtlas blockAtlas, WorldGrid worldGrid, Camera2D camera, System.Func<string, FaithburnEngine.Content.Models.ItemDef?>? itemLookup = null, int tileSize = 32)
        {
            _world = world;
            _spriteBatch = spriteBatch;
            _blockAtlas = blockAtlas;
            _worldGrid = worldGrid;
            _tileSize = tileSize;
            _camera = camera;
            _itemLookup = itemLookup;
        }

        /// <summary>
        /// Render all world tiles and entities.
        /// Call from Game.Draw().
        /// </summary>
        public void Draw()
        {
            var view = _camera?.GetViewMatrix() ?? Matrix.Identity;

            _spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp, // crisp pixels
                transformMatrix: view
            );


            // Draw world tiles (background layer)
            DrawWorldTiles();

            // Draw ECS entities (foreground layer)
            DrawEntities();

            _spriteBatch.End();
        }

        private void DrawWorldTiles()
        {
            if (_spriteBatch == null || _camera == null || _blockAtlas == null || _worldGrid == null)
                return;

            var vp = _spriteBatch.GraphicsDevice.Viewport;
            var screenTopLeft = new Vector3(0, 0, 0);
            var screenBottomRight = new Vector3(vp.Width, vp.Height, 0);

            var view = _camera.GetViewMatrix();

            // Invert the view matrix correctly using ref/out
            Matrix inv;
            try
            {
                Matrix.Invert(ref view, out inv);

                // Transform screen corners to world space
                var topLeftWorld = Vector3.Transform(screenTopLeft, inv);
                var bottomRightWorld = Vector3.Transform(screenBottomRight, inv);

                // Convert to tile indices
                int leftTile = (int)Math.Floor(Math.Min(topLeftWorld.X, bottomRightWorld.X) / _tileSize);
                int rightTile = (int)Math.Ceiling(Math.Max(topLeftWorld.X, bottomRightWorld.X) / _tileSize);
                int topTile = (int)Math.Floor(Math.Min(topLeftWorld.Y, bottomRightWorld.Y) / _tileSize);
                int bottomTile = (int)Math.Ceiling(Math.Max(topLeftWorld.Y, bottomRightWorld.Y) / _tileSize);

                // Clamp to safe bounds (replace with world bounds if available)
                const int HARD_LIMIT = 100000;
                leftTile = Math.Max(leftTile, -HARD_LIMIT);
                topTile = Math.Max(topTile, -HARD_LIMIT);
                rightTile = Math.Min(rightTile, HARD_LIMIT);
                bottomTile = Math.Min(bottomTile, HARD_LIMIT);

                for (int y = topTile; y <= bottomTile; y++)
                {
                    for (int x = leftTile; x <= rightTile; x++)
                    {
                        var coord = new Point(x, y);
                        var block = _worldGrid.GetBlock(coord);
                        if (block == null || block.Id == "air") continue;

                        var variant = _worldGrid.GetVariant(coord);
                        var spriteData = _blockAtlas.GetSpriteForVariant(block.Id, variant.VariantIndex);
                        if (!spriteData.HasValue) continue;

                        var (spritesheet, srcRect) = spriteData.Value;
                        var destRect = new Rectangle(x * _tileSize, y * _tileSize, _tileSize, _tileSize);
                        _spriteBatch.Draw(spritesheet, destRect, srcRect, Color.White);
                    }
                }
            }
            catch
            {
                // Fallback: approximate using camera.Position, Origin and Zoom if inversion fails
                float zoom = Math.Max(0.0001f, _camera.Zoom);
                var origin = _camera.Origin;
                var leftWorld = (_camera.Position.X - origin.X);
                var topWorld = (_camera.Position.Y - origin.Y);
                var rightWorld = leftWorld + vp.Width / zoom;
                var bottomWorld = topWorld + vp.Height / zoom;

                int leftTile = (int)Math.Floor(leftWorld / _tileSize);
                int rightTile = (int)Math.Ceiling(rightWorld / _tileSize);
                int topTile = (int)Math.Floor(topWorld / _tileSize);
                int bottomTile = (int)Math.Ceiling(bottomWorld / _tileSize);

                const int HARD_LIMIT = 100000;
                leftTile = Math.Max(leftTile, -HARD_LIMIT);
                topTile = Math.Max(topTile, -HARD_LIMIT);
                rightTile = Math.Min(rightTile, HARD_LIMIT);
                bottomTile = Math.Min(bottomTile, HARD_LIMIT);

                for (int y = topTile; y <= bottomTile; y++)
                {
                    for (int x = leftTile; x <= rightTile; x++)
                    {
                        var coord = new Point(x, y);
                        var block = _worldGrid.GetBlock(coord);
                        if (block == null || block.Id == "air") continue;

                        var variant = _worldGrid.GetVariant(coord);
                        var spriteData = _blockAtlas.GetSpriteForVariant(block.Id, variant.VariantIndex);
                        if (!spriteData.HasValue) continue;

                        var (spritesheet, srcRect) = spriteData.Value;
                        var destRect = new Rectangle(x * _tileSize, y * _tileSize, _tileSize, _tileSize);
                        _spriteBatch.Draw(spritesheet, destRect, srcRect, Color.White);
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
                var effects = sprite.Effects;
                _spriteBatch.Draw(sprite.Texture, pos.Value, null, tint, 0f, origin, scale, effects, 0f);

                // If entity is holding an item, draw it on top of the entity
                if (e.Has<HeldItem>())
                {
                    ref var hi = ref e.Get<HeldItem>();
                    if (hi.Texture != null)
                    {
                        var tex = hi.Texture;

                        // Determine facing from entity sprite
                        float facing = 1f;
                        if (e.Has<Sprite>())
                        {
                            ref var sp = ref e.Get<Sprite>();
                            // Sprite.Effects: FlipHorizontally means artwork flipped -> player faces RIGHT (art faces left by default)
                            facing = (sp.Effects.HasFlag(SpriteEffects.FlipHorizontally)) ? 1f : -1f;
                        }

                        // Pivot in texture pixels; after scaling, pivot world offset = Pivot * Scale
                        var pivot = hi.Pivot;
                        var scaledPivot = pivot * hi.Scale;

                        // Compute world position where the pivot should be placed: entity position + Offset
                        var pivotWorldPos = pos.Value + hi.Offset;

                        // Determine flip behavior from ItemDef if available, otherwise follow existing logic.
                        SpriteEffects heldEffects = SpriteEffects.None;
                        FaithburnEngine.Content.Models.ItemDef? itemDef = null;
                        if (!string.IsNullOrEmpty(hi.ItemId) && _itemLookup != null)
                        {
                            try { itemDef = _itemLookup(hi.ItemId); } catch { itemDef = null; }
                        }

                        if (e.Has<Sprite>())
                        {
                            var baseEffects = e.Get<Sprite>().Effects;
                            heldEffects = baseEffects;
                            if (itemDef != null)
                            {
                                if (itemDef.FlipToFacePlayer)
                                    heldEffects = baseEffects ^ SpriteEffects.FlipHorizontally;
                                // else keep baseEffects
                            }
                            else
                            {
                                // Legacy fallback: only special-case proto_pickaxe
                                if (!string.IsNullOrEmpty(hi.ItemId) && hi.ItemId == "proto_pickaxe")
                                    heldEffects = baseEffects ^ SpriteEffects.FlipHorizontally;
                            }
                        }

                        // Compute originForDraw using pivot override from ItemDef if provided; otherwise use HeldItem.Pivot
                        pivot = hi.Pivot;
                        if (itemDef != null && itemDef.PivotX.HasValue && itemDef.PivotY.HasValue)
                        {
                            pivot = new Vector2(itemDef.PivotX.Value, itemDef.PivotY.Value);
                        }

                        var originForDraw = pivot;
                        if (heldEffects.HasFlag(SpriteEffects.FlipHorizontally))
                        {
                            originForDraw = new Vector2(tex.Width - pivot.X, pivot.Y);
                        }

                        // Determine rotation accounting for flip so swing direction matches facing
                        float rot = hi.Rotation;
                        if (heldEffects.HasFlag(SpriteEffects.FlipHorizontally)) rot = -rot;

                        // Draw texture so that pivot (originForDraw) maps to pivotWorldPos
                        _spriteBatch.Draw(tex, pivotWorldPos, null, Color.White, rot, originForDraw, hi.Scale, heldEffects, 0f);
                     }
                 }
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
