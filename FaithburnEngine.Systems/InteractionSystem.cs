using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FaithburnEngine.Core.Inventory;
using FaithburnEngine.Content.Models;
using FaithburnEngine.World;
using FaithburnEngine.Core;
using FaithburnEngine.Content.Models.Enums;
using DefaultEcs.System;
using FaithburnEngine.Rendering;

namespace FaithburnEngine.Systems
{
    public sealed class InteractionSystem : ISystem<float>
    {
        private readonly Content.ContentLoader _content;
        private readonly InventorySystem _inventorySystem;
        private readonly WorldGrid _world; // your world grid API
        private readonly Camera2D _camera;

        public bool IsEnabled { get; set; } = true;

        public InteractionSystem(Content.ContentLoader content, InventorySystem invSys, WorldGrid world, Camera2D camera)
        {
            _content = content;
            _inventorySystem = invSys;
            _world = world;
            _camera = camera;
        }

        // Call from Update with mouse state and player entity info
        public void HandleMouse(PlayerContext player, MouseState mouse, bool leftClick, bool rightClick)
        {
            var worldPos = ScreenToWorld(mouse.Position, _camera);
            var tileCoord = _world.WorldToTileCoord(worldPos);

            
        }

        private void TryHarvest(Point tileCoord, ItemDef? toolDef, InventorySlot equippedSlot, PlayerContext player)
        {
            var block = _world.GetBlock(tileCoord);
            var rule = _content.HarvestRules.FirstOrDefault(r => r.TargetBlockId == block.Id);
            if (rule == null) return;

            // Check tool requirement
            var toolOk = CheckToolRequirement(toolDef, rule);
            if (!toolOk) return;

            // For PoC: instant harvest. Later use harvestTime and progress bar.
            _world.SetBlock(tileCoord, "air");

            var rng = new Random(); // consider injecting RNG for determinism in tests
            foreach (var y in rule.Yields)
            {
                if (rng.NextDouble() <= y.Chance)
                {
                    var count = y.MinCount == y.MaxCount ? y.MinCount : rng.Next(y.MinCount, y.MaxCount + 1);
                    _inventorySystem.AddToInventory(player.Inventory, y.ItemId, count);
                }
            }
        }

        private bool CheckToolRequirement(ItemDef? toolDef, HarvestRule rule)
        {
            if (toolDef == null) return false;
            if (toolDef.Type != ItemType.Tool) return false;

            // ToolKind must match or be a superset (e.g., Drill can act as Pickaxe if desired)
            if (toolDef.ToolKind == rule.ToolRequired) return true;

            // Optionally allow tools with higher harvest power to satisfy requirement
            if (toolDef.Stats.HarvestPower >= rule.MinHarvestPower) return true;

            return false;
        }


        private Vector2 ScreenToWorld(Point screen, Camera2D camera)
        {
            var s = screen.ToVector2();
            var origin = camera.Origin;
            var zoom = Math.Max(0.0001f, camera.Zoom);
            // Inverse of: world -> translate(-position) -> translate(-origin) -> scale(zoom) -> translate(origin)
            return (s - origin) / zoom + camera.Position;

        }

        public void Update(float dt)
        {

        }

        public void Dispose()
        {
            
        }
    }
}