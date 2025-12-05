using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FaithburnEngine.Content.Models;
using FaithburnEngine.World;
using FaithburnEngine.Core;
using FaithburnEngine.Content.Models.Enums;
using DefaultEcs.System;
using FaithburnEngine.Rendering;
using FaithburnEngine.Components;
using FaithburnEngine.Core.Inventory;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FaithburnEngine.Systems
{
    public sealed class InteractionSystem : ISystem<float>
    {
        private readonly Content.ContentLoader _content;
        private readonly InventorySystem _inventorySystem;
        private readonly WorldGrid _world; // your world grid API
        private readonly Camera2D _camera;
        private readonly PlayerContext _player;

        // Track mining intents per player (single-player focused)
        private struct MiningIntent
        {
            public Point Tile;
            public float TimeLeft;
            public float TotalTime;
            public double LastStoppedAt; // time when player stopped mining (for retention)
            public bool IsActive;
            public string ToolId;
        }

        private readonly Dictionary<int, MiningIntent> _miningByPlayer = new();

        public bool IsEnabled { get; set; } = true;

        public InteractionSystem(Content.ContentLoader content, InventorySystem invSys, WorldGrid world, Camera2D camera, PlayerContext player)
        {
            _content = content;
            _inventorySystem = invSys;
            _world = world;
            _camera = camera;
            _player = player;
        }

        // Call from Update with mouse state and player entity info
        public void HandleMouse(PlayerContext player, MouseState mouse, bool leftClick, bool rightClick)
        {
            var worldPos = ScreenToWorld(mouse.Position, _camera);
            var tileCoord = _world.WorldToTileCoord(worldPos);
            // Only handle leftClick mining interactions here
            if (leftClick)
            {
                TryStartOrContinueMining(player, tileCoord);
            }
            else
            {
                // if player released mining input, mark stop time for retention
                StopMiningForPlayer(player);
            }
        }

        private void StopMiningForPlayer(PlayerContext player)
        {
            int pid = player.GetHashCode();
            if (_miningByPlayer.TryGetValue(pid, out var mi))
            {
                if (mi.IsActive)
                {
                    mi.IsActive = false;
                    mi.LastStoppedAt = Environment.TickCount / 1000.0;
                    _miningByPlayer[pid] = mi;
                }
            }
        }

        private void TryStartOrContinueMining(PlayerContext player, Point tileCoord)
        {
            int pid = player.GetHashCode();

            // Get player's feet tile coordinate
            var feet = player.Position; // PlayerContext stores Position
            var playerTile = _world.WorldToTileCoord(feet);

            // Chebyshev distance
            // WHY Chebyshev distance for click radius:
            // In tile-based worlds, we consider the maximum of dx/dy so diagonal tiles are within
            // the same effective radius. This matches intuitive mining reach.
            int dx = Math.Abs(tileCoord.X - playerTile.X);
            int dy = Math.Abs(tileCoord.Y - playerTile.Y);
            int cheb = Math.Max(dx, dy);
            if (cheb > Constants.Mining.MaxMiningDistanceTiles) return; // outside radius

            // Only mine solid blocks
            var block = _world.GetBlock(tileCoord);
            if (block == null || !block.Solid) return;

            // Get currently equipped hotbar item
            var inv = player.Inventory;
            int idx = Math.Clamp(player.HotbarIndex, 0, Math.Min(HotbarConstants.DisplayCount, inv.Slots.Length) - 1);
            var slot = inv.Slots[idx];
            if (slot.IsEmpty) return;

            var itemId = slot.ItemId ?? string.Empty;
            if (string.IsNullOrEmpty(itemId)) return;

            var itemDef = _content.GetItem(itemId);
            if (itemDef == null) return;
            if (itemDef.Type != FaithburnEngine.Content.Models.Enums.ItemType.Tool) return;
            if (itemDef.ToolKind != FaithburnEngine.Content.Models.Enums.ToolType.Pickaxe) return;

            // Compute time to break
            // WHY harvest rule and time:
            // Tools have `HarvestPower`; blocks have `Hardness`. The time is Hardness / Power
            // clamped to a sane minimum. This mirrors Terraria-like feel and allows per-item tuning.
            float harvestPower = Math.Max(0.0001f, itemDef.Stats.HarvestPower);
            float timeToBreak = block.Hardness / (harvestPower * Constants.Mining.GlobalHarvestSpeedModifier);
            timeToBreak = Math.Max(Constants.Mining.MinTimeToBreak, timeToBreak);

            // If there's an existing intent for this player
            if (!_miningByPlayer.TryGetValue(pid, out var mi))
            {
                mi = new MiningIntent { Tile = tileCoord, TimeLeft = timeToBreak, TotalTime = timeToBreak, IsActive = true, ToolId = itemDef.Id, LastStoppedAt = 0 };
                _miningByPlayer[pid] = mi;
                return;
            }

            // If mining the same tile, continue (reset if stopped within retention window)
            if (mi.Tile == tileCoord)
            {
                if (!mi.IsActive)
                {
                    double now = Environment.TickCount / 1000.0;
                    // WHY retention:
                    // If the player stops mining briefly and restarts within a short window,
                    // keep their progress. It reduces frustration from minor input stalls.
                    if (now - mi.LastStoppedAt <= Constants.Mining.ProgressRetentionSeconds)
                    {
                        // resume
                        mi.IsActive = true;
                        _miningByPlayer[pid] = mi;
                        return;
                    }
                    else
                    {
                        // retention expired — reset
                        mi.TimeLeft = timeToBreak;
                        mi.TotalTime = timeToBreak;
                        mi.IsActive = true;
                        mi.ToolId = itemDef.Id;
                        _miningByPlayer[pid] = mi;
                        return;
                    }
                }
                // already active — nothing to do
                return;
            }

            // WHY TryHarvest reuse:
            // The harvesting logic (tool validation + yields) is centralized in TryHarvest so both
            // direct mining and any future scripted/automated harvests share the same behavior.
            // If mining a different tile, start mining new tile (allow double-mining)
            mi.Tile = tileCoord;
            mi.TimeLeft = timeToBreak;
            mi.TotalTime = timeToBreak;
            mi.IsActive = true;
            mi.ToolId = itemDef.Id;
            _miningByPlayer[pid] = mi;
        }

        private void TryHarvest(Point tileCoord, ItemDef? toolDef, InventorySlot equippedSlot, PlayerContext player)
        {
            var block = _world.GetBlock(tileCoord);
            var rule = _content.HarvestRules.FirstOrDefault(r => r.TargetBlockId == block.Id);
            if (rule == null) return;

            // WHY tool validation:
            // Enforces intended gameplay: only certain tools can harvest certain blocks, with
            // optional power-based overrides to allow progression.
            var toolOk = CheckToolRequirement(toolDef, rule);
            if (!toolOk) return;

            // Remove block and give yields using existing logic
            _world.SetBlock(tileCoord, "air");
            var rng = new Random(); // WHY: inject RNG for determinism in tests in the future
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
            // update mining intents: decrement active timers and finish harvest when done
            var toComplete = new List<(int playerId, Point tile, MiningIntent intent)>();
            var now = Environment.TickCount / 1000.0;
            foreach (var kv in _miningByPlayer)
            {
                var pid = kv.Key;
                var mi = kv.Value;
                if (!mi.IsActive)
                {
                    // retention expiry
                    if (mi.LastStoppedAt > 0 && now - mi.LastStoppedAt > Constants.Mining.ProgressRetentionSeconds)
                    {
                        // reset intent
                        _miningByPlayer.Remove(pid);
                    }
                    continue;
                }

                // update timer
                mi.TimeLeft -= dt;
                _miningByPlayer[pid] = mi;

                if (mi.TimeLeft <= 0f)
                {
                    toComplete.Add((pid, mi.Tile, mi));
                }
            }

            // complete harvests after iteration to avoid mutation during enumeration
            foreach (var c in toComplete)
            {
                // resolve player and try harvest
                TryHarvest(c.tile, _content.GetItem(c.intent.ToolId), _player.Inventory.Slots[Math.Clamp(_player.HotbarIndex, 0, _player.Inventory.Slots.Length - 1)], _player);

                // remove intent
                _miningByPlayer.Remove(c.playerId);
            }
        }

        public void Dispose()
        {
            
        }
    }
}