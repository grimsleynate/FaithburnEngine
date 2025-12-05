using System.Collections.Generic;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Inventory storage for items.
    /// </summary>
    public struct Inventory
    {
        /// <summary>
        /// All items in this inventory.
        /// </summary>
        public List<ItemStack> Items;

        /// <summary>
        /// Max items this inventory can hold (50 for player, 27 for chest, etc).
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Create empty inventory with given capacity.
        /// </summary>
        public Inventory(int capacity)
        {
            Capacity = capacity;
            Items = new List<ItemStack>(capacity);
        }
    }

    /// <summary>
    /// Single item stack in inventory.
    /// </summary>
    public struct ItemStack
    {
        /// <summary>
        /// Item ID (e.g., "sword_iron", "potion_health", or null for empty).
        /// </summary>
        public string ItemId;

        /// <summary>
        /// How many of ItemId in this stack.
        /// </summary>
        public int Count;

        /// <summary>
        /// Item cooldown timer (seconds). Used for item-specific delays.
        /// </summary>
        public float Cooldown;
    }
}
