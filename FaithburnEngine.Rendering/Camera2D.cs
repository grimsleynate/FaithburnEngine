using Microsoft.Xna.Framework;

namespace FaithburnEngine.Rendering
{
    public sealed class Camer2D
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; } = 1.0f;

        public Matrix GetViewMatrix() =>
            Matrix.CreateTranslation(new Vector3(-Position, 0)) *
            Matrix.CreateScale(Zoom, Zoom, 1f);
    }
}
