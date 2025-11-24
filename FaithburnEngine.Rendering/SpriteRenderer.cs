using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FaithburnEngine.Components;
using System.Collections.Generic;

namespace FaithburnEngine.Rendering
{
    public sealed class SpriteRenderer
    {
        private readonly DefaultEcs.World _world;
        private readonly SpriteBatch _spriteBatch;

        public SpriteRenderer(DefaultEcs.World world, SpriteBatch spriteBatch)
        {
            _world = world;
            _spriteBatch = spriteBatch;
        }

        // Call this from Game.Draw
        public void Draw()
        {
            // Single batched begin/end for the whole frame
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.Identity);

            // Iterate entities that have Position + Sprite (adjust type name to your component)
            foreach (var e in _world.GetEntities().With<Position>().With<Sprite>().AsEnumerable())
            {
                ref var pos = ref e.Get<Position>();
                ref var sprite = ref e.Get<Sprite>();

                if (sprite.Texture == null) continue;

                var origin = sprite.Origin;
                var tint = sprite.Tint == default ? Color.White : sprite.Tint;
                var scale = sprite.Scale <= 0f ? 1f : sprite.Scale;

                // Draw using position as the center if origin is set to center
                _spriteBatch.Draw(sprite.Texture, pos.Value, null, tint, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }

        // Optional small helper to draw a debug rectangle (1x1 white tex must be created elsewhere)
        public void DrawDebugRect(Texture2D whitePixel, Rectangle dest, Color color)
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(whitePixel, dest, color);
            _spriteBatch.End();
        }
    }
}
