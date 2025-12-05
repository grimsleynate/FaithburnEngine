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
    /// Updates Position based on Velocity. Applies gravity and AABB collision against world tiles when Collider is present.
    /// Uses swept checks to avoid tunneling at high speed. Adds horizontal inertia and improved gravity for platform feel.
    /// </summary>
    public sealed class MovementSystem : AEntitySetSystem<float>
    {
        private readonly WorldGrid? _worldGrid;
        private readonly float _maxSpeed;

        // Physics tuning
        private const float BaseGravity = 2200f; // pixels/s^2
        private const float FallMultiplier = 2.2f; // stronger gravity when falling
        private const float LowJumpMultiplier = 2.0f; // stronger gravity when releasing jump early
        private const float MaxFallSpeed = 1600f; // terminal velocity

        // Horizontal inertia tuning
        private const float AccelGround = 6000f; // pixels/s^2
        private const float AccelAir = 2800f;
        private const float DecelGround = 8000f; // braking
        private const float DecelAir = 3500f;

        public MovementSystem(DefaultEcs.World world, WorldGrid? worldGrid = null, float maxSpeed = 420f)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {
            _worldGrid = worldGrid;
            _maxSpeed = maxSpeed;
        }

        protected override void Update(float dt, ReadOnlySpan<Entity> entities)
        {
            foreach (ref readonly var entity in entities)
            {
                ref var pos = ref entity.Get<Position>(); // feet position
                ref var vel = ref entity.Get<Velocity>();

                // --- Horizontal input target (read from MoveIntent if present) ---
                float targetSpeedX = 0f;
                if (entity.Has<MoveIntent>())
                {
                    ref var mi = ref entity.Get<MoveIntent>();
                    targetSpeedX = MathHelper.Clamp(mi.TargetSpeedX, -_maxSpeed, _maxSpeed);
                }

                // Determine whether grounded (use existing GroundedState if present)
                bool currentlyGrounded = false;
                if (entity.Has<GroundedState>())
                {
                    ref var gs = ref entity.Get<GroundedState>();
                    currentlyGrounded = gs.TimeSinceGrounded <= 0.001f;
                }
                else if (_worldGrid != null)
                {
                    currentlyGrounded = _worldGrid.IsGrounded(pos.Value, epsilon: 3f);
                }

                // Apply horizontal inertia: accelerate / decelerate toward targetSpeedX
                float accel = currentlyGrounded ? AccelGround : AccelAir;
                float decel = currentlyGrounded ? DecelGround : DecelAir;
                float diff = targetSpeedX - vel.Value.X;
                if (Math.Abs(diff) > 0.01f)
                {
                    if (Math.Sign(diff) == Math.Sign(targetSpeedX) || targetSpeedX == 0f)
                    {
                        float change = (Math.Abs(targetSpeedX) > Math.Abs(vel.Value.X)) ? accel * dt : decel * dt;
                        if (Math.Abs(change) > Math.Abs(diff)) change = Math.Abs(diff);
                        vel.Value = new Vector2(vel.Value.X + Math.Sign(diff) * change, vel.Value.Y);
                    }
                    else
                    {
                        float change = decel * dt;
                        if (Math.Abs(change) > Math.Abs(diff)) change = Math.Abs(diff);
                        vel.Value = new Vector2(vel.Value.X + Math.Sign(diff) * change, vel.Value.Y);
                    }
                }

                // Enforce max speed after inertia update
                if (Math.Abs(vel.Value.X) > _maxSpeed)
                {
                    vel.Value = new Vector2(Math.Sign(vel.Value.X) * _maxSpeed, vel.Value.Y);
                }

                // --- Vertical gravity (better jump feel) ---
                bool jumpHeld = false;
                if (entity.Has<InputState>())
                {
                    ref var isState = ref entity.Get<InputState>();
                    jumpHeld = isState.JumpHeld;
                }

                float gravityThisFrame = BaseGravity;
                if (vel.Value.Y > 0f)
                {
                    gravityThisFrame *= FallMultiplier;
                }
                else if (vel.Value.Y < 0f && !jumpHeld)
                {
                    gravityThisFrame *= LowJumpMultiplier;
                }

                vel.Value = new Vector2(vel.Value.X, MathHelper.Clamp(vel.Value.Y + gravityThisFrame * dt, -float.MaxValue, MaxFallSpeed));

                Vector2 proposed = pos.Value + vel.Value * dt;

                bool grounded = false;

                if (_worldGrid != null && entity.Has<Collider>())
                {
                    var colCopy = entity.Get<Collider>();
                    var colOffset = colCopy.Offset;
                    float halfW = colCopy.Size.X * 0.5f;
                    float h = colCopy.Size.Y;
                    var worldGrid = _worldGrid; // non-null local copy
                    int tileSize = worldGrid.TileSize;

                    // Helper to compute AABB from feet position
                    static void ComputeAABB(Vector2 feet, Vector2 offset, float halfWLocal, float heightLocal, out float left, out float right, out float top, out float bottom)
                    {
                        bottom = feet.Y + offset.Y;
                        top = bottom - heightLocal;
                        left = feet.X - halfWLocal + offset.X;
                        right = feet.X + halfWLocal + offset.X;
                    }

                    // Helper to check whether an AABB at feet world position overlaps any solid tile
                    bool IsAreaFreeWorld(float feetWorldX, float feetWorldY)
                    {
                        ComputeAABB(new Vector2(feetWorldX, feetWorldY), colOffset, halfW, h, out float left, out float right, out float top, out float bottom);
                        int lx = (int)Math.Floor(left / tileSize);
                        int rx = (int)Math.Floor((right - 0.001f) / tileSize);
                        int ty = (int)Math.Floor(top / tileSize);
                        int by = (int)Math.Floor((bottom - 0.001f) / tileSize);
                        for (int tx = lx; tx <= rx; tx++)
                        {
                            for (int yy = ty; yy <= by; yy++)
                            {
                                if (worldGrid.IsSolidTile(new Point(tx, yy))) return false;
                            }
                        }
                        return true;
                    }

                    // --- Vertical sweep (handle movement from current.Y to proposed.Y) ---
                    float resolvedY = proposed.Y;
                    if (!MathFExtensions.ApproximatelyEqual(proposed.Y, pos.Value.Y))
                    {
                        float checkX = proposed.X; // conservative: use proposed X

                        ComputeAABB(new Vector2(checkX, pos.Value.Y), colOffset, halfW, h, out float curLeft, out float curRight, out float curTop, out float curBottom);
                        int tx0 = (int)Math.Floor(curLeft / tileSize);
                        int tx1 = (int)Math.Floor((curRight - 0.001f) / tileSize);

                        if (proposed.Y > pos.Value.Y)
                        {
                            ComputeAABB(new Vector2(checkX, pos.Value.Y), colOffset, halfW, h, out _, out _, out _, out float cbottom);
                            ComputeAABB(new Vector2(checkX, proposed.Y), colOffset, halfW, h, out _, out _, out _, out float pbottom);

                            int startTileY = (int)Math.Floor((cbottom - 0.001f) / tileSize) + 1;
                            int endTileY = (int)Math.Floor((pbottom - 0.001f) / tileSize);

                            for (int ty = startTileY; ty <= endTileY && !grounded; ty++)
                            {
                                for (int tx = tx0; tx <= tx1; tx++)
                                {
                                    if (worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        resolvedY = ty * tileSize - colOffset.Y;
                                        grounded = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            ComputeAABB(new Vector2(checkX, pos.Value.Y), colOffset, halfW, h, out _, out _, out float ctop, out _);
                            ComputeAABB(new Vector2(checkX, proposed.Y), colOffset, halfW, h, out _, out _, out float ptop, out _);

                            int startTileY = (int)Math.Floor(ctop / tileSize) - 1;
                            int endTileY = (int)Math.Floor(ptop / tileSize);

                            for (int ty = startTileY; ty >= endTileY && !grounded; ty--)
                            {
                                for (int tx = tx0; tx <= tx1; tx++)
                                {
                                    if (worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        resolvedY = (ty + 1) * tileSize + h - colOffset.Y;
                                        grounded = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (grounded)
                        {
                            vel.Value = new Vector2(vel.Value.X, 0f);
                        }

                        proposed.Y = resolvedY;
                    }

                    // --- Horizontal sweep (handle movement from current.X to proposed.X) ---
                    float resolvedX = proposed.X;
                    if (!MathFExtensions.ApproximatelyEqual(proposed.X, pos.Value.X))
                    {
                        ComputeAABB(new Vector2(pos.Value.X, proposed.Y), colOffset, halfW, h, out float vLeft, out float vRight, out float vTop, out float vBottom);
                        int ty0 = (int)Math.Floor(vTop / tileSize);
                        int ty1 = (int)Math.Floor((vBottom - 0.001f) / tileSize);

                        bool collidedX = false;

                        // player's current column x index
                        int currentColX = (int)Math.Floor(pos.Value.X / tileSize);

                        if (proposed.X > pos.Value.X)
                        {
                            ComputeAABB(new Vector2(pos.Value.X, proposed.Y), colOffset, halfW, h, out float curL, out float curR, out _, out _);
                            ComputeAABB(new Vector2(proposed.X, proposed.Y), colOffset, halfW, h, out float propL, out float propR, out _, out _);

                            int startTileX = (int)Math.Floor((curR - 0.001f) / tileSize) + 1;
                            int endTileX = (int)Math.Floor((propR - 0.001f) / tileSize);

                            for (int tx = startTileX; tx <= endTileX && !collidedX; tx++)
                            {
                                for (int ty = ty0; ty <= ty1; ty++)
                                {
                                    if (worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        // Restrict stepping: only allow step up if target column top is exactly one tile higher than current column top
                                        int targetTop = worldGrid.GetTopMostSolidTileY(tx);
                                        int currentTop = worldGrid.GetTopMostSolidTileY(currentColX);
                                        if (targetTop == currentTop - 1)
                                        {
                                            float candidateFeetY = (targetTop) * tileSize - colOffset.Y; // feet aligned to target surface
                                            float candidateFeetX = (tx + 0.5f) * tileSize; // center of tile column
                                            if (IsAreaFreeWorld(candidateFeetX, candidateFeetY))
                                            {
                                                // Step up by one tile
                                                proposed.Y = candidateFeetY;
                                                resolvedX = proposed.X;
                                                vel.Value = new Vector2(vel.Value.X, 0f);
                                                collidedX = true;
                                                break;
                                            }
                                        }

                                        // Otherwise, normal horizontal collision
                                        float tileLeft = tx * tileSize;
                                        resolvedX = tileLeft - halfW - colOffset.X;
                                        // Horizontal collision: zero horizontal velocity.
                                        vel.Value = new Vector2(0f, vel.Value.Y);
                                        // normal horizontal collision, no airborne nudge
                                        collidedX = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            ComputeAABB(new Vector2(pos.Value.X, proposed.Y), colOffset, halfW, h, out float curL, out float curR, out _, out _);
                            ComputeAABB(new Vector2(proposed.X, proposed.Y), colOffset, halfW, h, out float propL, out float propR, out _, out _);

                            int startTileX = (int)Math.Floor(curL / tileSize) - 1;
                            int endTileX = (int)Math.Floor(propL / tileSize);

                            for (int tx = startTileX; tx >= endTileX && !collidedX; tx--)
                            {
                                for (int ty = ty0; ty <= ty1; ty++)
                                {
                                    if (worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        int targetTop = worldGrid.GetTopMostSolidTileY(tx);
                                        int currentTop = worldGrid.GetTopMostSolidTileY(currentColX);
                                        if (targetTop == currentTop - 1)
                                        {
                                            float candidateFeetY = (targetTop) * tileSize - colOffset.Y;
                                            float candidateFeetX = (tx + 0.5f) * tileSize;
                                            if (IsAreaFreeWorld(candidateFeetX, candidateFeetY))
                                            {
                                                proposed.Y = candidateFeetY;
                                                resolvedX = proposed.X;
                                                vel.Value = new Vector2(vel.Value.X, 0f);
                                                collidedX = true;
                                                break;
                                            }
                                        }

                                        float tileRight = (tx + 1) * tileSize;
                                        resolvedX = tileRight + halfW - colOffset.X;
                                        vel.Value = new Vector2(0f, vel.Value.Y);
                                        collidedX = true;
                                        break;
                                    }
                                }
                            }
                        }

                        proposed.X = resolvedX;
                    }

                    // Apply resolved position
                    pos.Value = proposed;
                }
                else
                {
                    // No collider or no world -> simple integration
                    pos.Value = proposed;
                }

                // Update GroundedState
                if (entity.Has<GroundedState>())
                {
                    ref var gs = ref entity.Get<GroundedState>();
                    gs.TimeSinceGrounded = grounded ? 0f : gs.TimeSinceGrounded + dt;
                }
                else
                {
                    entity.Set(new GroundedState { TimeSinceGrounded = grounded ? 0f : float.MaxValue });
                }
            }
        }
    }

    // Small helper for float approx
    internal static class MathFExtensions
    {
        public static bool ApproximatelyEqual(float a, float b, float eps = 0.0001f) => Math.Abs(a - b) <= eps;
    }
}
