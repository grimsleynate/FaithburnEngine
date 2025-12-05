using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Components
{
    // Component representing an item held by an entity (e.g., player holding a pickaxe).
    public struct HeldItem
    {
        public string ItemId;
        public Texture2D? Texture;
        // Offset from the entity's position (world pixels) where the pivot (handle end) should be placed
        public Vector2 Offset;
        // Pivot point in texture pixels (origin for rotation)
        public Vector2 Pivot;
        // Visual scale to draw the texture
        public float Scale;
        // Current rotation in radians
        public float Rotation;
        // Timing
        public float Duration;
        public float TimeLeft;
    }
}