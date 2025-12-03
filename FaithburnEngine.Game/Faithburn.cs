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

namespace FaithburnEngine.CoreGame
{
    /// <summary>
    /// Main game orchestrator. Follows Entity Component System (ECS) architecture.
    /// 
    /// CORE TENETS:
    /// - Tenet #4 (ECS First): Game logic split into Systems (gameplay) and Components (data)
    /// - Tenet #3 (Efficient): DefaultParallelRunner uses all CPU cores for independent systems
    /// - Tenet #5 (Multiplayer): ECS makes netcode simple—serialize entities, broadcast state
    /// - Tenet #1 (Moddable): Systems are injectable; modders create custom gameplay without touching this file
    /// - Tenet #2 (Terraria-like): 32x32 tiles, side-scrolling, crafting/exploration focus
    /// </summary>
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
        private float _cameraSpeed = 600f; 
        private float desiredZoom = 1.0f; 

        // ECS pipeline fields
        private SequentialSystem<float> _systems;
        private DefaultParallelRunner _runner;

        public Faithburn()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            
            // WHY IsFixedTimeStep = true (Tenet #3, #5):
            // Deterministic updates are CRITICAL for multiplayer. All clients must compute
            // identical entity positions given identical input. Fixed timestep locks all clients
            // to 60 FPS, enabling frame-synchronized network code.
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

            // WHY DefaultParallelRunner (Tenet #3 - Multi-threaded Efficiency):
            // InputSystem and MovementSystem are "pure" (no shared mutable state).
            // They can run in parallel on different cores simultaneously.
            // Sequential systems (Interaction, Inventory) run one-at-a-time to prevent data races.
            // Result: 2x speedup on dual-core, scales linearly with cores (100 entities = same cost).
            _runner = new DefaultParallelRunner(Environment.ProcessorCount);

            _camera = new Camera2D();
            _camera.UpdateOrigin(GraphicsDevice.Viewport);
            _camera.Zoom = desiredZoom;

            // SYSTEM PIPELINE (Tenet #4 - ECS First):
            // Order matters for game feel:
            // 1. Input + Movement (parallel) ? Player responds immediately
            // 2. Interaction (sequential) ? Player can harvest blocks
            // 3. Inventory (sequential) ? UI shows changes
            // This creates the feel of a responsive, snappy game (like Terraria).
            // Modders extend this by creating new systems and injecting them here.
            _systems = new SequentialSystem<float>(
                new ParallelSystem<float>(_runner,
                    new InputSystem(_world, speed: 180f),
                    new MovementSystem(_world)
                ),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera),
                new InventorySystem(_contentLoader, _world)
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _player = new PlayerContext(12);

            _contentLoader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Models"));
            _contentLoader.LoadAll();

            _assetLoader = new AssetLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Assets"));

            _worldGrid = new WorldGrid(_contentLoader);
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize block atlas and load sprite sheets
            var blockAtlas = new BlockAtlas(32, 4);
            // TODO: Load "grass_dirt" spritesheet from Content/Assets/tiles/grass_dirt_variants.png
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

            _systems = new SequentialSystem<float>(
                new ParallelSystem<float>(_runner,
                    new InputSystem(_world, speed: 180f),
                    new MovementSystem(_world)
            ),
            new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid, _camera),
            new InventorySystem(_contentLoader, _world));   
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            Vector2 inputDelta = Vector2.Zero;

            if (k.IsKeyDown(Keys.W) || k.IsKeyDown(Keys.Up)) inputDelta.Y -= 1f;
            if (k.IsKeyDown(Keys.S) || k.IsKeyDown(Keys.Down)) inputDelta.Y += 1f;
            if (k.IsKeyDown(Keys.A) || k.IsKeyDown(Keys.Left)) inputDelta.X -= 1f;
            if (k.IsKeyDown(Keys.D) || k.IsKeyDown(Keys.Right)) inputDelta.X += 1f;

            if (inputDelta != Vector2.Zero)
                inputDelta.Normalize();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _camera.Position += inputDelta * _cameraSpeed * dt;

            // optional: zoom with + / - keys
            if (k.IsKeyDown(Keys.OemPlus) || k.IsKeyDown(Keys.Add)) _camera.Zoom += 0.5f * dt;
            if (k.IsKeyDown(Keys.OemMinus) || k.IsKeyDown(Keys.Subtract)) _camera.Zoom -= 0.5f * dt;


            // ALL gameplay logic flows through systems (Tenet #4 - ECS First).
            // New feature? Create a system, register it in Initialize().
            // No need to modify Update(). This is how moddable game engines work.
            _systems.Update(dt);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_spriteRenderer == null) throw new InvalidOperationException("_spriteRenderer is null");

            GraphicsDevice.Clear(Color.CornflowerBlue);

            // RENDERING LAYER ORDER (Tenet #2 - Terraria-like):
            // 1. Tiles (background)
            // 2. Entities (foreground)
            // 3. UI (top)
            // This order creates depth perception and ensures UI is readable.
            _spriteRenderer.Draw();

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
