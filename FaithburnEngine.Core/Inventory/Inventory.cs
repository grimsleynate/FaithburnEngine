namespace FaithburnEngine.Core.Inventory
{
    public sealed class Inventory
    {
        public InventorySlot[] Slots { get; }

        public Inventory(int slotCount)
        {
            Slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++) Slots[i] = new InventorySlot();
        }

        // Try to add an item stack, returns leftover count not added
        public int AddItem(string itemId, int count, Func<string, int> getStackMax)
        {
            var stackMax = getStackMax(itemId);

            // First pass: merge into existing stacks
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                var s = Slots[i];
                if (!s.IsEmpty && s.ItemId == itemId)
                {
                    var added = s.Add(count, stackMax);
                    count -= added;
                }
            }

            // Second pass: fill empty slots
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                var s = Slots[i];
                if (s.IsEmpty)
                {
                    var toPlace = Math.Min(count, stackMax);
                    s.Set(itemId, toPlace);
                    count -= toPlace;
                }
            }

            return count; // leftover
        }

        public bool RemoveItem(string itemId, int count)
        {
            var total = 0;
            foreach (var s in Slots) if (!s.IsEmpty && s.ItemId == itemId) total += s.Count;
            if (total < count) return false;

            var remaining = count;
            for (int i = 0; i < Slots.Length && remaining > 0; i++)
            {
                var s = Slots[i];
                if (!s.IsEmpty && s.ItemId == itemId)
                {
                    var removed = s.Remove(remaining);
                    remaining -= removed;
                }
            }
            return true;
        }
    }
}