using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Components;
using FaithburnEngine.World;

namespace FaithburnEngine.Systems
{
    public sealed class InputSystem : AEntitySetSystem<float>
    {
        private readonly float _speed;
        private readonly WorldGrid? _worldGrid;
        private KeyboardState _prevKb;

        // Jump settings
        private const float JumpVelocity = 960f;

        // Coyote time allows jumping shortly after leaving ledge
        private const float CoyoteTime = 0.12f; // seconds

        // Jump buffer allows queued jump input to trigger when ground contact occurs
        private const float JumpBufferTime = 0.12f;

        public InputSystem(DefaultEcs.World world, WorldGrid? worldGrid = null, float speed = 360f)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {
            _speed = speed;
            _worldGrid = worldGrid;
            _prevKb = Keyboard.GetState();
        }

        protected override void Update(float dt, in Entity entity)
        {
            var kb = Keyboard.GetState();
            Vector2 dir = Vector2.Zero;

            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1f;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1f;

            if (dir != Vector2.Zero) dir.Normalize();

            ref var vel = ref entity.Get<Velocity>();
            ref var pos = ref entity.Get<Position>();

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

            // Determine time since last grounded using GroundedState if available
            float timeSinceGrounded = float.MaxValue;
            if (entity.Has<GroundedState>())
            {
                ref var gs = ref entity.Get<GroundedState>();
                timeSinceGrounded = gs.TimeSinceGrounded;
            }
            else if (_worldGrid != null)
            {
                // Fallback to probing the world a little below feet
                bool groundedProbe = _worldGrid.IsGrounded(pos.Value, epsilon: 3f);
                timeSinceGrounded = groundedProbe ? 0f : float.MaxValue;
            }

            // Detect jump press edge and create/refresh JumpIntent component
            bool jumpPressed = kb.IsKeyDown(Keys.Space) && !_prevKb.IsKeyDown(Keys.Space);
            if (jumpPressed)
            {
                if (!entity.Has<JumpIntent>())
                {
                    entity.Set(new JumpIntent { TimeLeft = JumpBufferTime });
                }
                else
                {
                    ref var intent = ref entity.Get<JumpIntent>();
                    intent.TimeLeft = JumpBufferTime;
                }
            }

            // Update jump intent timers and attempt to execute if conditions met
            if (entity.Has<JumpIntent>())
            {
                ref var intent = ref entity.Get<JumpIntent>();
                intent.TimeLeft -= dt;

                // If ground contact within coyote window and intent buffered, perform jump
                if (intent.TimeLeft > 0f && timeSinceGrounded <= CoyoteTime)
                {
                    // Prevent double jump due to multiple frames: only jump if vertical velocity is near zero or downwards
                    if (vel.Value.Y >= -0.1f)
                    {
                        vel.Value = new Vector2(vel.Value.X, -JumpVelocity);
                    }

                    // Remove jump intent after successful jump
                    entity.Remove<JumpIntent>();
                }

                // Expire stale intent
                if (intent.TimeLeft <= 0f)
                {
                    entity.Remove<JumpIntent>();
                }
            }

            _prevKb = kb;
        }
    }
}
