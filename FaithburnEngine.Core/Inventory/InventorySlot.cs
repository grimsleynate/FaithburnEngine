namespace FaithburnEngine.Core.Inventory
{
    public sealed class InventorySlot
    {
        public string? ItemId { get; set; }
        public int Count { get; set; }

        public InventorySlot() { ItemId = null; Count = 0; }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId);

        public void Set(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public void Clear()
        {
            ItemId = null;
            Count = 0;
        }

        public int Add(int amount, int stackMax)
        {
            if (IsEmpty) return 0;
            var space = stackMax - Count;
            var toAdd = Math.Min(space, amount);
            Count += toAdd;
            return toAdd;
        }

        public int Remove(int amount)
        {
            if (IsEmpty) return 0;
            var toRemove = Math.Min(Count, amount);
            Count -= toRemove;
            return toRemove;
        }

        public bool CanAccept(string itemId, int stackMax)
        {
            if (IsEmpty) return true;
            return ItemId == itemId && Count < stackMax;
        }
    }
}