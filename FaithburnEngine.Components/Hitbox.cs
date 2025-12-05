using Microsoft.Xna.Framework;

namespace FaithburnEngine.Components
{
    // Transient hitbox component used for active weapon swings.
    public struct Hitbox
    {
        public Rectangle Rect; // world-space AABB
        public float TimeLeft; // seconds
    }
}
