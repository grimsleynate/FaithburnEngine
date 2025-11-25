using System;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Core.Inventory; // your core inventory namespace

namespace FaithburnEngine.UI
{
    // Adapter that wraps your core Inventory instance.
    // TODO: replace iconResolver with your actual icon lookup/cache.
    public class InventoryAdapterFromCore : IInventoryAdapter
    {
        readonly Inventory coreInventory;
        readonly Func<string, Texture2D> iconResolver;

        public InventoryAdapterFromCore(Inventory coreInventory, Func<string, Texture2D> iconResolver)
        {
            this.coreInventory = coreInventory ?? throw new ArgumentNullException(nameof(coreInventory));
            this.iconResolver = iconResolver ?? throw new ArgumentNullException(nameof(iconResolver));
            // If your core Inventory exposes change events, subscribe here and forward them.
            // Example (if you add an event): coreInventory.SlotChanged += idx => OnSlotChanged?.Invoke(idx);
        }

        public int SlotCount => coreInventory.Slots.Length;

        public InventorySlotView GetSlot(int index)
        {
            var src = coreInventory.Slots[index]; // uses your InventorySlot type
            var view = new InventorySlotView
            {
                ItemId = src.ItemId,
                Count = src.Count,
                Icon = string.IsNullOrEmpty(src.ItemId) ? null : iconResolver(src.ItemId)
            };
            return view;
        }

        public event Action<int> OnSlotChanged;
        // If you add a subscription to core inventory changes, call OnSlotChanged?.Invoke(index) there.
    }
}