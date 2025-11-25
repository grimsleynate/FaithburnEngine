using DefaultEcs.System;
using DefaultEcs.Threading;
using FaithburnEngine.Components;
using FaithburnEngine.Content;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;
using FaithburnEngine.Systems;
using FaithburnEngine.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;

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
        private HotbarRenderer _hotbarRenderer;
        private AssetLoader _assetLoader;

        // ECS pipeline fields
        private SequentialSystem<float> _systems;
        private DefaultParallelRunner _runner;

        public Faithburn()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsFixedTimeStep = true; // deterministic combat/networking
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60 FPS
        }

        protected override void Initialize()
        {
            _world = new DefaultEcs.World();

            // runner sized to CPU cores
            _runner = new DefaultParallelRunner(Environment.ProcessorCount);

            // build pipeline: parallel block for pure systems, sequential for shared mutation
            _systems = new SequentialSystem<float>(
                new ParallelSystem<float>(_runner,
                    new InputSystem(_world, speed: 180f),
                    new MovementSystem(_world)
                ),
                new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid),
                new InventorySystem(_contentLoader, _world)
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _player = new PlayerContext(12);
            _contentLoader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Models"));
            _contentLoader.LoadAll();

            _worldGrid = new WorldGrid(_contentLoader);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _inventorySystem = new InventorySystem(_contentLoader, _world);
            _interactionSystem = new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid);
            _hotbarRenderer = new HotbarRenderer(_spriteBatch, _contentLoader, GraphicsDevice);
            _assetLoader = new AssetLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Assets"));

            if (_world == null) throw new InvalidOperationException("_world is null");

            // Create test entity
            var entity = _world.CreateEntity();
            var center = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
            entity.Set(new Position { Value = center });
            entity.Set(new Velocity { Value = Vector2.Zero });

            _assetLoader.LoadTexture(GraphicsDevice, "Liliana.png");
            var tex = _assetLoader.GetTexture("Liliana.png");

            var sprite = new Sprite
            {
                Texture = tex,
                Origin = tex != null ? new Vector2(tex.Width / 2f, tex.Height / 2f) : Vector2.Zero,
                Tint = Color.White,
                Scale = 1f
            };
            entity.Set(sprite);

            _spriteRenderer = new SpriteRenderer(_world, _spriteBatch);

            if (sprite.Texture == null) Debug.WriteLine("Sprite texture is null");
            else Debug.WriteLine($"Texture size: {sprite.Texture.Width}x{sprite.Texture.Height}");
            Debug.WriteLine($"Pos: {entity.Get<Position>().Value.X}, {entity.Get<Position>().Value.Y}");
        }

        protected override void Update(GameTime gameTime)
        {
            _assetLoader.Publish(GraphicsDevice);

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _systems.Update(dt);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_spriteRenderer == null) throw new InvalidOperationException("_spriteRenderer is null");

            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteRenderer.Draw();
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _systems?.Dispose();
            _runner?.Dispose();
            base.UnloadContent();
        }
    }
}
