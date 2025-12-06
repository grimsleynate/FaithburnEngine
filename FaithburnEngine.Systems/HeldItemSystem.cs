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
using FaithburnEngine.Systems.HeldAnimations;

namespace FaithburnEngine.Systems
{
    public sealed class HeldItemSystem : ISystem<float>
    {
        private readonly DefaultEcs.World _world;
        private readonly ContentLoader _content;
        private readonly GraphicsDevice _graphics;
        private readonly ActiveHitboxSystem _hitboxes;
        private readonly AssetRegistry _assets;
        private readonly HeldAnimationRegistry _animators;
        private readonly FaithburnEngine.Rendering.Camera2D _camera;
        private readonly PlayerContext _player;
        private MouseState _prevMouse;

        public bool IsEnabled { get; set; } = true;

        public HeldItemSystem(DefaultEcs.World world, ContentLoader content, GraphicsDevice graphics, ActiveHitboxSystem hitboxes, AssetRegistry assets, HeldAnimationRegistry animators, FaithburnEngine.Rendering.Camera2D camera, PlayerContext player)
        {
            _world = world;
            _content = content;
            _graphics = graphics;
            _hitboxes = hitboxes;
            _assets = assets;
            _animators = animators;
            _camera = camera;
            _player = player;
            _prevMouse = Mouse.GetState();
        }

        public void Update(float dt)
        {
            var mouse = Mouse.GetState();
            bool leftEdge = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            var mouseWorld = _camera != null ? ScreenToWorld(new Point(mouse.X, mouse.Y)) : new Vector2(mouse.X, mouse.Y);

            foreach (var e in _world.GetEntities().With<Position>().With<Sprite>().AsEnumerable())
            {
                if (!e.IsAlive) continue;

                // Detect sprite flip and always cancel/restart held item animation
                ref var sprite = ref e.Get<Sprite>();
                if (!e.Has<FacingState>()) e.Set(new FacingState { LastEffects = sprite.Effects });
                ref var facing = ref e.Get<FacingState>();
                bool flipped = facing.LastEffects != sprite.Effects;
                if (flipped && e.Has<HeldItem>())
                {
                    // Always cancel current animation and restart in new direction
                    ref var hiFlip = ref e.Get<HeldItem>();
                    var itemFlip = _content.GetItem(hiFlip.ItemId);
                    e.Remove<HeldItem>();
                    if (itemFlip != null)
                    {
                        var next = CreateHeldItem(e, itemFlip);
                        next.AimTarget = mouseWorld;
                        if (!string.IsNullOrEmpty(itemFlip.HeldAnim) && _animators.TryGet(itemFlip.HeldAnim, out var anim))
                        {
                            anim.Begin(e, next);
                        }
                        e.Set(next);
                    }
                    facing.LastEffects = sprite.Effects;
                    e.Set(facing);
                }

                if (leftEdge && !e.Has<HeldItem>())
                {
                    var slotIndex = Math.Clamp(_player.HotbarIndex, 0, _player.Inventory.Slots.Length - 1);
                    var slot = _player.Inventory.Slots[slotIndex];
                    if (!slot.IsEmpty && !string.IsNullOrEmpty(slot.ItemId))
                    {
                        var item = _content.GetItem(slot.ItemId);
                        if (item != null)
                        {
                            var held = CreateHeldItem(e, item);
                            held.AimTarget = mouseWorld;
                            if (!string.IsNullOrEmpty(item.HeldAnim) && _animators.TryGet(item.HeldAnim, out var animator))
                            {
                                animator.Begin(e, held);
                            }
                            e.Set(held);
                        }
                    }
                }

                if (e.Has<HeldItem>())
                {
                    ref var hi = ref e.Get<HeldItem>();
                    hi.AimTarget = mouseWorld;
                    var item = _content.GetItem(hi.ItemId);
                    if (item != null && !string.IsNullOrEmpty(item.HeldAnim) && _animators.TryGet(item.HeldAnim, out var animator))
                    {
                        animator.Update(e, ref hi, dt);
                        e.Set(hi);
                    }

                    if (hi.TimeLeft <= 0f)
                    {
                        e.Remove<HeldItem>();
                        bool cont = item?.ContinuousUse == true ? mouse.LeftButton == ButtonState.Pressed : (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released);
                        if (cont && item != null)
                        {
                            var next = CreateHeldItem(e, item);
                            next.AimTarget = mouseWorld;
                            if (!string.IsNullOrEmpty(item.HeldAnim) && _animators.TryGet(item.HeldAnim, out var anim2)) anim2.Begin(e, next);
                            e.Set(next);
                        }
                    }
                    else
                    {
                        float t = 1f - (hi.TimeLeft / Math.Max(0.0001f, hi.Duration));
                        if (!hi.HitboxSpawned && t >= 0.5f)
                        {
                            var center = e.Get<Position>().Value + hi.Offset;
                            var rect = new Rectangle((int)center.X, (int)(center.Y - 20), 40, 20);
                            _hitboxes.SpawnHitbox(rect, 0.1f, hi.ItemId, e);
                            hi.HitboxSpawned = true;
                            e.Set(hi);
                        }
                    }
                }
            }

            _prevMouse = mouse;
        }

        private Vector2 ScreenToWorld(Point screen)
        {
            var origin = _camera.Origin;
            var zoom = Math.Max(0.0001f, _camera.Zoom);
            return (screen.ToVector2() - origin) / zoom + _camera.Position;
        }

        public void Dispose() { }

        private HeldItem CreateHeldItem(Entity e, FaithburnEngine.Content.Models.ItemDef item)
        {
            Texture2D tex;
            if (!string.IsNullOrEmpty(item.SpriteKey) && _assets.TryGetTexture(item.SpriteKey, out var keyTex)) tex = keyTex; else tex = new Texture2D(_graphics, 24, 24);
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
