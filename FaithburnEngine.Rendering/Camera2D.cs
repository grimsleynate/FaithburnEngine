using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Rendering
{
    public sealed class Camera2D
    {
        // World-space camera position (center of view)
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

        // Dead zone size in world pixels (box centered on camera). Small movements inside this box won't move the camera.
        public Vector2 DeadZoneSize { get; set; } = new Vector2(160f, 96f);

        // Lookahead multiplier applied to player velocity to offset camera in movement direction.
        // The final lookahead offset = playerVelocity * LookaheadMultiplier (then clamped by screen fraction)
        public float LookaheadMultiplier { get; set; } = 0.18f; // reduced from 0.35 for less aggressive lookahead

        // Maximum fraction of the viewport to use for lookahead (2/3 = 0.666...)
        public float LookaheadViewportFraction { get; set; } = 2f / 3f;

        // Smooth time used by SmoothDamp (approximate response time in seconds). Smaller => snappier.
        public float SmoothTime { get; set; } = 0.12f;

        // Internal velocity state used by SmoothDamp
        private Vector2 _smoothVelocity = Vector2.Zero;

        // Set origin to the current backbuffer center once you know viewport size
        public void UpdateOrigin(Viewport viewport)
        {
            Origin = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
        }

        // Build the view matrix used by SpriteBatch.Begin(transformMatrix: ...)
        public Matrix GetViewMatrix()
        {
            // Corrected order: translate world by -Position (so Position becomes origin), then rotate/scale around Origin, then translate back by Origin.
            // This ensures a world point at `Position` appears at screen `Origin` (center).
            return
                Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(new Vector3(Origin, 0f));
        }

        /// <summary>
        /// Update the camera to follow a target using a deadzone, lookahead and smooth damping.
        /// - targetPosition: world position to follow (center of target)
        /// - targetVelocity: world velocity of the target (pixels/sec)
        /// - dt: delta time in seconds
        /// </summary>
        public void UpdateFollow(Vector2 targetPosition, Vector2 targetVelocity, float dt)
        {
            if (dt <= 0f) return;

            // Compute lookahead from velocity (scales with speed)
            var lookahead = targetVelocity * LookaheadMultiplier;

            // Compute maximum allowed lookahead in world space based on viewport and zoom
            // Origin is half-viewport in screen pixels; convert to world-space by dividing by zoom
            var maxLook = (Origin / MathHelper.Max(0.0001f, Zoom)) * LookaheadViewportFraction;

            // Clamp lookahead so it doesn't exceed a fraction of the screen in any axis
            lookahead = new Vector2(
                MathHelper.Clamp(lookahead.X, -maxLook.X, maxLook.X),
                MathHelper.Clamp(lookahead.Y, -maxLook.Y, maxLook.Y)
            );

            // Desired camera center is target position plus lookahead
            var desired = targetPosition + lookahead;

            // Deadzone bounds (centered on current camera position)
            var half = DeadZoneSize * 0.5f;
            float left = Position.X - half.X;
            float right = Position.X + half.X;
            float top = Position.Y - half.Y;
            float bottom = Position.Y + half.Y;

            Vector2 moveTo = Position;

            // If desired point is outside deadzone, compute minimal translation so it lies on the edge
            if (desired.X < left) moveTo.X = desired.X + half.X;
            else if (desired.X > right) moveTo.X = desired.X - half.X;

            if (desired.Y < top) moveTo.Y = desired.Y + half.Y;
            else if (desired.Y > bottom) moveTo.Y = desired.Y - half.Y;

            // Smoothly move the camera from current Position to moveTo using SmoothDamp (critically damped-like)
            Position = SmoothDamp(Position, moveTo, ref _smoothVelocity, SmoothTime, dt);
        }

        // SmoothDamp ported for Vector2 (approximates a critically-damped spring, similar to Unity's implementation)
        private static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, float deltaTime)
        {
            smoothTime = MathHelper.Max(smoothTime, 0.0001f);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            Vector2 change = current - target;
            Vector2 temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            Vector2 output = target + (change + temp) * exp;

            return output;
        }
    }
}
