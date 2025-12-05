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

            Vector2 playerHalf;
            if (hasSprite && playerTexWidth > 0 && playerTexHeight > 0)
            {
                float scale = playerScale <= 0f ? 1f : playerScale;
                float halfHeight = playerTexHeight * 0.5f * scale;
                float halfWidth = playerTexWidth * 0.5f * scale;

                float xEdge = dir * (halfWidth + VisualConstants.HandEdgePadding);
                playerHalf = new Vector2(xEdge, -halfHeight + VisualConstants.HandYAdjust);
                playerHalf.X -= dir * VisualConstants.HandBodyOffset;
                playerHalf.Y += VisualConstants.HandBodyOffset;
            }
            else
            {
                float halfHeight = colliderSize.Y * 0.5f;
                float halfWidth = colliderSize.X * 0.5f;
                float xEdge = dir * (halfWidth + VisualConstants.HandEdgePadding);
                playerHalf = new Vector2(xEdge, -halfHeight);
            }

            // If item provides explicit HandOffset, use that (relative to feet)
            if (itemHandOffsetX.HasValue && itemHandOffsetY.HasValue)
            {
                playerHalf = new Vector2(itemHandOffsetX.Value, itemHandOffsetY.Value);
            }

            // Compute pivot
            Vector2 pivot;
            if (itemPivotX.HasValue && itemPivotY.HasValue)
            {
                pivot = new Vector2(itemPivotX.Value, itemPivotY.Value);
            }
            else
            {
                // default pivot at bottom-left of item texture
                pivot = new Vector2(0f, itemTextureHeight);
            }

            return (playerHalf, pivot);
        }
    }
}
