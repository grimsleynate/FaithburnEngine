using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Rendering
{
    public sealed class Camera2D
    {
        // World-space camera position (top-left or center depending on origin)
        public Vector2 Position { get; set; } = Vector2.Zero;

        // Zoom factor: 1 = 100%, >1 zoom in, <1 zoom out
        private float _zoom = 1f;
        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, 0.1f, 10f);
        }

        // Optional rotation (radians)
        public float Rotation { get; set; } = 0f;

        // Origin around which we zoom/rotate (usually screen center)
        public Vector2 Origin { get; set; } = Vector2.Zero;

        // Set origin to the current backbuffer center once you know viewport size
        public void UpdateOrigin(Viewport viewport)
        {
            Origin = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
        }

        // Build the view matrix used by SpriteBatch.Begin(transformMatrix: ...)
        public Matrix GetViewMatrix()
        {
            // Translate world by -Position, then rotate/scale around Origin, then translate back
            return
                Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                Matrix.CreateTranslation(new Vector3(-Origin, 0f)) *
                Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(new Vector3(Origin, 0f));
        }

    }
}
