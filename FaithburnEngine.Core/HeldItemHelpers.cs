using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Core
{
    public static class HeldItemHelpers
    {
        // Compute hand offset and pivot for held item drawing.
        // - playerTexWidth/Height and playerScale: player's sprite dimensions and scale.
        // - playerEffects: sprite effects for facing information.
        // - hasSprite: whether player uses a sprite; if false, colliderSize is used instead.
        // - colliderSize: player's collider size used when no sprite available.
        // - itemPivotX/Y: optional pivot override in texture pixels
        // - itemHandOffsetX/Y: optional hand offset override relative to player's feet
        // - itemTextureHeight: height of the held item texture in pixels (used for default pivot).
        public static (Vector2 Offset, Vector2 Pivot) ComputeVisuals(
            int playerTexWidth,
            int playerTexHeight,
            float playerScale,
            SpriteEffects playerEffects,
            bool hasSprite,
            Vector2 colliderSize,
            int? itemPivotX,
            int? itemPivotY,
            int? itemHandOffsetX,
            int? itemHandOffsetY,
            int itemTextureHeight)
        {
            float dir = (playerEffects == SpriteEffects.FlipHorizontally) ? 1f : -1f;

            // Default spawn point: player hand
            // 32px toward facing from player center X, and 48px above feet
            Vector2 playerHalf = new Vector2(dir * 32f, -48f);

            // Compute pivot
            Vector2 pivot;
            // default pivot at bottom-left of item texture
            pivot = new Vector2(0f, itemTextureHeight);

            return (playerHalf, pivot);
        }
    }
}
