using System;
using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FaithburnEngine.Systems.HeldAnimations
{
    public sealed class ThrustAnimator : IHeldItemAnimator
    {
        public void Begin(Entity entity, Components.HeldItem hi)
        {
            var tex = hi.Texture;
            hi.Pivot = tex != null ? new Vector2(tex.Width * 0.5f, tex.Height) : Vector2.Zero;
            // Compute center-bottom from collider: feet (Position) minus half height
            var feet = entity.Get<Components.Position>().Value;
            Vector2 centerBottom = feet;
            if (entity.Has<Components.Collider>())
            {
                ref var col = ref entity.Get<Components.Collider>();
                centerBottom = new Vector2(feet.X + col.Offset.X, feet.Y + col.Offset.Y - col.Size.Y * 0.5f);
            }
            hi.Offset = centerBottom - feet;
            hi.Rotation = 0f;
            hi.HitboxSpawned = false;
        }

        public void Update(Entity entity, ref Components.HeldItem hi, float dt)
        {
            hi.TimeLeft -= dt;
            float t = MathF.Max(0f, 1f - (hi.TimeLeft / MathF.Max(0.0001f, hi.Duration)));

            var feet = entity.Get<Components.Position>().Value;
            Vector2 centerBottom = feet;
            if (entity.Has<Components.Collider>())
            {
                ref var col = ref entity.Get<Components.Collider>();
                centerBottom = new Vector2(feet.X + col.Offset.X, feet.Y + col.Offset.Y - col.Size.Y * 0.5f);
            }

            // Direction toward world-space aim target
            Vector2 dir = hi.AimTarget - centerBottom;
            if (dir.LengthSquared() > 0.0001f) dir.Normalize();

            float outDist = 16f;
            float half = 0.5f;
            float dist = t <= half
                ? outDist * EaseOutQuad(t / half)
                : outDist * (1f - EaseInQuad((t - half) / half));

            hi.Offset = (centerBottom - feet) + dir * dist;
        }

        public bool ShouldContinue(Entity entity, ref Components.HeldItem hi, MouseState mouse)
        {
            return mouse.LeftButton == ButtonState.Pressed;
        }

        private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
        private static float EaseInQuad(float x) => x * x;
    }
}
