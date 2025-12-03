using System.Collections.Generic;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Inventory storage for items.
    /// 
    /// TENET ALIGNMENT:
    /// - Tenet #4 (ECS): Struct-based, efficient for iterating thousands of entities
    /// - Tenet #3 (Efficient): Pre-allocated capacity, no runtime bounds checking overhead
    /// - Tenet #1 (Moddable): Separate from UI—modders create custom inventory UIs
    /// - Tenet #5 (Multiplayer): Value type, easily serializable for network sync
    /// 
    /// WHY STRUCT: Cache-friendly for ECS iteration. 1000 entities with Inventory
    /// packed tightly in memory = fast bulk operations (like Terraria's performance).
    /// </summary>
    public struct Inventory
    {
        /// <summary>
        /// All items in this inventory.
        /// 
        /// WHY List (not array):
        /// - Capacity is pre-allocated at construction ? O(1) add operations
        /// - Iteration speed equals array (tight memory layout)
        /// - More modder-friendly than fixed-size array
        /// </summary>
        public List<ItemStack> Items;

        /// <summary>
        /// Max items this inventory can hold (50 for player, 27 for chest, etc).
        /// 
        /// MODDER HOOK (Tenet #1):
        /// Create "Inventory Expansion" items that increase Capacity.
        /// Backpack mod? Increases Capacity. No code changes needed.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Create empty inventory with given capacity.
        /// 
        /// NOTE: Pre-allocates capacity. Capacity enforcement is caller's responsibility
        /// for performance (Tenet #3). No runtime bounds checking overhead.
        /// </summary>
        public Inventory(int capacity)
        {
            Capacity = capacity;
            Items = new List<ItemStack>(capacity);
        }
    }

    /// <summary>
    /// Single item stack in inventory.
    /// 
    /// STRUCT DESIGN (Tenet #4 - ECS, Tenet #5 - Multiplayer):
    /// Lightweight value type. Easily copied, serialized, sent over network.
    /// </summary>
    public struct ItemStack
    {
        /// <summary>
        /// Item ID (e.g., "sword_iron", "potion_health", or null for empty).
        /// 
        /// WHY STRING ID (Tenet #5 - Multiplayer):
        /// String IDs are serializable. ItemDefs are loaded once and shared locally.
        /// Network packet: Send "sword_iron", client resolves to ItemDef locally.
        /// Same approach as Terraria/Starbound (Tenet #2).
        /// </summary>
        public string ItemId;

        /// <summary>
        /// How many of ItemId in this stack.
        /// 
        /// EXAMPLE: "torch" ItemId with Count=42 means 42 torches.
        /// Different items have different stack limits (ItemDef.StackMax).
        /// </summary>
        public int Count;

        /// <summary>
        /// Item cooldown timer (seconds). Used for item-specific delays.
        /// 
        /// USES (Tenet #1 - Moddable):
        /// - Potion drinking: Set to 3.0, decrement each frame, check in InventorySystem
        /// - Pickaxe swing: Set to 0.5 to prevent spam-clicking (Terraria feel, Tenet #2)
        /// - Spell cast: Set to spell.Cooldown, prevents mana burning
        /// 
        /// WHY HERE (not separate CooldownSystem):
        /// Most items have one cooldown. Keeping it on ItemStack avoids extra lookups.
        /// Cache-efficient struct design (Tenet #3).
        /// </summary>
        public float Cooldown;
    }
}
