using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Component representing an item held by an entity (e.g., player holding a pickaxe).
    /// </summary>
    public struct HeldItem
    {
        /// <summary>
        /// Identifier for the held item (game-specific).
        /// </summary>
        public string ItemId;
        /// <summary>
        /// Texture to draw for the held item. May be null if not set.
        /// </summary>
        public Texture2D? Texture;
        /// <summary>
        /// Offset from the entity's position (world pixels) where the pivot (handle end) should be placed.
        /// </summary>
        // Offset from the entity's position (world pixels) where the pivot (handle end) should be placed
        public Vector2 Offset;
        /// <summary>
        /// Pivot point in texture pixels (origin for rotation).
        /// </summary>
        // Pivot point in texture pixels (origin for rotation)
        public Vector2 Pivot;
        /// <summary>
        /// Visual scale to draw the texture.
        /// </summary>
        // Visual scale to draw the texture
        public float Scale;
        /// <summary>
        /// Current rotation of the held item in radians.
        /// </summary>
        // Current rotation in radians
        public float Rotation;
        /// <summary>
        /// Total duration (seconds) of the current animation/action.
        /// </summary>
        // Timing
        public float Duration;
        /// <summary>
        /// Remaining time (seconds) for the current animation/action.
        /// </summary>
        public float TimeLeft;
        /// <summary>
        /// Whether a hitbox has been spawned for the current swing/action.
        /// </summary>
        // Whether we've spawned a hitbox for the current swing
        public bool HitboxSpawned;
    }
}