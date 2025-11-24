using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FaithburnEngine.Core.Inventory;
using FaithburnEngine.Content.Models;
using FaithburnEngine.World;
using FaithburnEngine.Core;

namespace FaithburnEngine.Systems
{
    public sealed class InteractionSystem
    {
        private readonly Content.ContentLoader _content;
        private readonly InventorySystem _inventorySystem;
        private readonly WorldGrid _world; // your world grid API

        public InteractionSystem(Content.ContentLoader content, InventorySystem invSys, WorldGrid world)
        {
            _content = content;
            _inventorySystem = invSys;
            _world = world;
        }

        // Call from Update with mouse state and player entity info
        public void HandleMouse(PlayerContext player, MouseState mouse, bool leftClick, bool rightClick)
        {
            var worldPos = ScreenToWorld(mouse.Position, player.Camera);
            var tileCoord = _world.WorldToTileCoord(worldPos);

            if (leftClick)
            {
                // Place block from hotbar if selected slot has a block item
                var slot = player.Inventory.Slots[player.HotbarIndex];
                if (!slot.IsEmpty)
                {
                    var itemDef = _content.Items.FirstOrDefault(i => i.Id == slot.ItemId);
                    if (itemDef != null && itemDef.Type == "block")
                    {
                        var placed = _world.PlaceBlock(tileCoord, itemDef.Id);
                        if (placed)
                        {
                            slot.Remove(1);
                        }
                    }
                }
            }

            if (rightClick)
            {
                var block = _world.GetBlock(tileCoord); // BlockDef
                var rule = _content.HarvestRules.FirstOrDefault(r => r.TargetBlockId == block.Id);
                if (rule != null)
                {
                    var equipped = player.Inventory.Slots[player.HotbarIndex];
                    var toolOk = CheckToolRequirement(equipped, rule);
                    if (toolOk)
                    {
                        _world.SetBlock(tileCoord, "air");

                        foreach (var kvp in rule.Yields)
                        {
                            var itemId = kvp.Key;
                            var count = kvp.Value;
                            _inventorySystem.AddToInventory(player.Inventory, itemId, count);
                        }
                    }
                }
            }
        }

        private bool CheckToolRequirement(InventorySlot slot, HarvestRule rule)
        {
            if (rule.ToolRequired == "any") return true;
            if (slot.IsEmpty) return false;
            var itemDef = _content.Items.FirstOrDefault(i => i.Id == slot.ItemId);
            if (itemDef == null) return false;
            // simple mapping: tool types are encoded in item.Type or stats
            if (rule.ToolRequired == "pick" && itemDef.Type == "tool" && itemDef.Stats.HarvestPower >= rule.MinHarvestPower) return true;
            if (rule.ToolRequired == "axe" && itemDef.Type == "tool" && itemDef.Stats.HarvestPower >= rule.MinHarvestPower) return true;
            return false;
        }

        private Vector2 ScreenToWorld(Point screen, Camera2D camera)
        {
            // simple conversion; adapt to your camera implementation
            return screen.ToVector2() + camera.Position;
        }
    }
}