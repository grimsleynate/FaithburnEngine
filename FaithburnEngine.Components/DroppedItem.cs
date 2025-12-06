using Microsoft.Xna.Framework;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Component for items dropped in the world that can be picked up by the player.
    /// WHY separate from inventory: Dropped items exist as world entities with physics,
    /// rendering, and pickup behavior. They transition to inventory slots when collected.
    /// This matches Terraria/Starbound where mined blocks become collectible world items.
    /// </summary>
    public struct DroppedItem
    {
        /// <summary>
        /// The item ID that will be added to inventory when collected.
        /// </summary>
        public string ItemId;
        
        /// <summary>
        /// Number of items in this stack.
        /// </summary>
        public int Count;
        
        /// <summary>
        /// Time remaining before this item can be picked up (seconds).
        /// WHY: Prevents instant re-pickup when mining, gives visual feedback.
        /// </summary>
        public float PickupDelay;
        
        /// <summary>
        /// Whether this item is currently being magnetized toward a player.
        /// </summary>
        public bool IsMagnetized;
        
        /// <summary>
        /// Entity reference to the player magnetizing this item (if any).
        /// Used to continue magnetizing toward the same player.
        /// </summary>
        public int MagnetTargetId;
    }
}
