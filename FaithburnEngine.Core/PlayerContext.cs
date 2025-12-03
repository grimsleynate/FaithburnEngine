using Microsoft.Xna.Framework;

namespace FaithburnEngine.Core
{
    /// <summary>
    /// Holds player-specific state for systems to consume.
    /// 
    /// CORE TENET ALIGNMENT:
    /// - Tenet #4 (ECS first): This is a convenience wrapper during prototyping. Each field should
    ///   eventually become an ECS component (Position, Inventory, Hotbar, Camera). This structure
    ///   enables easy transition: delete PlayerContext, extract components into separate types.
    /// 
    /// - Tenet #3 (Efficient & Multi-threaded): Decoupling concerns allows independent update rates.
    ///   Camera can smooth-follow without affecting physics. Inventory can serialize without affecting
    ///   rendering. Supports scaling to thousands of entities.
    /// 
    /// - Tenet #5 (Multiplayer): Each player will be an ECS entity with these components attached.
    ///   This structure prepares us: easy to create Player_1, Player_2 entities with identical components.
    /// 
    /// WHY THIS DESIGN:
    /// Early development prioritizes rapid iteration. A single PlayerContext is faster to prototype
    /// than full ECS. Once systems are stable, components can be extracted without changing core logic.
    /// </summary>
    public sealed class PlayerContext
    {
        /// <summary>
        /// Inventory storage. Separated from UI (FaithburnEngine.UI) to keep mechanics independent
        /// of presentation. Enables multiple UIs, serialization, and testing without graphics.
        /// 
        /// FUTURE: Will become an InventoryComponent { items: ItemStack[]; capacity: int; }
        /// </summary>
        public Inventory.Inventory Inventory { get; }

        /// <summary>
        /// Currently selected hotbar slot index (0-11).
        /// 
        /// WHY SEPARATE:
        /// Selection state is independent of inventory items. Allows quick-switching and enables
        /// modders to create custom hotbar behaviors (filters, aliases, multi-key bindings) without
        /// modifying core inventory.
        /// 
        /// Tenet #1 (Moddable): Different hotbar layouts can be created by modding this component.
        /// </summary>
        public int HotbarIndex { get; set; }

        /// <summary>
        /// World position in pixels. Stored in PlayerContext for convenience during prototyping.
        /// 
        /// Tenet #4 (ECS): Will become a Position component { x: float; y: float; } used by all
        /// entities (enemies, NPCs, projectiles). Enables spatial queries: "All entities in chunk (5,10)?"
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Camera controlling viewport rendering. DECOUPLED from Position intentionally.
        /// 
        /// WHY DECOUPLED:
        /// Enables smooth camera lag, zoom, screen shake, split-screen—all without affecting physics.
        /// 
        /// Tenet #3 (Efficient): Camera updates independently of entity position updates. Can use
        /// different interpolation/damping without impacting collision/physics performance.
        /// 
        /// Tenet #5 (Multiplayer): Each player needs independent camera frustum for split-screen
        /// or shared-screen gameplay.
        /// </summary>
        public Camera2D Camera { get; }

        public PlayerContext(int inventorySlots)
        {
            Inventory = new Inventory.Inventory(inventorySlots);
            HotbarIndex = 0;
            Position = Vector2.Zero;
            Camera = new Camera2D();
        }
    }

    /// <summary>
    /// Simple 2D camera for viewport management.
    /// 
    /// STUB IMPLEMENTATION: Handles translation only. Future enhancements (zoom, rotation, screen
    /// shake, parallax) can be added without breaking existing code.
    /// 
    /// EFFICIENCY (Tenet #3): GetTransform() is O(1) and called once per frame. Matrix math is
    /// cheap. No performance scaling issues even with thousands of entities.
    /// 
    /// MODDABILITY (Tenet #1): Modders can subclass or patch GetTransform() to implement custom
    /// camera behaviors (follow, free-look, fixed point, shake, zoom).
    /// </summary>
    public sealed class Camera2D
    {
        /// <summary>
        /// Camera position in world space (pixels).
        /// 
        /// Example: Player at (500, 300), viewport 1920x1080 ? Camera.Position ? (-460, -240)
        /// to center player on screen.
        /// </summary>
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// Returns transformation matrix for world?screen rendering.
        /// 
        /// Pass to: SpriteBatch.Begin(transformMatrix: GetTransform())
        /// 
        /// MATH: Matrix.CreateTranslation(-Position.X, -Position.Y, 0)
        /// Negates position so camera movement translates world opposite direction.
        /// </summary>
        public Matrix GetTransform()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
        }
    }
}
