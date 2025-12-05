using DefaultEcs.System;
using DefaultEcs.Threading;
using FaithburnEngine.Content;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;
using FaithburnEngine.Systems;
using FaithburnEngine.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using DefaultEcs;

namespace FaithburnEngine.CoreGame
{
    public class Faithburn : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ContentLoader _contentLoader;
        private PlayerContext _player;
        private WorldGrid _worldGrid;
        private DefaultEcs.World _world;
        private SpriteRenderer _spriteRenderer;
        private InventorySystem _inventorySystem;
        private InteractionSystem _interactionSystem;
        private AssetLoader _assetLoader;
        private Camera2D _camera;
        private float desiredZoom = 1.0f; 

        // ECS pipeline fields
        private SequentialSystem<float> _systems;
        private DefaultParallelRunner _runner;

        // Keep a reference to the player entity so we can read position/velocity for camera follow
        private Entity _playerEntity;

        // Hotbar UI
        private HotbarRenderer _hotbarRenderer;
        private int _lastScrollValue;
        private const int HotbarDisplayCount = 10; // show 10 hotbar slots (1..0 keys)
        private const int HotbarSlotSize = 48;
        private const int HotbarPadding = 6;

        public Faithburn()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60 FPS
        }

        protected override void Initialize()
        {
            // Get display resolution
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            // Set backbuffer to match screen resolution
            _graphics.PreferredBackBufferWidth = displayMode.Width;
            _graphics.PreferredBackBufferHeight = displayMode.Height;
            _graphics.IsFullScreen = false; // keep border
            _graphics.ApplyChanges();

            // Position window at top-left
            Window.Position = new Point(0, 0);

            _world = new DefaultEcs.World();

            _runner = new DefaultParallelRunner(Environment.ProcessorCount);

            _camera = new Camera2D();
            _camera.UpdateOrigin(GraphicsDevice.Viewport);
            _camera.Zoom = desiredZoom;

            // Order matters for game feel:
            // 1. Input (read player input, buffer jump intent)
            // 2. Movement (apply velocity, gravity, resolve collisions, apply buffered jump)
            // 3. Interaction (sequential) ? Player can harvest blocks
            // 4. Inventory (sequential) ? UI shows changes
            // This creates the feel of a responsive, snappy game (like Terraria).
            // Modders extend this by creating new systems and injecting them here.
            _systems = new SequentialSystem<float>(
                // Run Input and Movement sequentially to avoid races on grounded state / jump intent
                new InputSystem(_world, worldGrid: null, speed: 180f),
                new MovementSystem(_world, worldGrid: null),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera),
                new InventorySystem(_contentLoader, _world)
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create player with 10 inventory slots (hotbar uses first 10)
            _player = new PlayerContext(10);

            _contentLoader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Models"));
            _contentLoader.LoadAll();

            _assetLoader = new AssetLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Assets"));

            _worldGrid = new WorldGrid(_contentLoader);
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Hotbar renderer depends on SpriteBatch, Content and UI assets
            Texture2D slotBg = null;
            SpriteFont uiFont = null;
            try { slotBg = Content.Load<Texture2D>("UI/slot_bg"); } catch { slotBg = null; }
            try { uiFont = Content.Load<SpriteFont>("Fonts/UiFont"); } catch { uiFont = null; }
            _hotbarRenderer = new HotbarRenderer(_spriteBatch, _contentLoader, slotBg, uiFont, GraphicsDevice);
            _lastScrollValue = Mouse.GetState().ScrollWheelValue;

            // Initialize block atlas and load sprite sheets
            var blockAtlas = new BlockAtlas(32, 4);
            // For now, assume it's loaded:
            var grassDirtSheet = Content.Load<Texture2D>("Assets/tiles/grass_dirt_variants");
            blockAtlas.RegisterAtlas("grass_dirt", grassDirtSheet, 16);

            // Generate world with flat terrain
            var generator = new WorldGenerator(
                widthInTiles: 1000,
                heightInTiles: 500,
                surfaceLevel: 50
            );
            generator.FillWorld(_worldGrid);

            _inventorySystem = new InventorySystem(_contentLoader, _world);
            _interactionSystem = new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera);
            _spriteRenderer = new SpriteRenderer(_world, _spriteBatch, blockAtlas, _worldGrid, _camera);

            // Create player entity and set initial position and sprite
            // Place X at half of world width and Y at top-most solid tile for that column
            int minX = _worldGrid.GetMinX();
            int maxX = _worldGrid.GetMaxX();
            int spawnXTile = (minX + maxX) / 2;
            int topY = _worldGrid.GetTopMostSolidTileY(spawnXTile);

            _playerEntity = _world.CreateEntity();
            // Position.Value represents the player's feet world coordinate (x,y)
            var playerPos = new FaithburnEngine.Components.Position { Value = new Vector2(spawnXTile * _worldGrid.TileSize + _worldGrid.TileSize / 2f, topY * _worldGrid.TileSize) };
            _playerEntity.Set(playerPos);

            // Give the player a Velocity component so InputSystem can control them
            _playerEntity.Set(new FaithburnEngine.Components.Velocity { Value = Vector2.Zero });

            // Load player texture using Content (assumes file at Content/Assets/Liliana.png)
            Texture2D playerTex = null;
            // 1) Try content pipeline (asset name without extension)
            try
            {
                playerTex = Content.Load<Texture2D>("Assets/Liliana");
            }
            catch
            {
                // ignore and try disk load
            }

            // 2) If Content.Load failed, try loading the PNG directly from disk (common in non-Content pipeline setups)
            if (playerTex == null)
            {
                string[] candidatePaths = new[] {
                    Path.Combine(AppContext.BaseDirectory, "Content", "Assets", "Liliana.png"),
                    Path.Combine(AppContext.BaseDirectory, "Content", "Liliana.png"),
                    Path.Combine(AppContext.BaseDirectory, "Content", "Assets", "sprites", "Liliana.png"),
                    Path.Combine(AppContext.BaseDirectory, "Content", "Assets", "Sprites", "Liliana.png")
                };

                foreach (var p in candidatePaths)
                {
                    try
                    {
                        if (File.Exists(p))
                        {
                            using var fs = File.OpenRead(p);
                            playerTex = Texture2D.FromStream(GraphicsDevice, fs);
                            break;
                        }
                    }
                    catch
                    {
                        // keep trying other paths
                    }
                }
            }

            // 3) Try AssetLoader cache as final non-destructive fallback
            if (playerTex == null)
            {
                playerTex = _asset_loader_get(_assetLoader, "Liliana.png");
            }

            // If we still don't have a player texture, create a simple placeholder so the player is visible.
            if (playerTex == null)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load Liliana.png from content or disk; using magenta placeholder.");
                int pw = 64;
                int ph = 96;
                playerTex = new Texture2D(GraphicsDevice, pw, ph);
                var pixels = new Color[pw * ph];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.Magenta;
                playerTex.SetData(pixels);
            }

            // Helper: safe AssetLoader lookup (handles nulls)
            static Texture2D _asset_loader_get(AssetLoader loader, string key)
            {
                try
                {
                    return loader?.GetTexture(key);
                }
                catch
                {
                    return null;
                }
            }

            var sprite = new FaithburnEngine.Components.Sprite
            {
                Texture = playerTex,
                // Use bottom-center origin so Position corresponds to feet on the ground
                Origin = new Vector2(playerTex.Width / 2f, playerTex.Height),
                Tint = Color.White,
                Scale = 1f
            };
            _playerEntity.Set(sprite);

            // Add a collider so AABB collision is used. Anchor at feet (bottom-center). Size chosen to fit typical character art.
            var playerCollider = new FaithburnEngine.Components.Collider
            {
                Size = new Microsoft.Xna.Framework.Vector2(32f, 64f),
                Offset = Microsoft.Xna.Framework.Vector2.Zero
            };
            _playerEntity.Set(playerCollider);

            // Center camera on player initially and use very small deadzone so player remains centered
            ref var ppos = ref _playerEntity.Get<FaithburnEngine.Components.Position>();
            _camera.Position = ppos.Value;
            _camera.DeadZoneSize = new Vector2(2f, 2f); // tiny deadzone to avoid micro-jitter but keep player centered

            _systems = new SequentialSystem<float>(
                // Ensure Input runs before Movement so buffered jumps are processed deterministically
                new InputSystem(_world, _worldGrid, speed: 360f),
                new MovementSystem(_world, _worldGrid),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera),
                new InventorySystem(_contentLoader, _world));   
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            var m = Mouse.GetState();

            // HOTBAR INPUT
            if (_player != null)
            {
                var inv = _player.Inventory;
                int total = Math.Min(HotbarDisplayCount, inv.Slots.Length);

                // Number keys 1..9,0 -> indices 0..9
                for (int i = 0; i < total; i++)
                {
                    Keys key = i == 9 ? Keys.D0 : (Keys)((int)Keys.D1 + i);
                    if (k.IsKeyDown(key))
                    {
                        _player.HotbarIndex = i;
                        break;
                    }
                }

                // Mouse wheel
                int scroll = m.ScrollWheelValue;
                if (scroll != _lastScrollValue)
                {
                    int delta = Math.Sign(scroll - _lastScrollValue);
                    _player.HotbarIndex = (_player.HotbarIndex - delta + total) % total;
                    _lastScrollValue = scroll;
                }

                // Mouse click selection (use same layout math as renderer)
                if (m.LeftButton == ButtonState.Pressed)
                {
                    int screenW = GraphicsDevice.Viewport.Width;
                    int y = GraphicsDevice.Viewport.Height - HotbarSlotSize - 10;
                    int width = total * HotbarSlotSize + (total - 1) * HotbarPadding;
                    int startX = (screenW - width) / 2;
                    var mx = m.X;
                    var my = m.Y;
                    if (my >= y && my <= y + HotbarSlotSize)
                    {
                        int rel = mx - startX;
                        if (rel >= 0)
                        {
                            int idx = rel / (HotbarSlotSize + HotbarPadding);
                            if (idx >= 0 && idx < total)
                            {
                                _player.HotbarIndex = idx;
                            }
                        }
                    }
                }
            }

             // optional: zoom with + / - keys
             if (k.IsKeyDown(Keys.OemPlus) || k.IsKeyDown(Keys.Add)) _camera.Zoom += 0.5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
             if (k.IsKeyDown(Keys.OemMinus) || k.IsKeyDown(Keys.Subtract)) _camera.Zoom -= 0.5f * (float)gameTime.ElapsedGameTime.TotalSeconds;

             float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

             // ALL gameplay logic flows through systems (Tenet #4 - ECS First).
             _systems.Update(dt);

            // After systems update, make the camera follow the player
            if (_playerEntity.IsAlive)
            {
                ref var pos = ref _playerEntity.Get<Components.Position>();
                ref var vel = ref _playerEntity.Get<Components.Velocity>();
                _camera.UpdateFollow(pos.Value, vel.Value, dt);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_spriteRenderer == null) throw new InvalidOperationException("_spriteRenderer is null");

            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteRenderer.Draw();

            // Draw hotbar UI on top
            if (_hotbarRenderer != null && _player != null)
            {
                _hotbarRenderer.Draw(_player.Inventory, Math.Clamp(_player.HotbarIndex, 0, HotbarDisplayCount - 1), HotbarSlotSize, HotbarDisplayCount, HotbarPadding);
            }

             base.Draw(gameTime);
         }

        protected override void UnloadContent()
        {
            // WHY explicit disposal (Tenet #3 - Efficient):
            // DefaultEcs and DefaultParallelRunner hold OS resources (threads, events).
            // Dispose them explicitly to ensure clean shutdown, no resource leaks.
            // Important for hot-reload and testing.
            _systems?.Dispose();
            _runner?.Dispose();
            base.UnloadContent();
        }
    }
}
