using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
            set => _zoom = MathHelper.Clamp(value, 0.75f, 3.0f);
        }

        // Optional rotation (radians)
        public float Rotation { get; set; } = 0f;

        // Origin around which we zoom/rotate (screen point where Position maps to)
        public Vector2 Origin { get; set; } = Vector2.Zero;

        // Anchor fractions (0..1) used when computing origin from viewport size
        public float HorizontalAnchor { get; set; } = 1f / 3f;
        public float VerticalAnchor { get; set; } = 0.6f;

        // Dead zone size in world pixels (box centered on camera). Small movements inside this box won't move the camera.
        public Vector2 DeadZoneSize { get; set; } = new Vector2(160f, 96f);

        // Lookahead multiplier applied to player velocity to offset camera in movement direction.
        // The final lookahead offset = smoothedLookahead (which approaches targetVelocity * LookaheadMultiplier)
        public float LookaheadMultiplier { get; set; } = 0.18f; // reduced from 0.35 for less aggressive lookahead

        // Lookahead smoothing: smaller => quicker to follow, larger => slower pan when direction changes
        public float LookaheadSmoothTime { get; set; } = 0.08f;

        // Maximum fraction of the viewport to use for lookahead (2/3 = 0.666...)
        public float LookaheadViewportFraction { get; set; } = 2f / 3f;

        // Smooth time used by SmoothDamp (approximate response time in seconds). Smaller => snappier.
        public float SmoothTime { get; set; } = 0.12f;

        // WHY PixelSnap: When enabled, camera position is rounded to prevent sub-pixel
        // rendering which causes "texture bleeding" or "seam artifacts" between tiles.
        // This happens because floating-point camera positions cause the GPU to sample
        // from adjacent pixels in the texture atlas, showing hairline gaps between tiles.
        public bool PixelSnap { get; set; } = true;

        // Internal velocity state used by SmoothDamp for camera position
        private Vector2 _smoothVelocity = Vector2.Zero;

        // Internal lookahead state and velocity used to smoothly change lookahead when direction changes
        private Vector2 _smoothedLookahead = Vector2.Zero;
        private Vector2 _lookaheadVelocity = Vector2.Zero;

        // Set origin to the current backbuffer anchor once you know viewport size
        public void UpdateOrigin(Viewport viewport)
        {
            Origin = new Vector2(viewport.Width * HorizontalAnchor, viewport.Height * VerticalAnchor);
        }

        /// <summary>
        /// Get the effective camera position, optionally snapped to pixel boundaries.
        /// WHY: Sub-pixel camera positions cause texture bleeding at tile seams.
        /// </summary>
        public Vector2 GetEffectivePosition()
        {
            if (PixelSnap)
            {
                // Round to nearest pixel to prevent sub-pixel rendering artifacts
                return new Vector2(
                    MathF.Round(Position.X),
                    MathF.Round(Position.Y)
                );
            }
            return Position;
        }

        // Build the view matrix used by SpriteBatch.Begin(transformMatrix: ...)
        public Matrix GetViewMatrix()
        {
            // WHY use GetEffectivePosition: Snapping prevents tile seam artifacts
            var pos = GetEffectivePosition();
            
            return
                Matrix.CreateTranslation(new Vector3(-pos, 0f)) *
                Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(new Vector3(Origin, 0f));
        }

        /// <summary>
        /// Update the camera to follow a target using a deadzone, smoothed lookahead and smooth damping.
        /// - targetPosition: world position to follow (center of target)
        /// - targetVelocity: world velocity of the target (pixels/sec)
        /// - dt: delta time in seconds
        /// </summary>
        public void UpdateFollow(Vector2 targetPosition, Vector2 targetVelocity, float dt)
        {
            if (dt <= 0f) return;

            // Compute desired lookahead from velocity (scales with speed)
            var desiredLook = targetVelocity * LookaheadMultiplier;

            // Compute maximum allowed lookahead in world space based on viewport and zoom
            // Origin is an anchor in screen pixels; convert to world-space by dividing with zoom
            var maxLook = (Origin / MathHelper.Max(0.0001f, Zoom)) * LookaheadViewportFraction;

            // Clamp desired lookahead so it doesn't exceed a fraction of the screen in any axis
            desiredLook = new Vector2(
                MathHelper.Clamp(desiredLook.X, -maxLook.X, maxLook.X),
                MathHelper.Clamp(desiredLook.Y, -maxLook.Y, maxLook.Y)
            );

            // Smoothly move the current lookahead toward the desired lookahead (so camera pans when direction changes)
            _smoothedLookahead = SmoothDamp(_smoothedLookahead, desiredLook, ref _lookaheadVelocity, Math.Max(0.0001f, LookaheadSmoothTime), dt);

            // Desired camera center is target position plus smoothed lookahead
            var desired = targetPosition + _smoothedLookahead;

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
