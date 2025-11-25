using System.Collections.Generic;

namespace FaithburnEngine.Components
{
    public struct Inventory
    {
        public List<ItemStack> Items;

        public int Capacity;

        public Inventory(int capacity)
        {
            Capacity = capacity;
            Items = new List<ItemStack>(capacity);
        }
    }

    public struct ItemStack
    {
        public string ItemId;
        public int Count;
        public float Cooldown; // optional per‑item timer
    }
}