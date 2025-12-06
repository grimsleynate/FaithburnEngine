using DefaultEcs;
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
using FaithburnEngine.Content.Models;
using System.Collections.Generic;

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

        // Previous mouse state for edge detection
        private MouseState _prevMouseState;

        // Cached reference to ActiveHitboxSystem
        private ActiveHitboxSystem _hitboxSystem;

        // Asset registry
        private AssetRegistry _assets;

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
            _world = new DefaultEcs.World();
            SetUpDisplay();
            SetUpCamera();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _player = new PlayerContext(10);

            _contentLoader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Models"));
            _contentLoader.LoadAll();

            _assetLoader = new AssetLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Assets"));

            _worldGrid = new WorldGrid(_contentLoader);
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Init mod-aware asset registry: Mods first, then base Assets
            var searchRoots = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, "Mods"),
                Path.Combine(AppContext.BaseDirectory, "Content", "Assets")
            };
            _assets = new AssetRegistry(GraphicsDevice, searchRoots);
            RegisterDefaultAssets();

            InitHotbarUi();
            var blockAtlas = InitBlockAtlas();
            GenerateWorld();
            _inventorySystem = new InventorySystem(_contentLoader, _world);
            _interactionSystem = new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera, _player);
            _hitboxSystem = new ActiveHitboxSystem(_world, _worldGrid, _contentLoader);
            _spriteRenderer = new SpriteRenderer(_world, _spriteBatch, blockAtlas, _worldGrid, _camera);

            CreatePlayerEntity();
            CenterCameraOnPlayer();
            BuildSystemPipeline();
        }

        private void RegisterDefaultAssets()
        {
            // Map logical keys to relative paths under asset roots
            _assets.Register("ui.slot_bg", Path.Combine("slot_bg.png"));
            _assets.Register("tiles.grass_dirt", Path.Combine("tiles", "grass_dirt_variants.png"));
            _assets.Register("player.liliana", Path.Combine("Liliana.png"));
            _assets.Register("item.proto_pickaxe", Path.Combine("items", "proto_pickaxe.png"));
        }

        // --- helpers ---
        private void InitHotbarUi()
        {
            Texture2D slotBg = null;
            if (_assets.TryGetTexture("ui.slot_bg", out var tex)) slotBg = tex;
            SpriteFont uiFont = null;
            _hotbarRenderer = new HotbarRenderer(_spriteBatch, _contentLoader, slotBg, uiFont, GraphicsDevice, _assets);
            _lastScrollValue = Mouse.GetState().ScrollWheelValue;
            _prevMouseState = Mouse.GetState();
        }

        private BlockAtlas InitBlockAtlas()
        {
            var blockAtlas = new BlockAtlas(32, 4);
            if (_assets.TryGetTexture("tiles.grass_dirt", out var sheet))
            {
                blockAtlas.RegisterAtlas("grass_dirt", sheet, 16);
            }
            return blockAtlas;
        }

        private void GenerateWorld()
        {
            var generator = new WorldGenerator(widthInTiles: 1000, heightInTiles: 500, surfaceLevel: 50);
            generator.FillWorld(_worldGrid);
        }

        private void CreatePlayerEntity()
        {
            int minX = _worldGrid.GetMinX();
            int maxX = _worldGrid.GetMaxX();
            int spawnXTile = (minX + maxX) / 2;
            int topY = _worldGrid.GetTopMostSolidTileY(spawnXTile);

            _playerEntity = _world.CreateEntity();
            var playerPos = new FaithburnEngine.Components.Position { Value = new Vector2(spawnXTile * _worldGrid.TileSize + _worldGrid.TileSize / 2f, topY * _worldGrid.TileSize) };
            _playerEntity.Set(playerPos);
            _playerEntity.Set(new FaithburnEngine.Components.Velocity { Value = Vector2.Zero });

            GiveStarterItem("proto_pickaxe");

            var playerTex = LoadPlayerTexture();
            if (playerTex == null)
            {
                int pw = 64, ph = 96;
                playerTex = new Texture2D(GraphicsDevice, pw, ph);
                var pixels = new Color[pw * ph];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.Magenta;
                playerTex.SetData(pixels);
            }

            var sprite = new FaithburnEngine.Components.Sprite
            {
                Texture = playerTex,
                Origin = new Vector2(playerTex.Width / 2f, playerTex.Height),
                Tint = Color.White,
                Scale = 1f
            };
            _playerEntity.Set(sprite);

            var playerCollider = new FaithburnEngine.Components.Collider
            {
                Size = new Microsoft.Xna.Framework.Vector2(32f, 64f),
                Offset = Microsoft.Xna.Framework.Vector2.Zero
            };
            _playerEntity.Set(playerCollider);
        }

        private Texture2D LoadPlayerTexture()
        {
            if (_assets.TryGetTexture("player.liliana", out var tex)) return tex;
            return null;
        }

        private void GiveStarterItem(string itemId)
        {
            var proto = _contentLoader.GetItem(itemId);
            if (proto == null) return;
            for (int i = 0; i < _player.Inventory.Slots.Length; i++)
            {
                var s = _player.Inventory.Slots[i];
                if (s.IsEmpty)
                {
                    s.Set(proto.Id, 1);
                    break;
                }
            }
        }

        private void CenterCameraOnPlayer()
        {
            ref var ppos = ref _playerEntity.Get<FaithburnEngine.Components.Position>();
            _camera.Position = ppos.Value;
            _camera.DeadZoneSize = new Vector2(2f, 2f);
        }

        private void BuildSystemPipeline()
        {
            _systems = new SequentialSystem<float>(
                new InputSystem(_world, _worldGrid, speed: 360f, GraphicsDevice),
                new MovementSystem(_world, _worldGrid),
                _hitboxSystem,
                new HeldItemSystem(_world, _contentLoader, GraphicsDevice, _hitboxSystem, _assets),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera, _player),
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
                int total = Math.Min(HotbarConstants.DisplayCount, inv.Slots.Length);

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
                    int y = GraphicsDevice.Viewport.Height - HotbarConstants.SlotSize - HotbarConstants.BottomOffset;
                    int width = total * HotbarConstants.SlotSize + (total - 1) * HotbarConstants.Padding;
                    int startX = (screenW - width) / 2;
                    var mx = m.X;
                    var my = m.Y;
                    if (my >= y && my <= y + HotbarConstants.SlotSize)
                    {
                        int rel = mx - startX;
                        if (rel >= 0)
                        {
                            int idx = rel / (HotbarConstants.SlotSize + HotbarConstants.Padding);
                            if (idx >= 0 && idx < total)
                            {
                                _player.HotbarIndex = idx;
                            }
                        }
                    }
                }
            }

            // optional: zoom with + / - keys (speed increased ~2x)
            if (k.IsKeyDown(Keys.OemPlus) || k.IsKeyDown(Keys.Add)) _camera.Zoom += 1.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (k.IsKeyDown(Keys.OemMinus) || k.IsKeyDown(Keys.Subtract)) _camera.Zoom -= 1.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            _prevMouseState = m;

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
                _hotbarRenderer.Draw(_player.Inventory, Math.Clamp(_player.HotbarIndex, 0, HotbarConstants.DisplayCount - 1), HotbarConstants.SlotSize, HotbarConstants.DisplayCount, HotbarConstants.Padding);
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

        private void SetUpDisplay()
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

            _runner = new DefaultParallelRunner(Environment.ProcessorCount);
        }

        private void SetUpCamera()
        {
            _camera = new Camera2D();
            _camera.UpdateOrigin(GraphicsDevice.Viewport);
            _camera.Zoom = desiredZoom;
        }
    }
}
