using DefaultEcs.System;
using FaithburnEngine.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace FaithburnEngine.CoreGame;

public class Faithburn : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private DefaultEcs.World _world;
    private ISystem<float> _movementSystem;

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
        _movementSystem = new Systems.MovementSystem(_world);
        base.Initialize();

        var e = _world.CreateEntity();
        e.Set(new Position { Value = new Vector2(10, 10) });
        e.Set(new Velocity { Value = new Vector2(50, 0) });
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        //TODO: Initialize atlas manager, camera, tileset, etc.
    }

    protected override void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _movementSystem.Update(dt);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        //TODO: draw tiles/entities via rendering module
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
