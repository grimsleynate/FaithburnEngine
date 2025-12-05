using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Components;

namespace FaithburnEngine.Systems
{
    public sealed class InputSystem : AEntitySetSystem<float>
    {
        private readonly float _speed;

        public InputSystem(DefaultEcs.World world, float speed = 360f)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {    
            _speed = speed; 
        }

        protected override void Update(float dt, in Entity entity)
        {
            var kb = Keyboard.GetState();
            Vector2 dir = Vector2.Zero;

            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1f;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1f;

            if (dir != Vector2.Zero) dir.Normalize();

            ref var vel = ref entity.Get<Velocity>();
            vel.Value = new Vector2(dir.X * _speed, vel.Value.Y);

            // Flip sprite horizontally based on movement direction
            if (entity.Has<Sprite>())
            {
                ref var sprite = ref entity.Get<Sprite>();
                if (dir.X > 0f)
                {
                    // sprite art faces left by default; flip when moving right
                    sprite.Effects = SpriteEffects.FlipHorizontally;
                }
                else if (dir.X < 0f)
                {
                    sprite.Effects = SpriteEffects.None;
                }
                // if dir.X == 0, keep previous orientation
            }
        }
    }
}
