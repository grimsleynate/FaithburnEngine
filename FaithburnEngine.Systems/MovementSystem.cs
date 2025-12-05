using DefaultEcs;
using DefaultEcs.System;
using FaithburnEngine.Components;
using Microsoft.Xna.Framework;
using FaithburnEngine.World;
using System;

namespace FaithburnEngine.Systems
{
    /// <summary>
    /// Updates Position based on Velocity. Applies gravity and AABB collision against world tiles when Collider is present.
    /// Uses swept checks to avoid tunneling at high speed.
    /// </summary>
    public sealed class MovementSystem : AEntitySetSystem<float>
    {
        private readonly WorldGrid? _worldGrid;

        // Physics tuning
        private const float Gravity = 2200f; // pixels/s^2
        private const float MaxFallSpeed = 1600f; // terminal velocity

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

                // Apply gravity
                vel.Value = new Vector2(vel.Value.X, MathHelper.Clamp(vel.Value.Y + Gravity * dt, -float.MaxValue, MaxFallSpeed));

                Vector2 current = pos.Value;
                Vector2 proposed = current + vel.Value * dt;

                bool grounded = false;

                if (_worldGrid != null && entity.Has<Collider>())
                {
                    ref var col = ref entity.Get<Collider>();
                    float halfW = col.Size.X * 0.5f;
                    float h = col.Size.Y;

                    // Helper to compute AABB from feet position
                    static void ComputeAABB(Vector2 feet, Vector2 offset, float halfW, float height, out float left, out float right, out float top, out float bottom)
                    {
                        bottom = feet.Y + offset.Y;
                        top = bottom - height;
                        left = feet.X - halfW + offset.X;
                        right = feet.X + halfW + offset.X;
                    }

                    // --- Vertical sweep (handle movement from current.Y to proposed.Y) ---
                    float resolvedY = proposed.Y;
                    if (!MathFExtensions.ApproximatelyEqual(proposed.Y, current.Y))
                    {
                        // Build horizontal span using feet X at current/proposed average
                        float checkX = proposed.X; // conservative: use proposed X

                        // Determine horizontal tile range that collider spans
                        ComputeAABB(new Vector2(checkX, current.Y), col.Offset, halfW, h, out float curLeft, out float curRight, out float curTop, out float curBottom);
                        int tx0 = (int)Math.Floor(curLeft / _worldGrid.TileSize);
                        int tx1 = (int)Math.Floor((curRight - 0.001f) / _worldGrid.TileSize);

                        if (proposed.Y > current.Y)
                        {
                            // moving down: check tiles between currentBottom -> proposedBottom
                            ComputeAABB(new Vector2(checkX, current.Y), col.Offset, halfW, h, out _, out _, out _, out float cbottom);
                            ComputeAABB(new Vector2(checkX, proposed.Y), col.Offset, halfW, h, out _, out _, out _, out float pbottom);

                            int startTileY = (int)Math.Floor((cbottom - 0.001f) / _worldGrid.TileSize) + 1;
                            int endTileY = (int)Math.Floor((pbottom - 0.001f) / _worldGrid.TileSize);

                            for (int ty = startTileY; ty <= endTileY && !grounded; ty++)
                            {
                                for (int tx = tx0; tx <= tx1; tx++)
                                {
                                    if (_worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        // place feet on top of this tile
                                        resolvedY = ty * _worldGrid.TileSize - col.Offset.Y;
                                        grounded = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // moving up: check tiles between currentTop -> proposedTop
                            ComputeAABB(new Vector2(checkX, current.Y), col.Offset, halfW, h, out _, out _, out float ctop, out _);
                            ComputeAABB(new Vector2(checkX, proposed.Y), col.Offset, halfW, h, out _, out _, out float ptop, out _);

                            int startTileY = (int)Math.Floor(ctop / _worldGrid.TileSize) - 1;
                            int endTileY = (int)Math.Floor(ptop / _worldGrid.TileSize);

                            for (int ty = startTileY; ty >= endTileY && !grounded; ty--)
                            {
                                for (int tx = tx0; tx <= tx1; tx++)
                                {
                                    if (_worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        // place feet so collider top is just below tile bottom
                                        resolvedY = (ty + 1) * _worldGrid.TileSize + h - col.Offset.Y;
                                        // not grounded when hitting head
                                        grounded = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (grounded)
                        {
                            // landed: zero vertical velocity
                            vel.Value = new Vector2(vel.Value.X, 0f);
                        }

                        proposed.Y = resolvedY;
                    }

                    // --- Horizontal sweep (handle movement from current.X to proposed.X) ---
                    float resolvedX = proposed.X;
                    if (!MathFExtensions.ApproximatelyEqual(proposed.X, current.X))
                    {
                        // Build vertical span using feet Y after vertical resolution
                        ComputeAABB(new Vector2(current.X, proposed.Y), col.Offset, halfW, h, out float vLeft, out float vRight, out float vTop, out float vBottom);
                        int ty0 = (int)Math.Floor(vTop / _worldGrid.TileSize);
                        int ty1 = (int)Math.Floor((vBottom - 0.001f) / _worldGrid.TileSize);

                        bool collidedX = false;

                        if (proposed.X > current.X)
                        {
                            // moving right: check tiles between currentRight -> proposedRight
                            ComputeAABB(new Vector2(current.X, proposed.Y), col.Offset, halfW, h, out float curL, out float curR, out _, out _);
                            ComputeAABB(new Vector2(proposed.X, proposed.Y), col.Offset, halfW, h, out float propL, out float propR, out _, out _);

                            int startTileX = (int)Math.Floor((curR - 0.001f) / _worldGrid.TileSize) + 1;
                            int endTileX = (int)Math.Floor((propR - 0.001f) / _worldGrid.TileSize);

                            for (int tx = startTileX; tx <= endTileX && !collidedX; tx++)
                            {
                                for (int ty = ty0; ty <= ty1; ty++)
                                {
                                    if (_worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        // place feet so collider left side touches tile left
                                        float tileLeft = tx * _worldGrid.TileSize;
                                        resolvedX = tileLeft - halfW - col.Offset.X;
                                        vel.Value = new Vector2(0f, vel.Value.Y);
                                        collidedX = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // moving left
                            ComputeAABB(new Vector2(current.X, proposed.Y), col.Offset, halfW, h, out float curL, out float curR, out _, out _);
                            ComputeAABB(new Vector2(proposed.X, proposed.Y), col.Offset, halfW, h, out float propL, out float propR, out _, out _);

                            int startTileX = (int)Math.Floor(curL / _worldGrid.TileSize) - 1;
                            int endTileX = (int)Math.Floor(propL / _worldGrid.TileSize);

                            for (int tx = startTileX; tx >= endTileX && !collidedX; tx--)
                            {
                                for (int ty = ty0; ty <= ty1; ty++)
                                {
                                    if (_worldGrid.IsSolidTile(new Point(tx, ty)))
                                    {
                                        float tileRight = (tx + 1) * _worldGrid.TileSize;
                                        resolvedX = tileRight + halfW - col.Offset.X;
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
