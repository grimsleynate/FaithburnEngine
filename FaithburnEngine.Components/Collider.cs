using Microsoft.Xna.Framework;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Axis-aligned collider anchored at the entity's feet (bottom-center).
    /// Size is in pixels. Offset allows shifting the collider center from the feet.
    /// </summary>
    public struct Collider
    {
        public Vector2 Size; // width, height in pixels
        public Vector2 Offset; // additional center offset (in world pixels)
    }
}
