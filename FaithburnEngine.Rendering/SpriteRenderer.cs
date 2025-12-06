using FaithburnEngine.Components;
using FaithburnEngine.Core;
using FaithburnEngine.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Content.Models;

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
        private readonly IWorldGrid _worldGrid;
        private readonly int _tileSize;
        private readonly Camera2D _camera;
        private readonly System.Func<string, FaithburnEngine.Content.Models.ItemDef?>? _itemLookup;
        private readonly AssetRegistry? _assets;
        
        // 1x1 white pixel texture for drawing solid colored rectangles
        private Texture2D? _whitePixel;

        /// <summary>
        /// Creates a new SpriteRenderer.
        /// </summary>
        /// <param name="world">ECS world containing entities to render.</param>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="blockAtlas">Atlas for block tile variants.</param>
        /// <param name="worldGrid">World grid for tile lookup.</param>
        /// <param name="camera">Camera for view transformation.</param>
        /// <param name="itemLookup">Optional function to look up ItemDef by item ID.</param>
        /// <param name="assets">Optional AssetRegistry for loading item textures.</param>
        /// <param name="tileSize">Tile size in pixels (default 32).</param>
        public SpriteRenderer(
            DefaultEcs.World world, 
            SpriteBatch spriteBatch, 
            BlockAtlas blockAtlas, 
            IWorldGrid worldGrid, 
            Camera2D camera, 
            System.Func<string, FaithburnEngine.Content.Models.ItemDef?>? itemLookup = null,
            AssetRegistry? assets = null,
            int tileSize = 32)
        {
            _world = world;
            _spriteBatch = spriteBatch;
            _blockAtlas = blockAtlas;
            _worldGrid = worldGrid;
            _tileSize = tileSize;
            _camera = camera;
            _itemLookup = itemLookup;
            _assets = assets;
            
            // Create 1x1 white pixel texture for drawing solid colored shapes
            _whitePixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
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

            // Draw dropped items (above tiles, below entities)
            DrawDroppedItems();

            // Draw ECS entities (foreground layer)
            DrawEntities();

            _spriteBatch.End();
        }

        /// <summary>
        /// Draw dropped items in the world.
        /// WHY separate from DrawEntities: Dropped items don't have Sprite components,
        /// they use the item's icon texture based on ItemId looked up via _itemLookup and _assets.
        /// </summary>
        private void DrawDroppedItems()
        {
            if (_whitePixel == null) return;

            foreach (var e in _world.GetEntities().With<Position>().With<DroppedItem>().AsEnumerable())
            {
                ref var pos = ref e.Get<Position>();
                ref var drop = ref e.Get<DroppedItem>();

                Texture2D? icon = null;

                // Try to look up the item's sprite texture
                if (_itemLookup != null && _assets != null)
                {
                    var itemDef = _itemLookup(drop.ItemId);
                    if (itemDef != null && !string.IsNullOrEmpty(itemDef.SpriteKey))
                    {
                        _assets.TryGetTexture(itemDef.SpriteKey, out icon);
                    }
                }

                if (icon != null)
                {
                    // Draw the item icon centered at position
                    // WHY center origin: Dropped items should appear centered at their world position
                    var origin = new Vector2(icon.Width / 2f, icon.Height / 2f);
                    
                    // Add slight bobbing animation for visual interest
                    float bob = MathF.Sin((float)Environment.TickCount / 300f) * 2f;
                    var drawPos = pos.Value + new Vector2(0f, bob - 8f); // Offset up slightly from feet position
                    
                    // Tint yellow when magnetized
                    Color tint = Color.White;
                    if (drop.IsMagnetized)
                    {
                        float pulse = MathF.Sin((float)Environment.TickCount / 100f) * 0.3f + 0.7f;
                        tint = Color.Lerp(Color.White, Color.Yellow, pulse * 0.5f);
                    }
                    
                    _spriteBatch.Draw(icon, drawPos, null, tint, 0f, origin, 1f, SpriteEffects.None, 0f);
                }
                else
                {
                    // Fallback: draw colored square if no texture found
                    var rect = new Rectangle(
                        (int)(pos.Value.X - 8),
                        (int)(pos.Value.Y - 16),
                        16, 16
                    );

                    Color color = drop.ItemId switch
                    {
                        "dirt" => new Color(139, 90, 43),
                        "grass_dirt" => new Color(86, 125, 70),
                        _ => Color.White
                    };

                    if (drop.IsMagnetized)
                    {
                        float pulse = MathF.Sin((float)Environment.TickCount / 100f) * 0.3f + 0.7f;
                        color = Color.Lerp(color, Color.Yellow, pulse * 0.5f);
                    }

                    _spriteBatch.Draw(_whitePixel, rect, color);
                }
            }
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

                var tex = sprite.Texture;
                var tint = sprite.Tint == default ? Color.White : sprite.Tint;
                var scale = sprite.Scale <= 0f ? 1f : sprite.Scale;
                var effects = sprite.Effects;

                // Decide whether this texture should be treated as a column spritesheet.
                // Artists supply most character sheets as columns with left/top padding and fixed stride.
                bool looksLikeColumnSheet = tex.Height >= Constants.Spritesheet.DefaultFrameStrideY && tex.Width >= Constants.Spritesheet.DefaultLeftPadding + 1;
                if (looksLikeColumnSheet)
                {
                    // Derive frame metrics using defaults but try several candidate strides so sheets with
                    // different bottom padding (or none) are still recognized (e.g., 80x2352 uses stride 112).
                    int frameWidth = Math.Max(1, tex.Width - Constants.Spritesheet.DefaultLeftPadding);
                    int frameHeight = Constants.Spritesheet.DefaultFrameHeight;

                    int[] strideCandidates = new[] {
                        Constants.Spritesheet.DefaultFrameStrideY, // default (116)
                        Constants.Spritesheet.DefaultTopPadding + Constants.Spritesheet.DefaultFrameHeight, // top + frame (112)
                        Constants.Spritesheet.DefaultFrameHeight + Constants.Spritesheet.DefaultBottomPadding, // frame + bottom (100)
                        Constants.Spritesheet.DefaultFrameHeight // frame only (96)
                    };

                    int strideY = tex.Height; // fallback: single-frame entire texture
                    int frameCount = 1;
                    foreach (var cand in strideCandidates)
                    {
                        if (cand <= 0) continue;
                        if (tex.Height % cand == 0)
                        {
                            strideY = cand;
                            frameCount = tex.Height / strideY;
                            break;
                        }
                    }

                    // Ensure at least 1
                    frameCount = Math.Max(1, frameCount);

                    // Determine which frame to draw: idle -> frame 0; walking -> alternate between frames 1 and 2
                    int frameIndex = 0;
                    bool walking = false;
                    if (e.Has<FaithburnEngine.Components.Velocity>())
                    {
                        var vel = e.Get<FaithburnEngine.Components.Velocity>().Value;
                        walking = Math.Abs(vel.X) > Constants.Spritesheet.WalkVelocityThreshold;
                    }

                    if (walking && frameCount > 1)
                    {
                        double t = Environment.TickCount / 1000.0;
                        int phase = (int)(t / Constants.Spritesheet.WalkFrameInterval) % 2;
                        frameIndex = 1 + phase;
                        if (frameIndex >= frameCount) frameIndex = frameCount - 1;
                    }
                    else
                    {
                        frameIndex = 0;
                    }

                    // Source rectangle for this frame (column layout). Include bottom padding area so
                    // the drawn region contains the sprite's feet area and padding, preventing a visible gap.
                    int srcX = Constants.Spritesheet.DefaultLeftPadding;
                    int srcY = Constants.Spritesheet.DefaultTopPadding + frameIndex * strideY;
                    int srcHeight = frameHeight + Math.Max(0, strideY - (Constants.Spritesheet.DefaultTopPadding + Constants.Spritesheet.DefaultFrameHeight));
                    // Clamp srcHeight to remaining texture height
                    if (srcY + srcHeight > tex.Height) srcHeight = Math.Max(0, tex.Height - srcY);
                    var srcRect = new Rectangle(srcX, srcY, frameWidth, srcHeight);

                    // Compute bottom padding present in the sheet for this stride candidate
                    int bottomPaddingUsed = Math.Max(0, strideY - (Constants.Spritesheet.DefaultTopPadding + Constants.Spritesheet.DefaultFrameHeight));

                    // Origin relative to source rect (bottom-center of frame). Include bottom padding so
                    // the sprite's feet align to the entity's feet world position (removes 4px gap).
                    var originForDraw = new Vector2(frameWidth * 0.5f, frameHeight + bottomPaddingUsed);

                    // Draw frame so that origin maps to entity position.
                    // Apply small downward adjustment equal to DefaultBottomPadding to compensate for common artist padding
                    var drawPos = pos.Value + new Vector2(0f, Constants.Spritesheet.DefaultBottomPadding);
                    _spriteBatch.Draw(tex, drawPos, srcRect, tint, 0f, originForDraw, scale, effects, 0f);
                }
                else
                {
                    // Fallback: single texture draw (legacy behavior)
                    var origin = sprite.Origin;
                    _spriteBatch.Draw(sprite.Texture, pos.Value, null, tint, 0f, origin, scale, effects, 0f);
                }

                // If entity is holding an item, draw it on top of the entity
                if (e.Has<HeldItem>())
                {
                    ref var hi = ref e.Get<HeldItem>();
                    if (hi.Texture != null)
                    {
                        var heldTex = hi.Texture;

                        // Player/NPC/Enemy art faces LEFT by default; items are sprited facing RIGHT.
                        // Mirror item to match the player's facing:
                        // - When player faces RIGHT (FlipHorizontally), draw item as-is (no flip).
                        // - When player faces LEFT (no flip), flip item horizontally so it faces LEFT.
                        SpriteEffects heldEffects = SpriteEffects.None;
                        if (e.Has<Sprite>())
                        {
                            var baseEffects = e.Get<Sprite>().Effects;
                            heldEffects = baseEffects.HasFlag(SpriteEffects.FlipHorizontally)
                                ? SpriteEffects.None
                                : SpriteEffects.FlipHorizontally;
                        }

                        var pivotWorldPos = pos.Value + hi.Offset;
                        var pivot = hi.Pivot;

                        // Adjust origin for horizontal flip so rotation/pivot remain correct
                        var originForDraw = pivot;
                        if (heldEffects.HasFlag(SpriteEffects.FlipHorizontally))
                            originForDraw = new Vector2(heldTex.Width - pivot.X, pivot.Y);

                        float rot = hi.Rotation;
                        if (heldEffects.HasFlag(SpriteEffects.FlipHorizontally)) rot = -rot;

                        _spriteBatch.Draw(heldTex, pivotWorldPos, null, Color.White, rot, originForDraw, hi.Scale, heldEffects, 0f);
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
