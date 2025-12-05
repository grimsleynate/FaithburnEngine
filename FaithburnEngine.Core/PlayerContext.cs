using Microsoft.Xna.Framework;

namespace FaithburnEngine.Core
{
    public sealed class PlayerContext
    {
        /// <summary>
        /// Inventory storage. Separated from UI (FaithburnEngine.UI) to keep mechanics independent
        /// of presentation. Enables multiple UIs, serialization, and testing without graphics.
        /// </summary>
        public Inventory.Inventory Inventory { get; }

        /// <summary>
        /// Currently selected hotbar slot index (0-11).
        /// </summary>
        public int HotbarIndex { get; set; }

        /// <summary>
        /// World position in pixels (feet origin). We use FEET as origin so sprite rendering with
        /// bottom-center origins aligns naturally to ground contact and collision.
        /// Stored in PlayerContext for convenience during prototyping.
        ///
        /// Tenet #4 (ECS): Will become a `Position` component { x: float; y: float; } used by all
        /// entities (enemies, NPCs, projectiles). Enables spatial queries: "All entities in chunk (5,10)?".
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Camera controlling viewport rendering. DECOUPLED from Position intentionally.
        /// 
        /// WHY DECOUPLED:
        /// Enables smooth camera lag, zoom, screen shake, split-screen without affecting physics.
        /// 
        /// Tenet #3 (Efficient): Camera updates independently of entity position updates. Can use
        /// different interpolation/damping without impacting collision/physics performance.
        /// 
        /// Tenet #5 (Multiplayer): Each player needs independent camera frustum for split-screen
        /// or shared-screen gameplay.
        /// </summary>

        public PlayerContext(int inventorySlots)
        {
            Inventory = new Inventory.Inventory(inventorySlots);
            HotbarIndex = 0;
            Position = Vector2.Zero;
        }
    }
}
