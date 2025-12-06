using System;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Components;
using FaithburnEngine.Content;
using FaithburnEngine.Content.Models;
using FaithburnEngine.Content.Models.Enums;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;
using FaithburnEngine.World;

namespace FaithburnEngine.Systems
{
    /// <summary>
    /// Handles mining/harvesting of blocks in the world.
    /// 
    /// WHY this design (Terraria/Starbound-like):
    /// - Mining requires holding the attack button on a tile within range
    /// - Progress accumulates based on tool power vs block hardness
    /// - Switching targets or releasing resets progress
    /// - When complete, block is destroyed and item drops spawn
    /// - Tool type must match block requirements (pickaxe for stone, axe for wood)
    /// 
    /// Formula: HarvestTime = BaseTime * (BlockHardness / ToolPower)
    /// Example: Hardness 1 block with Power 5 tool = 1.0 * (1/5) = 0.2 seconds
    /// </summary>
    public sealed class HarvestingSystem : ISystem<float>
    {
        private readonly DefaultEcs.World _world;
        private readonly IWorldGrid _worldGrid;
        private readonly ContentLoader _content;
        private readonly Camera2D _camera;
        private readonly PlayerContext _player;
        private readonly GraphicsDevice _graphics;
        
        private MouseState _prevMouse;
        private Entity? _playerEntity;

        public bool IsEnabled { get; set; } = true;

        public HarvestingSystem(
            DefaultEcs.World world, 
            IWorldGrid worldGrid, 
            ContentLoader content, 
            Camera2D camera, 
            PlayerContext player,
            GraphicsDevice graphics)
        {
            _world = world;
            _worldGrid = worldGrid;
            _content = content;
            _camera = camera;
            _player = player;
            _graphics = graphics;
            _prevMouse = Mouse.GetState();
        }

        public void Update(float dt)
        {
            var mouse = Mouse.GetState();
            bool mining = mouse.LeftButton == ButtonState.Pressed;
            
            // Find player entity (cache for performance)
            if (_playerEntity == null || !_playerEntity.Value.IsAlive)
            {
                foreach (var e in _world.GetEntities().With<Position>().With<Velocity>().With<Collider>().AsEnumerable())
                {
                    _playerEntity = e;
                    break;
                }
            }
            
            if (_playerEntity == null || !_playerEntity.Value.IsAlive)
            {
                _prevMouse = mouse;
                return;
            }

            var playerEntity = _playerEntity.Value;
            ref var playerPos = ref playerEntity.Get<Position>();
            
            // Get currently held item
            var heldItem = GetHeldItem();
            if (heldItem == null || !heldItem.Type.HasFlag(ItemType.Tool))
            {
                // Not holding a tool - clear any mining state
                if (playerEntity.Has<MiningState>())
                    playerEntity.Remove<MiningState>();
                _prevMouse = mouse;
                return;
            }

            // Convert mouse position to world coordinates
            var mouseWorld = ScreenToWorld(new Point(mouse.X, mouse.Y));
            var targetTile = _worldGrid.WorldToTileCoord(mouseWorld);
            
            // Check if tile is in range
            var tileCenter = new Vector2(
                targetTile.X * _worldGrid.TileSize + _worldGrid.TileSize / 2f,
                targetTile.Y * _worldGrid.TileSize + _worldGrid.TileSize / 2f
            );
            float distance = Vector2.Distance(playerPos.Value, tileCenter);
            
            if (distance > Constants.Harvesting.MaxMiningRange)
            {
                // Out of range - clear mining state
                if (playerEntity.Has<MiningState>())
                    playerEntity.Remove<MiningState>();
                _prevMouse = mouse;
                return;
            }

            // Get block at target
            var blockDef = _worldGrid.GetBlock(targetTile);
            if (blockDef == null || blockDef.Id == "air")
            {
                // No block to mine
                if (playerEntity.Has<MiningState>())
                    playerEntity.Remove<MiningState>();
                _prevMouse = mouse;
                return;
            }

            // Check if tool can mine this block
            if (!CanToolMineBlock(heldItem, blockDef))
            {
                if (playerEntity.Has<MiningState>())
                    playerEntity.Remove<MiningState>();
                _prevMouse = mouse;
                return;
            }

            if (mining)
            {
                // Get or create mining state
                if (!playerEntity.Has<MiningState>())
                {
                    playerEntity.Set(new MiningState
                    {
                        TargetTile = targetTile,
                        Progress = 0f,
                        TimeRequired = CalculateHarvestTime(heldItem, blockDef),
                        ToolItemId = heldItem.Id,
                        IsActive = true
                    });
                }

                ref var state = ref playerEntity.Get<MiningState>();
                
                // Check if target changed or tool changed
                if (state.TargetTile != targetTile || state.ToolItemId != heldItem.Id)
                {
                    // Reset progress for new target
                    state.TargetTile = targetTile;
                    state.Progress = 0f;
                    state.TimeRequired = CalculateHarvestTime(heldItem, blockDef);
                    state.ToolItemId = heldItem.Id;
                }

                // Accumulate progress
                state.Progress += dt;
                state.IsActive = true;

                // Check if mining complete
                if (state.Progress >= state.TimeRequired)
                {
                    // Destroy block
                    _worldGrid.RemoveBlock(targetTile);
                    
                    // Spawn dropped item
                    SpawnDroppedItem(tileCenter, blockDef);
                    
                    // Reset state (in case player keeps mining)
                    state.Progress = 0f;
                    
                    // Check if there's another block at this position (shouldn't be, but safety check)
                    var newBlock = _worldGrid.GetBlock(targetTile);
                    if (newBlock != null && newBlock.Id != "air")
                    {
                        state.TimeRequired = CalculateHarvestTime(heldItem, newBlock);
                    }
                }

                playerEntity.Set(state);
            }
            else
            {
                // Not mining - clear state
                if (playerEntity.Has<MiningState>())
                    playerEntity.Remove<MiningState>();
            }

            _prevMouse = mouse;
        }

        private ItemDef? GetHeldItem()
        {
            if (_player == null) return null;
            int idx = Math.Clamp(_player.HotbarIndex, 0, _player.Inventory.Slots.Length - 1);
            var slot = _player.Inventory.Slots[idx];
            if (slot.IsEmpty || string.IsNullOrEmpty(slot.ItemId)) return null;
            return _content.GetItem(slot.ItemId);
        }

        /// <summary>
        /// Check if a tool can mine a specific block.
        /// WHY: Terraria/Starbound require specific tool types for blocks.
        /// Pickaxe for stone/ore, axe for wood, hammer for furniture, etc.
        /// </summary>
        private bool CanToolMineBlock(ItemDef tool, BlockDef block)
        {
            // For now, pickaxe can mine anything solid
            // TODO: Add ToolRequired field to BlockDef for proper validation
            if (tool.ToolKind == ToolType.Pickaxe && block.Solid)
                return true;
            
            // Axe for wood-type blocks (future)
            // Hammer for backgrounds/furniture (future)
            
            return false;
        }

        /// <summary>
        /// Calculate time to harvest a block based on tool power and block hardness.
        /// Formula: BaseTime * (Hardness / ToolPower)
        /// </summary>
        private float CalculateHarvestTime(ItemDef tool, BlockDef block)
        {
            float toolPower = tool.Stats?.HarvestPower ?? 1f;
            float hardness = block.Hardness > 0 ? block.Hardness : 1f;
            
            // Prevent division by zero
            if (toolPower <= 0) toolPower = 1f;
            
            float time = Constants.Harvesting.BaseHarvestTime * (hardness / toolPower);
            return Math.Max(Constants.Harvesting.MinHarvestTime, time);
        }

        /// <summary>
        /// Spawn a dropped item entity at the block position.
        /// </summary>
        private void SpawnDroppedItem(Vector2 position, BlockDef block)
        {
            string? dropItemId = block.DropItemId;
            
            // If no specific drop, use block ID as item (common pattern)
            if (string.IsNullOrEmpty(dropItemId))
                dropItemId = block.Id;
            
            // Create entity with DroppedItem component
            var entity = _world.CreateEntity();
            
            // Spawn slightly above the block center with small random offset
            var rand = new Random();
            var spawnPos = position + new Vector2(
                (float)(rand.NextDouble() - 0.5) * 16f,
                -8f
            );
            
            entity.Set(new Position { Value = spawnPos });
            entity.Set(new Velocity { Value = new Vector2(
                (float)(rand.NextDouble() - 0.5) * 100f,
                -150f // Pop up slightly
            )});
            entity.Set(new DroppedItem
            {
                ItemId = dropItemId,
                Count = 1,
                PickupDelay = Constants.Items.PickupDelay,
                IsMagnetized = false,
                MagnetTargetId = -1
            });
            
            // Add a small collider for ground collision
            entity.Set(new Collider
            {
                Size = new Vector2(16f, 16f),
                Offset = Vector2.Zero
            });
        }

        private Vector2 ScreenToWorld(Point screen)
        {
            var origin = _camera.Origin;
            var zoom = Math.Max(0.0001f, _camera.Zoom);
            var pos = _camera.GetEffectivePosition();
            return (screen.ToVector2() - origin) / zoom + pos;
        }

        public void Dispose() { }
    }
}
