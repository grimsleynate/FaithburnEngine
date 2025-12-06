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
        private MouseState _prevMouse;
        private readonly GraphicsDevice _graphics; // optional for UI interactions

        private const float JumpVelocity = 960f;
        private const float CoyoteTime = 0.12f; // seconds
        private const float JumpBufferTime = 0.12f;
        private const int DefaultHotbarCount = 10;

        public InputSystem(DefaultEcs.World world, WorldGrid? worldGrid = null, float speed = 540f, GraphicsDevice? graphics = null)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {
            _speed = speed;
            _worldGrid = worldGrid;
            _prevKb = Keyboard.GetState();
            _prevMouse = Mouse.GetState();
            _graphics = graphics!;
        }

        protected override void Update(float dt, in Entity entity)
        {
            var kb = Keyboard.GetState();
            var mouse = Mouse.GetState();
            Vector2 dir = Vector2.Zero;

            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1f;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1f;

            if (dir != Vector2.Zero) dir.Normalize();

            ref var vel = ref entity.Get<Velocity>();
            ref var pos = ref entity.Get<Position>();

            float targetSpeed = dir.X * _speed;
            if (!entity.Has<MoveIntent>()) entity.Set(new MoveIntent { TargetSpeedX = targetSpeed });
            else { ref var mi = ref entity.Get<MoveIntent>(); mi.TargetSpeedX = targetSpeed; }

            // Hotbar selection: number keys 1..9,0
            if (entity.Has<PlayerTag>())
            {
                if (!entity.Has<HotbarSelection>()) entity.Set(new HotbarSelection { Index = 0 });
                ref var hotbar = ref entity.Get<HotbarSelection>();
                int total = DefaultHotbarCount;
                for (int i = 0; i < total; i++)
                {
                    Keys key = i == 9 ? Keys.D0 : (Keys)((int)Keys.D1 + i);
                    if (kb.IsKeyDown(key)) { hotbar.Index = i; break; }
                }

                int scroll = mouse.ScrollWheelValue;
                int last = _prevMouse.ScrollWheelValue;
                if (scroll != last)
                {
                    int delta = Math.Sign(scroll - last);
                    hotbar.Index = (hotbar.Index - delta + total) % total;
                }
            }

            // Update InputState for other systems
            if (!entity.Has<InputState>()) entity.Set(new InputState { JumpHeld = kb.IsKeyDown(Keys.Space) });
            else { ref var isState = ref entity.Get<InputState>(); isState.JumpHeld = kb.IsKeyDown(Keys.Space); }

            // Flip sprite horizontally based on movement direction
            if (entity.Has<Sprite>())
            {
                ref var sprite = ref entity.Get<Sprite>();
                if (dir.X > 0f) sprite.Effects = SpriteEffects.FlipHorizontally;
                else if (dir.X < 0f) sprite.Effects = SpriteEffects.None;
            }

            // GroundedState init once
            if (!entity.Has<GroundedState>()) entity.Set(new GroundedState { TimeSinceGrounded = float.MaxValue });

            // Determine grounded
            float timeSinceGrounded = entity.Get<GroundedState>().TimeSinceGrounded;
            if (_worldGrid != null && _worldGrid.IsGrounded(pos.Value, epsilon: 3f)) timeSinceGrounded = 0f;

            // Jump buffer
            bool jumpPressed = kb.IsKeyDown(Keys.Space) && !_prevKb.IsKeyDown(Keys.Space);
            if (jumpPressed)
            {
                if (!entity.Has<JumpIntent>()) entity.Set(new JumpIntent { TimeLeft = JumpBufferTime });
                else { ref var intent = ref entity.Get<JumpIntent>(); intent.TimeLeft = JumpBufferTime; }
            }

            if (entity.Has<JumpIntent>())
            {
                ref var intent = ref entity.Get<JumpIntent>();
                intent.TimeLeft -= dt;
                if (intent.TimeLeft > 0f && timeSinceGrounded <= CoyoteTime)
                {
                    if (vel.Value.Y >= -0.1f) vel.Value = new Vector2(vel.Value.X, -JumpVelocity);
                    entity.Remove<JumpIntent>();
                }
                if (intent.TimeLeft <= 0f) entity.Remove<JumpIntent>();
            }

            _prevKb = kb;
            _prevMouse = mouse;
        }
    }

    // Marker for player entity; a simple struct component
    public struct PlayerTag { }
    public struct HotbarSelection { public int Index; }
}
