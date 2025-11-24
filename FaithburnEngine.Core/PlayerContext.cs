using Microsoft.Xna.Framework;

namespace FaithburnEngine.Core
{
    /// <summary>
    /// Holds player-specific state for systems to consume.
    /// </summary>
    public sealed class PlayerContext
    {
        public Inventory.Inventory Inventory { get; }
        public int HotbarIndex { get; set; }
        public Vector2 Position { get; set; }
        public Camera2D Camera { get; }  // implement a simple Camera2D later

        public PlayerContext(int inventorySlots)
        {
            Inventory = new Inventory.Inventory(inventorySlots);
            HotbarIndex = 0;
            Position = Vector2.Zero;
            Camera = new Camera2D();
        }
    }

    /// <summary>
    /// Minimal camera stub. Expand with zoom/rotation later.
    /// </summary>
    public sealed class Camera2D
    {
        public Vector2 Position { get; set; } = Vector2.Zero;

        public Matrix GetTransform()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
        }
    }
}