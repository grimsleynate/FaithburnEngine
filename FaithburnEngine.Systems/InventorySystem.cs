using FaithburnEngine.Core.Inventory;

namespace FaithburnEngine.Systems
{
    public sealed class InventorySystem
    {
        private readonly Content.ContentLoader _content;

        public InventorySystem(Content.ContentLoader content)
        {
            _content = content;
        }

        public int GetStackMax(string itemId)
        {
            var def = _content.Items.FirstOrDefault(i => i.Id == itemId);
            return def?.StackMax ?? 99;
        }

        // Example helper to add to player's inventory
        public int AddToInventory(Inventory inv, string itemId, int count)
        {
            return inv.AddItem(itemId, count, GetStackMax);
        }
    }
}