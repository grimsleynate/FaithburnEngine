using System;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FaithburnEngine.Components;
using FaithburnEngine.Content;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;

namespace FaithburnEngine.Systems
{
    // Equips and animates a held item (e.g., proto_pickaxe) on left click.
    // Also spawns a transient hitbox at swing peak.
    public sealed class HeldItemSystem : ISystem<float>
    {
        private readonly DefaultEcs.World _world;
        private readonly ContentLoader _content;
        private readonly GraphicsDevice _graphics;
        private readonly ActiveHitboxSystem _hitboxes;
        private readonly AssetRegistry _assets;
        private MouseState _prevMouse;

        public bool IsEnabled { get; set; } = true;

        public HeldItemSystem(DefaultEcs.World world, ContentLoader content, GraphicsDevice graphics, ActiveHitboxSystem hitboxes, AssetRegistry assets)
        {
            _world = world;
            _content = content;
            _graphics = graphics;
            _hitboxes = hitboxes;
            _assets = assets;
            _prevMouse = Mouse.GetState();
        }

        public void Update(float dt)
        {
            var mouse = Mouse.GetState();
            bool leftEdge = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

            foreach (var e in _world.GetEntities().With<Position>().With<Sprite>().AsEnumerable())
            {
                if (!e.IsAlive) continue;

                if (leftEdge && !e.Has<HeldItem>())
                {
                    var item = _content.GetItem("proto_pickaxe");
                    if (item != null)
                    {
                        var held = CreateHeldItem(e, item);
                        e.Set(held);
                    }
                }

                if (e.Has<HeldItem>())
                {
                    ref var hi = ref e.Get<HeldItem>();
                    hi.TimeLeft -= dt;
                    if (hi.TimeLeft <= 0f)
                    {
                        e.Remove<HeldItem>();
                        if (mouse.LeftButton == ButtonState.Pressed)
                        {
                            var item = _content.GetItem(hi.ItemId);
                            if (item != null)
                            {
                                var next = CreateHeldItem(e, item);
                                e.Set(next);
                            }
                        }
                    }
                    else
                    {
                        float t = 1f - (hi.TimeLeft / Math.Max(0.0001f, hi.Duration));
                        float swing = (float)Math.Sin(t * Math.PI) * 0.9f;
                        hi.Rotation = swing;
                        e.Set(hi);

                        if (!hi.HitboxSpawned && t >= 0.5f)
                        {
                            var pivotWorld = e.Get<Position>().Value + hi.Offset;
                            var rect = new Rectangle((int)(pivotWorld.X + 16), (int)(pivotWorld.Y - 20), 40, 20);
                            _hitboxes.SpawnHitbox(rect, 0.1f, hi.ItemId, e);
                            hi.HitboxSpawned = true;
                            e.Set(hi);
                        }
                    }
                }
            }

            _prevMouse = mouse;
        }

        public void Dispose() { }

        private HeldItem CreateHeldItem(Entity e, FaithburnEngine.Content.Models.ItemDef item)
        {
            Texture2D tex;
            if (!string.IsNullOrEmpty(item.SpriteKey) && _assets.TryGetTexture(item.SpriteKey, out var keyTex))
            {
                tex = keyTex;
            }
            else
            {
                // placeholder when missing
                tex = new Texture2D(_graphics, 24, 24);
                var pixels = new Color[24 * 24];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.Magenta;
                tex.SetData(pixels);
            }

            float duration = item.Stats?.Cooldown > 0 ? item.Stats.Cooldown : VisualConstants.DefaultHeldItemDuration;

            int pW = 0, pH = 0; float pScale = 1f; SpriteEffects pEffects = SpriteEffects.None; bool hasSprite = false; Vector2 colliderSize = Vector2.Zero;
            if (e.Has<Sprite>())
            {
                ref var ps = ref e.Get<Sprite>();
                if (ps.Texture != null) { hasSprite = true; pW = ps.Texture.Width; pH = ps.Texture.Height; pScale = ps.Scale <= 0f ? 1f : ps.Scale; }
                pEffects = ps.Effects;
            }
            if (e.Has<Collider>()) { ref var pc = ref e.Get<Collider>(); colliderSize = pc.Size; }

            var (offset, pivot) = HeldItemHelpers.ComputeVisuals(pW, pH, pScale, pEffects, hasSprite, colliderSize, null, null, null, null, tex?.Height ?? 0);

            return new HeldItem { ItemId = item.Id, Texture = tex, Offset = offset, Pivot = pivot, Scale = 1f, Rotation = 0f, Duration = duration, TimeLeft = duration };
        }
    }
}
