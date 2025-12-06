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
        private readonly IWorldGrid _worldGrid;
        private readonly ContentLoader _content;
        private readonly Queue<Entity> _pool = new();
        private readonly List<Entity> _active = new();

        public bool IsEnabled { get; set; } = true;

        public ActiveHitboxSystem(DefaultEcs.World world, IWorldGrid worldGrid, ContentLoader content)
        {
            _world = world;
            _worldGrid = worldGrid;
            _content = content;
        }

        // Spawn a pooled hitbox entity with given rectangle and lifetime (seconds)
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
            return e;
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
