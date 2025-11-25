using System;

namespace FaithburnEngine.UI
{
    public interface IInventoryAdapter
    {
        int SlotCount { get; }
        InventorySlotView GetSlot(int index);
        event Action<int> OnSlotChanged;
    }
}