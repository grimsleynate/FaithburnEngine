using DefaultEcs;
using DefaultEcs.System;
using FaithburnEngine.Components;
using Microsoft.Xna.Framework;
using FaithburnEngine.World;
using FaithburnEngine.Core;
using System;

namespace FaithburnEngine.Systems
{
    /// <summary>
    /// Kinematic character controller implementing axis-separated movement, velocity projection
    /// for sliding, coyote time, jump buffering, and variable jump height.
    /// - Axis separated: horizontal then vertical resolution to avoid corner snagging.
    /// - Velocity projection: remove component into contact normal on collision.
    /// </summary>
    public sealed class MovementSystem : AEntitySetSystem<float>
    {
        private readonly WorldGrid? _worldGrid;

        public MovementSystem(DefaultEcs.World world, WorldGrid? worldGrid = null)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {
            _worldGrid = worldGrid;
        }

        protected override void Update(float dt, ReadOnlySpan<Entity> entities)
        {
            foreach (ref readonly var entity in entities)
            {
                ref var pos = ref entity.Get<Position>(); // feet position
                ref var vel = ref entity.Get<Velocity>();

                // Read intent
                float targetSpeedX = 0f;
                if (entity.Has<MoveIntent>())
                {
                    ref var mi = ref entity.Get<MoveIntent>();
                    targetSpeedX = MathHelper.Clamp(mi.TargetSpeedX, -Constants.Player.MaxSpeed, Constants.Player.MaxSpeed);
                }

                bool jumpHeld = false;
                if (entity.Has<InputState>()) jumpHeld = entity.Get<InputState>().JumpHeld;

                // Coyote & jump buffer stored per-entity via components (create if missing)
                if (!entity.Has<GroundedState>()) entity.Set(new GroundedState { TimeSinceGrounded = float.MaxValue });
                ref var gsRef = ref entity.Get<GroundedState>();

                if (!entity.Has<JumpIntent>()) { /* leave absent until input sets it */ }

                // Update timers
                gsRef.TimeSinceGrounded += dt;
                float timeSinceGrounded = gsRef.TimeSinceGrounded;

                // --- Horizontal movement (axis-separated) ---
                float vx = vel.Value.X;

                // Determine accel/decel based on grounded state
                bool grounded = _worldGrid != null ? _worldGrid.IsGrounded(pos.Value, epsilon: 2f) : (timeSinceGrounded <= Constants.Player.CoyoteTime);

                float accel = grounded ? Constants.Player.AccelGround : Constants.Player.AccelAir;
                float decel = grounded ? Constants.Player.DecelGround : Constants.Player.DecelAir;

                float desiredVX = targetSpeedX;
                float diff = desiredVX - vx;
                if (Math.Abs(diff) > 0.01f)
                {
                    float change = (Math.Abs(desiredVX) > Math.Abs(vx)) ? accel * dt : decel * dt;
                    if (Math.Abs(change) > Math.Abs(diff)) change = Math.Abs(diff);
                    vx += Math.Sign(diff) * change;
                }

                // Soft cap
                vx = MathHelper.Clamp(vx, -Constants.Player.MaxSpeed, Constants.Player.MaxSpeed);

                // Apply horizontal movement with collision
                Vector2 horizProposed = new Vector2(pos.Value.X + vx * dt, pos.Value.Y);
                vx = HandleHorizontalCollision(entity, pos.Value, horizProposed, vx, out float resolvedX);

                // write back intermediate X change
                pos.Value = new Vector2(resolvedX, pos.Value.Y);

                // --- Vertical movement ---
                float vy = vel.Value.Y;

                // Gravity and variable jump height
                float g = Constants.Player.Gravity;
                if (vy > 0f) g *= Constants.Player.FallMultiplier;
                else if (vy < 0f && !jumpHeld) g *= Constants.Player.LowJumpMultiplier;

                vy = MathHelper.Clamp(vy + g * dt, -float.MaxValue, Constants.Player.MaxFallSpeed);

                // Jump buffering handling
                if (entity.Has<JumpIntent>())
                {
                    ref var ji = ref entity.Get<JumpIntent>();
                    ji.TimeLeft -= dt;
                    if (ji.TimeLeft > 0f)
                    {
                        // if we are allowed to jump (grounded or within coyote time)
                        if (timeSinceGrounded <= Constants.Player.CoyoteTime)
                        {
                            vy = -Constants.Player.JumpVelocity;
                            entity.Remove<JumpIntent>();
                            gsRef.TimeSinceGrounded = float.MaxValue; // left ground
                        }
                    }
                    else
                    {
                        entity.Remove<JumpIntent>();
                    }
                }

                // Integrate vertical with collision
                Vector2 vertProposed = new Vector2(pos.Value.X, pos.Value.Y + vy * dt);
                vy = HandleVerticalCollision(entity, pos.Value, vertProposed, vy, out float resolvedY, out bool landed);

                pos.Value = new Vector2(pos.Value.X, resolvedY);

                // Update velocity
                vel.Value = new Vector2(vx, vy);

                // Update grounded state
                if (landed)
                {
                    gsRef.TimeSinceGrounded = 0f;
                }
                else
                {
                    gsRef.TimeSinceGrounded += dt; // keep increasing
                }
            }
        }

        private float HandleHorizontalCollision(Entity entity, Vector2 fromPos, Vector2 proposedPos, float vx, out float resolvedX)
        {
            resolvedX = proposedPos.X;
            if (_worldGrid == null || !entity.Has<Collider>()) return vx;

            var col = entity.Get<Collider>();
            var offset = col.Offset;
            float halfW = col.Size.X * 0.5f;
            float h = col.Size.Y;
            int tileSize = _worldGrid.TileSize;

            // Compute swept AABB horizontally at proposed X
            float bottom = proposedPos.Y + offset.Y;
            float top = bottom - h;
            float left = proposedPos.X - halfW + offset.X;
            float right = proposedPos.X + halfW + offset.X;

            int lx = (int)Math.Floor(left / tileSize);
            int rx = (int)Math.Floor((right - 0.001f) / tileSize);
            int ty = (int)Math.Floor(top / tileSize);
            int by = (int)Math.Floor((bottom - 0.001f) / tileSize);

            // For each tile we overlap, if solid, compute contact normal and project velocity
            for (int tx = lx; tx <= rx; tx++)
            {
                for (int tyi = ty; tyi <= by; tyi++)
                {
                    if (!_worldGrid.IsSolidTile(new Point(tx, tyi))) continue;

                    // Compute tile AABB
                    float tileLeft = tx * tileSize;
                    float tileRight = tileLeft + tileSize;
                    float tileTop = tyi * tileSize;
                    float tileBottom = tileTop + tileSize;

                    // Determine penetration on X axis
                    var penLeft = right - tileLeft; // positive if overlapping from left
                    var penRight = tileRight - left; // positive if overlapping from right

                    if (penLeft > 0f && penRight > 0f)
                    {
                        // choose minimum penetration
                        if (penLeft < penRight)
                        {
                            // push left
                            resolvedX = tileLeft - halfW - offset.X - Constants.Player.CollisionSkin;
                            vx = 0f;
                        }
                        else
                        {
                            // push right
                            resolvedX = tileRight + halfW - offset.X + Constants.Player.CollisionSkin;
                            vx = 0f;
                        }
                        return vx;
                    }
                }
            }

            return vx;
        }

        private float HandleVerticalCollision(Entity entity, Vector2 fromPos, Vector2 proposedPos, float vy, out float resolvedY, out bool landed)
        {
            resolvedY = proposedPos.Y;
            landed = false;
            if (_worldGrid == null || !entity.Has<Collider>()) return vy;

            var col = entity.Get<Collider>();
            var offset = col.Offset;
            float halfW = col.Size.X * 0.5f;
            float h = col.Size.Y;
            int tileSize = _worldGrid.TileSize;

            float bottom = proposedPos.Y + offset.Y;
            float top = bottom - h;
            float left = proposedPos.X - halfW + offset.X;
            float right = proposedPos.X + halfW + offset.X;

            int lx = (int)Math.Floor(left / tileSize);
            int rx = (int)Math.Floor((right - 0.001f) / tileSize);
            int ty = (int)Math.Floor(top / tileSize);
            int by = (int)Math.Floor((bottom - 0.001f) / tileSize);

            for (int tx = lx; tx <= rx; tx++)
            {
                for (int tyi = ty; tyi <= by; tyi++)
                {
                    if (!_worldGrid.IsSolidTile(new Point(tx, tyi))) continue;

                    float tileTop = tyi * tileSize;
                    float tileBottom = tileTop + tileSize;

                    // check vertical penetration
                    float penDown = bottom - tileTop; // positive when overlapping downward
                    float penUp = tileBottom - top; // positive when overlapping upward

                    if (penDown > 0f && penUp > 0f)
                    {
                        if (penDown < penUp)
                        {
                            // land on tile
                            resolvedY = tileTop - offset.Y;
                            vy = 0f;
                            landed = true;
                            return vy;
                        }
                        else
                        {
                            // hit head
                            resolvedY = tileBottom + h - offset.Y;
                            vy = 0f;
                            return vy;
                        }
                    }
                }
            }

            return vy;
        }
    }
}
