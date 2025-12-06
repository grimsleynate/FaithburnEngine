using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using FaithburnEngine.Components;
using FaithburnEngine.Content;
using FaithburnEngine.Core;
using FaithburnEngine.World;

namespace FaithburnEngine.Systems
{
    /// <summary>
    /// Handles pickup of dropped items by the player.
    /// 
    /// WHY ISystem instead of AEntitySetSystem: 
    /// DefaultEcs entity sets are invalidated when entities are created or destroyed mid-frame.
    /// HarvestingSystem creates DroppedItem entities, and we destroy them when collected.
    /// Using ISystem with fresh queries and deferred disposal avoids corruption.
    /// 
    /// WHY this design (Terraria/Starbound-like):
    /// - Items have a brief delay before becoming collectible (prevents instant re-pickup)
    /// - Items within magnet radius start moving toward the player
    /// - Items within collect radius are added to inventory and destroyed
    /// - Magnetized items ignore physics and home in on player
    /// - If inventory is full, items bounce off and remain in world
    /// </summary>
    public sealed class ItemPickupSystem : ISystem<float>
    {
        private readonly DefaultEcs.World _world;
        private readonly IWorldGrid _worldGrid;
        private readonly ContentLoader _content;
        private readonly PlayerContext _player;
        
        // Reusable lists to avoid allocations each frame
        private readonly List<Entity> _entitiesToProcess = new();
        private readonly List<Entity> _entitiesToDestroy = new();

        public bool IsEnabled { get; set; } = true;

        public ItemPickupSystem(DefaultEcs.World world, IWorldGrid worldGrid, ContentLoader content, PlayerContext player)
        {
            _world = world;
            _worldGrid = worldGrid;
            _content = content;
            _player = player;
        }

        public void Update(float dt)
        {
            // Clear lists from previous frame
            _entitiesToProcess.Clear();
            _entitiesToDestroy.Clear();
            
            // Find player entity
            Vector2 playerPos = Vector2.Zero;
            bool foundPlayer = false;
            
            foreach (var e in _world.GetEntities().With<Position>().With<Velocity>().With<Collider>().AsEnumerable())
            {
                // Skip entities that are dropped items
                if (e.Has<DroppedItem>()) continue;
                
                playerPos = e.Get<Position>().Value;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer) return;

            // Collect all dropped item entities BEFORE processing
            foreach (var e in _world.GetEntities().With<Position>().With<DroppedItem>().AsEnumerable())
            {
                _entitiesToProcess.Add(e);
            }

            // Process each entity
            foreach (var entity in _entitiesToProcess)
            {
                if (!entity.IsAlive) continue;

                ref var pos = ref entity.Get<Position>();
                ref var drop = ref entity.Get<DroppedItem>();

                // Tick down pickup delay
                if (drop.PickupDelay > 0f)
                {
                    drop.PickupDelay -= dt;
                    entity.Set(drop);
                    
                    // Apply physics while waiting (item falls to ground)
                    ApplyPhysics(entity, dt);
                    continue;
                }

                // Calculate distance to player
                float distance = Vector2.Distance(pos.Value, playerPos);

                // Check if within collect radius - add to inventory
                if (distance <= Constants.Items.PickupCollectRadius)
                {
                    if (TryAddToInventory(drop.ItemId, drop.Count))
                    {
                        // Successfully collected - queue for destruction
                        _entitiesToDestroy.Add(entity);
                        continue;
                    }
                    else
                    {
                        // Inventory full - stop magnetizing and apply physics
                        drop.IsMagnetized = false;
                        entity.Set(drop);
                        ApplyPhysics(entity, dt);
                        continue;
                    }
                }

                // Check if within magnet radius - start homing
                if (distance <= Constants.Items.PickupMagnetRadius)
                {
                    drop.IsMagnetized = true;
                    entity.Set(drop);
                    
                    // WHY no physics when magnetized: Once an item starts moving toward the player,
                    // it becomes "incorporeal" - ignoring world collision. This prevents items from
                    // getting stuck on terrain while trying to reach the player (Terraria-like behavior).
                    // The item flies directly toward the player regardless of walls or ground.
                    
                    // Move directly toward player (no collision checks)
                    Vector2 direction = playerPos - pos.Value;
                    if (direction.LengthSquared() > 0.01f)
                    {
                        direction.Normalize();
                        pos.Value += direction * Constants.Items.PickupMagnetSpeed * dt;
                        entity.Set(pos);
                    }
                    
                    // Skip physics entirely while magnetized
                    continue;
                }
                else
                {
                    // Outside magnet range - apply normal physics (gravity + ground collision)
                    drop.IsMagnetized = false;
                    entity.Set(drop);
                    ApplyPhysics(entity, dt);
                }
            }

            // Destroy collected entities AFTER iteration is complete
            foreach (var entity in _entitiesToDestroy)
            {
                if (entity.IsAlive)
                {
                    entity.Dispose();
                }
            }
        }

        /// <summary>
        /// Apply simple physics to dropped items (gravity + ground collision).
        /// WHY only when not magnetized: Items should fall and rest on ground normally,
        /// but once they start flying toward the player, they ignore all collision.
        /// </summary>
        private void ApplyPhysics(Entity entity, float dt)
        {
            if (!entity.Has<Velocity>()) return;

            ref var pos = ref entity.Get<Position>();
            ref var vel = ref entity.Get<Velocity>();

            // Apply gravity (reduced compared to player for floaty feel)
            vel.Value.Y += Constants.Player.Gravity * 0.5f * dt;
            vel.Value.Y = Math.Min(vel.Value.Y, Constants.Player.MaxFallSpeed * 0.5f);

            // Apply velocity
            Vector2 proposed = pos.Value + vel.Value * dt;

            // Ground collision - snap to surface
            if (_worldGrid != null)
            {
                // Check tile directly below item center
                var tileBelow = _worldGrid.WorldToTileCoord(proposed + new Vector2(0, 8));
                if (_worldGrid.IsSolidTile(tileBelow))
                {
                    // Snap to top of tile
                    proposed.Y = tileBelow.Y * _worldGrid.TileSize - 1f;
                    vel.Value.Y = 0f;
                    
                    // Apply ground friction
                    vel.Value.X *= 0.85f;
                    if (Math.Abs(vel.Value.X) < 1f) vel.Value.X = 0f;
                }
                
                // Simple horizontal collision to prevent items from going through walls
                // Check left/right tiles at item position
                var tileLeft = _worldGrid.WorldToTileCoord(proposed + new Vector2(-8, 0));
                var tileRight = _worldGrid.WorldToTileCoord(proposed + new Vector2(8, 0));
                
                if (_worldGrid.IsSolidTile(tileLeft) && vel.Value.X < 0)
                {
                    proposed.X = (tileLeft.X + 1) * _worldGrid.TileSize + 8f;
                    vel.Value.X = 0f;
                }
                else if (_worldGrid.IsSolidTile(tileRight) && vel.Value.X > 0)
                {
                    proposed.X = tileRight.X * _worldGrid.TileSize - 8f;
                    vel.Value.X = 0f;
                }
            }

            pos.Value = proposed;
            entity.Set(pos);
            entity.Set(vel);
        }

        /// <summary>
        /// Try to add items to player inventory.
        /// Returns true if all items were added, false if inventory is full.
        /// </summary>
        private bool TryAddToInventory(string itemId, int count)
        {
            if (_player == null) return false;
            
            var itemDef = _content.GetItem(itemId);
            int stackMax = itemDef?.StackMax ?? 9999;
            int remaining = count;

            // First pass: try to stack with existing items
            for (int i = 0; i < _player.Inventory.Slots.Length && remaining > 0; i++)
            {
                var slot = _player.Inventory.Slots[i];
                if (slot.ItemId == itemId && slot.Count < stackMax)
                {
                    int added = slot.Add(remaining, stackMax);
                    remaining -= added;
                }
            }

            // Second pass: find empty slots
            for (int i = 0; i < _player.Inventory.Slots.Length && remaining > 0; i++)
            {
                var slot = _player.Inventory.Slots[i];
                if (slot.IsEmpty)
                {
                    int toAdd = Math.Min(remaining, stackMax);
                    slot.Set(itemId, toAdd);
                    remaining -= toAdd;
                }
            }

            return remaining == 0;
        }

        public void Dispose() { }
    }
}
