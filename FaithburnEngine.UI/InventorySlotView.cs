using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.UI
{
    // UI-facing, lightweight view of a core inventory slot.
    public struct InventorySlotView
    {
        public string ItemId;   // null or empty = empty
        public int Count;
        public Texture2D Icon;  // may be null; adapter should provide cached textures
        public bool IsEmpty => string.IsNullOrEmpty(ItemId);
    }
}