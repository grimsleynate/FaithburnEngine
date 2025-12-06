namespace FaithburnEngine.Core
{
    public static class Constants
    {
        // --- Player movement related constants ---
        public static class Player
        {
            // Physics
            public const float Gravity = 2200f; // pixels/s^2
            public const float FallMultiplier = 2.2f;
            public const float LowJumpMultiplier = 2.0f;
            public const float MaxFallSpeed = 1600f;

            // Movement tuning
            public const float MaxSpeed = 420f;
            public const float AccelGround = 6000f;
            public const float AccelAir = 2800f;
            public const float DecelGround = 8000f;
            public const float DecelAir = 3500f;

            // Jump tuning
            public const float JumpVelocity = 960f;
            public const float CoyoteTime = 0.12f; // seconds
            public const float JumpBufferTime = 0.12f; // seconds

            // Wall jump / unstick
            public const float WallUnstickImpulse = 160f; // horizontal impulse applied when jumping off wall

            // Collision/snap tuning
            public const float CollisionSkin = 0.5f; // pixels applied inward to AABB checks to reduce corner snagging
        }

        // --- Spritesheet / Character rendering ---
        public static class Spritesheet
        {
            // Default layout for NPC/character column spritesheets used by artists (pixels)
            public const int DefaultFrameWidth = 64;
            public const int DefaultFrameHeight = 96;
            public const int DefaultLeftPadding = 16;
            public const int DefaultTopPadding = 16;
            public const int DefaultBottomPadding = 4;
            // Vertical stride between frames
            public const int DefaultFrameStrideY = DefaultTopPadding + DefaultFrameHeight + DefaultBottomPadding; // 116

            // Walk animation frame interval (seconds) when toggling between walking frames
            public const float WalkFrameInterval = 0.18f;

            // Threshold to consider an entity 'walking' (pixels/sec)
            public const float WalkVelocityThreshold = 4f;
        }

        // ... add other mechanic groups here (Combat, Camera, Rendering, etc.) ...
    }
}
