using DefaultEcs.System;
using FaithburnEngine.Components;
using FaithburnEngine.Rendering;
using FaithburnEngine.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;
namespace FaithburnEngine.CoreGame;

public class Faithburn : Game
{
    private GraphicsDeviceManager _graphics; private SpriteBatch _spriteBatch;
    private DefaultEcs.World _world;
    private InputSystem _inputSystem;
    private ISystem<float> _movementSystem;
    private SpriteRenderer _spriteRenderer;

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
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        //Create test entity
        var entity = _world.CreateEntity();
        var center = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
        entity.Set(new Position { Value = center });
        entity.Set(new Velocity { Value = Vector2.Zero });

        //Load player texture
        var playerPath = Path.Combine(System.AppContext.BaseDirectory, "Content", "player.png");
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

    protected override void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _inputSystem.Update(dt);
        _movementSystem.Update(dt);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteRenderer.Draw();
        base.Draw(gameTime);
    }
}