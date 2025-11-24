using DefaultEcs.System;
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
namespace FaithburnEngine.CoreGame;

public class Faithburn : Game
{
    private GraphicsDeviceManager _graphics; 
    private SpriteBatch _spriteBatch;
    private ContentLoader _contentLoader;
    private PlayerContext _player;
    private WorldGrid _worldGrid;
    private DefaultEcs.World _world;
    private InputSystem _inputSystem;
    private ISystem<float> _movementSystem;
    private SpriteRenderer _spriteRenderer;
    private InventorySystem _inventorySystem;
    private InteractionSystem _interactionSystem;
    private HotbarRenderer _hotbarRenderer;

    public Faithburn()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsFixedTimeStep = true; //We want fixed time step for deterministic combat and networking.
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); //60 FPS
    }

    protected override void Initialize()
    {
        _world = new DefaultEcs.World();

        _inputSystem = new InputSystem(_world, speed: 180f);
        _movementSystem = new MovementSystem(_world);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        

        _player = new PlayerContext(12);
        _worldGrid = new WorldGrid(_contentLoader);
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _contentLoader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "Content", "Models"));
        _contentLoader.LoadAll();
        _inventorySystem = new InventorySystem(_contentLoader);
        _interactionSystem = new InteractionSystem(_contentLoader, _inventorySystem, _worldGrid);
        _hotbarRenderer = new HotbarRenderer(_spriteBatch, _contentLoader, GraphicsDevice);

        if (_world == null) throw new InvalidOperationException("_world is null");
        else
        {
            //Create test entity
            var entity = _world.CreateEntity();
            var center = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
            entity.Set(new Position { Value = center });
            entity.Set(new Velocity { Value = Vector2.Zero });

            //Load player texture
            var playerPath = Path.Combine(AppContext.BaseDirectory, "Content", "Assets", "Liliana.png");
            Texture2D tex = null;
            if (File.Exists(playerPath))
            {
                using var stream = File.OpenRead(playerPath);
                tex = Texture2D.FromStream(GraphicsDevice, stream);
            }

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
    }

    protected override void Update(GameTime gameTime)
    {
        if(_inputSystem == null || _movementSystem == null) throw new InvalidOperationException("Systems not initialized");
        else
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _inputSystem.Update(dt);
            _movementSystem.Update(dt);

            base.Update(gameTime);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_spriteRenderer == null) throw new InvalidOperationException("_spriteRenderer is null");
        else
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteRenderer.Draw();
            base.Draw(gameTime);
        }
    }
}