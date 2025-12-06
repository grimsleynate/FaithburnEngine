using System;
using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Systems.HeldAnimations
{
    public interface IHeldItemAnimator
    {
        void Begin(Entity entity, Components.HeldItem hi);
        void Update(Entity entity, ref Components.HeldItem hi, float dt);
        bool ShouldContinue(Entity entity, ref Components.HeldItem hi, Microsoft.Xna.Framework.Input.MouseState mouse);
    }

    // Swing animation: pivot from player's center, lower-left CoG, rotate over time.
    public sealed class SwingAnimator : IHeldItemAnimator
    {
        public void Begin(Entity entity, Components.HeldItem hi)
        {
            // Set pivot to lower-left corner of the item texture (CoG for swing)
            var tex = hi.Texture;
            hi.Pivot = tex != null ? new Vector2(0f, tex.Height) : Vector2.Zero;

            // Compute offset from player feet to player center-body (hand position)
            // Position is feet; offset up to roughly chest/hand height
            float handOffsetY = -48f; // 48px above feet (roughly mid-body for 96px tall sprite)
            
            // Determine facing direction from sprite
            float facingDir = 1f; // default right
            if (entity.Has<Components.Sprite>())
            {
                var effects = entity.Get<Components.Sprite>().Effects;
                // Player art faces LEFT by default; FlipHorizontally means facing RIGHT
                facingDir = effects.HasFlag(SpriteEffects.FlipHorizontally) ? 1f : -1f;
            }

            // Offset X slightly toward facing direction so item appears in front of player
            float handOffsetX = facingDir * 16f;

            hi.Offset = new Vector2(handOffsetX, handOffsetY);
            hi.Rotation = 0f;
            hi.HitboxSpawned = false;
        }

        public void Update(Entity entity, ref Components.HeldItem hi, float dt)
        {
            hi.TimeLeft -= dt;
            float t = MathF.Max(0f, 1f - (hi.TimeLeft / MathF.Max(0.0001f, hi.Duration)));
            
            // Swing arc: start at ~-45deg, swing through to ~+45deg
            float startAngle = -0.8f; // radians, roughly -45deg
            float endAngle = 0.8f;
            float swing = startAngle + (endAngle - startAngle) * MathF.Sin(t * MathF.PI);
            
            // Flip rotation direction based on facing
            if (entity.Has<Components.Sprite>())
            {
                var effects = entity.Get<Components.Sprite>().Effects;
                if (!effects.HasFlag(SpriteEffects.FlipHorizontally))
                {
                    swing = -swing; // mirror for left-facing
                }
            }

            hi.Rotation = swing;
        }

        public bool ShouldContinue(Entity entity, ref Components.HeldItem hi, Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        }
    }
}
