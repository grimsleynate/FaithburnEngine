using DefaultEcs;
using DefaultEcs.System;
using FaithburnEngine.Components;
using FaithburnEngine.World;
using FaithburnEngine.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FaithburnEngine.Systems
{
    // Simple pooled hitbox manager used during swings. Spawns transient AABB entities.
    public sealed class ActiveHitboxSystem : ISystem<float>
    {
        private readonly DefaultEcs.World _world;
        private readonly WorldGrid _worldGrid;
        private readonly ContentLoader _content;
        private readonly Queue<Entity> _pool = new();
        private readonly List<Entity> _active = new();

        public bool IsEnabled { get; set; } = true;

        public ActiveHitboxSystem(DefaultEcs.World world, WorldGrid worldGrid, ContentLoader content)
        {
            _world = world;
            _worldGrid = worldGrid;
            _content = content;
        }

        // Spawn a pooled hitbox entity with given rectangle and lifetime (seconds)
        // itemId: optional item that caused the hitbox (used for damage/harvest rules)
        // owner: optional owner entity to avoid friendly-fire
        public Entity SpawnHitbox(Rectangle rect, float life, string? itemId = null, Entity? owner = null)
        {
            Entity e;
            if (_pool.Count > 0)
            {
                e = _pool.Dequeue();
                if (!e.IsAlive) e = _world.CreateEntity();
            }
            else
            {
                e = _world.CreateEntity();
            }

            e.Set(new Hitbox { Rect = rect, TimeLeft = life });
            _active.Add(e);

            // Process immediate collisions (tiles + entities)
            ProcessHitbox(rect, itemId, owner);

            return e;
        }

        private void ProcessHitbox(Rectangle rect, string? itemId, Entity? owner)
        {
            // 1) Tile interactions: break tiles overlapping the hitbox if item's harvest power allows
            if (_worldGrid != null && !string.IsNullOrEmpty(itemId))
            {
                var def = _content.GetItem(itemId);
                if (def != null)
                {
                    int tileSize = _worldGrid.TileSize;
                    int left = rect.Left / tileSize;
                    int right = (rect.Right - 1) / tileSize;
                    int top = rect.Top / tileSize;
                    int bottom = (rect.Bottom - 1) / tileSize;

                    for (int ty = top; ty <= bottom; ty++)
                    {
                        for (int tx = left; tx <= right; tx++)
                        {
                            var coord = new Point(tx, ty);
                            var block = _worldGrid.GetBlock(coord);
                            if (block != null && block.Id != "air")
                            {
                                // Simple rule: if item has any harvest power, break the block
                                if (def.Stats?.HarvestPower > 0)
                                {
                                    _worldGrid.SetBlock(coord, "air");
                                }
                            }
                        }
                    }
                }
            }

            // 2) Entity interactions: find entities with Collider and Damageable and apply damage
            // Use DefaultEcs query. This is coarse but acceptable for PoC; optimize later with spatial buckets.
            foreach (var ent in _world.GetEntities().With<Components.Position>().With<Components.Collider>().AsEnumerable())
            {
                if (!ent.IsAlive) continue;
                if (owner.HasValue && ent == owner.Value) continue;

                ref var pos = ref ent.Get<Components.Position>();
                ref var col = ref ent.Get<Components.Collider>();

                // compute entity AABB (world space)
                float halfW = col.Size.X * 0.5f;
                float bottom = pos.Value.Y + col.Offset.Y;
                float top = bottom - col.Size.Y;
                float leftF = pos.Value.X - halfW + col.Offset.X;
                float rightF = pos.Value.X + halfW + col.Offset.X;

                var entRect = new Rectangle((int)leftF, (int)top, (int)(rightF - leftF), (int)(bottom - top));

                if (rect.Intersects(entRect))
                {
                    // apply damage if entity is damageable
                    if (ent.Has<Damageable>())
                    {
                        ref var dam = ref ent.Get<Damageable>();
                        float dmg = 0f;
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            var idef = _content.GetItem(itemId);
                            if (idef != null) dmg = idef.Stats?.Damage ?? 0f;
                            dmg *= idef?.HitboxDamageMultiplier ?? 1f;
                        }

                        dam.Health -= dmg;
                        ent.Set(dam);

                        if (dam.Health <= 0f)
                        {
                            // simple death: delete entity
                            ent.Dispose();
                        }
                    }
                }
            }
        }

        public void Update(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var e = _active[i];
                if (!e.IsAlive)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                ref var hb = ref e.Get<Hitbox>();
                hb.TimeLeft -= dt;
                if (hb.TimeLeft <= 0f)
                {
                    e.Remove<Hitbox>();
                    _active.RemoveAt(i);
                    _pool.Enqueue(e);
                }
                else
                {
                    e.Set(hb);
                }
            }
        }

        public void Dispose() { }
    }
}
