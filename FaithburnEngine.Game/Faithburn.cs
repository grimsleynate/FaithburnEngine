using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using FaithburnEngine.Content;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;
using FaithburnEngine.Systems;
using FaithburnEngine.Systems.HeldAnimations;
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
        private ChunkedWorldGrid _worldGrid;
        private DefaultEcs.World _world;
        private SpriteRenderer _spriteRenderer;
        private InventorySystem _inventorySystem;
        private InteractionSystem _interactionSystem;
        private AssetLoader _assetLoader;
        private Camera2D _camera;
        private float desiredZoom = 1.0f;

        private SequentialSystem<float> _systems;
        private DefaultParallelRunner _runner;
        private Entity _playerEntity;
        private HotbarRenderer _hotbarRenderer;
        private int _lastScrollValue;
        private MouseState _prevMouseState;
        private ActiveHitboxSystem _hitboxSystem;
        private AssetRegistry _assets;
        private HeldAnimationRegistry _heldAnimRegistry;

        public Faithburn()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
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

            _worldGrid = new ChunkedWorldGrid(_contentLoader);
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            var searchRoots = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, "Mods"),
                Path.Combine(AppContext.BaseDirectory, "Content", "Assets")
            };
            _assets = new AssetRegistry(GraphicsDevice, searchRoots);
            RegisterDefaultAssets();

            _heldAnimRegistry = new HeldAnimationRegistry();
            _heldAnimRegistry.Register("swing", new FaithburnEngine.Systems.HeldAnimations.SwingAnimator());
            _heldAnimRegistry.Register("thrust", new FaithburnEngine.Systems.HeldAnimations.ThrustAnimator());

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
            _assets.Register("ui.slot_bg", Path.Combine("slot_bg.png"));
            _assets.Register("tiles.grass_dirt", Path.Combine("tiles", "grass_dirt_variants.png"));
            _assets.Register("player.liliana", Path.Combine("Liliana.png"));
            _assets.Register("item.proto_pickaxe", Path.Combine("items", "proto_pickaxe.png"));
        }

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
            var generator = new WorldGenerator(widthInTiles: 8400, heightInTiles: 2400, surfaceLevel: 1200);
            generator.FillWorld(_worldGrid);
        }

        private void CreatePlayerEntity()
        {
            int minX = _worldGrid.GetMinX();
            int maxX = _worldGrid.GetMaxX();
            int spawnXTile = (minX + maxX) / 2;
            
            // Find the topmost solid tile at spawn column and adjacent columns
            int topTileY = _worldGrid.GetTopMostSolidTileY(spawnXTile);
            int leftTileY = _worldGrid.GetTopMostSolidTileY(spawnXTile - 1);
            int rightTileY = _worldGrid.GetTopMostSolidTileY(spawnXTile + 1);
            int highestSurface = Math.Min(topTileY, Math.Min(leftTileY, rightTileY));
            
            // Player is 96 pixels tall (3 tiles). We need to check that the 3 tiles
            // ABOVE the spawn point are all clear. Find the highest surface among
            // all tiles the player's body will overlap.
            int playerHeightInTiles = 3; // 96 pixels / 32 pixels per tile
            for (int checkX = spawnXTile - 1; checkX <= spawnXTile + 1; checkX++)
            {
                for (int checkY = highestSurface - playerHeightInTiles; checkY < highestSurface; checkY++)
                {
                    if (_worldGrid.IsSolidTile(new Point(checkX, checkY)))
                    {
                        // Found a solid tile above - need to spawn even higher
                        highestSurface = Math.Min(highestSurface, checkY);
                    }
                }
            }
            
            // Spawn player with feet at the top of the highest surface tile
            float spawnX = spawnXTile * _worldGrid.TileSize + _worldGrid.TileSize / 2f;
            float spawnY = highestSurface * _worldGrid.TileSize - 1f;

            System.Diagnostics.Debug.WriteLine($"SPAWN DEBUG: Final spawn at ({spawnX}, {spawnY}), highestSurface={highestSurface}");

            _playerEntity = _world.CreateEntity();
            _playerEntity.Set(new FaithburnEngine.Components.Position { Value = new Vector2(spawnX, spawnY) });
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

            var playerCollider = new Components.Collider
            {
                Size = new Vector2(64f, 96f),
                Offset = Vector2.Zero
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
                new HeldItemSystem(_world, _contentLoader, GraphicsDevice, _hitboxSystem, _assets, _heldAnimRegistry, _camera, _player),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera, _player),
                new InventorySystem(_contentLoader, _world));
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            var m = Mouse.GetState();

            // DEBUG: Log velocity and position every 60 frames
            if (_playerEntity.IsAlive && gameTime.TotalGameTime.TotalSeconds < 2)
            {
                ref var pos = ref _playerEntity.Get<Components.Position>();
                ref var vel = ref _playerEntity.Get<Components.Velocity>();
                System.Diagnostics.Debug.WriteLine($"FRAME DEBUG: pos=({pos.Value.X:F1}, {pos.Value.Y:F1}), vel=({vel.Value.X:F1}, {vel.Value.Y:F1})");
            }

            if (_player != null)
            {
                var inv = _player.Inventory;
                int total = Math.Min(HotbarConstants.DisplayCount, inv.Slots.Length);

                for (int i = 0; i < total; i++)
                {
                    Keys key = i == 9 ? Keys.D0 : (Keys)((int)Keys.D1 + i);
                    if (k.IsKeyDown(key))
                    {
                        _player.HotbarIndex = i;
                        break;
                    }
                }

                int scroll = m.ScrollWheelValue;
                if (scroll != _lastScrollValue)
                {
                    int delta = Math.Sign(scroll - _lastScrollValue);
                    _player.HotbarIndex = (_player.HotbarIndex - delta + total) % total;
                    _lastScrollValue = scroll;
                }

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

            if (k.IsKeyDown(Keys.OemPlus) || k.IsKeyDown(Keys.Add)) _camera.Zoom += 1.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (k.IsKeyDown(Keys.OemMinus) || k.IsKeyDown(Keys.Subtract)) _camera.Zoom -= 1.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _systems.Update(dt);

            if (_playerEntity.IsAlive)
            {
                ref var pos = ref _playerEntity.Get<Components.Position>();
                ref var vel = ref _playerEntity.Get<Components.Velocity>();
                _worldGrid.UpdateLoadedChunks(pos.Value, loadRadius: 3);
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

            if (_hotbarRenderer != null && _player != null)
            {
                _hotbarRenderer.Draw(_player.Inventory, Math.Clamp(_player.HotbarIndex, 0, HotbarConstants.DisplayCount - 1), HotbarConstants.SlotSize, HotbarConstants.DisplayCount, HotbarConstants.Padding);
            }

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _systems?.Dispose();
            _runner?.Dispose();
            base.UnloadContent();
        }

        private void SetUpDisplay()
        {
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = displayMode.Width;
            _graphics.PreferredBackBufferHeight = displayMode.Height;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
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
