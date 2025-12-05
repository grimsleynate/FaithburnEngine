namespace FaithburnEngine.Core
{
    public static class Constants
    {
        // --- Player movement related constants ---
        public static class Player
        {
            public const float DefaultSpeed = 360f;
            public const float DefaultJumpVelocity = 960f;
        }

        // --- Mining related constants ---
        public static class Mining
        {
            // Max tile distance (Chebyshev) the player can mine from their feet position
            public const int MaxMiningDistanceTiles = 5;

            // Time (seconds) to retain partial mining progress after player stops mining that tile
            public const float ProgressRetentionSeconds = 2.0f;

            // Minimum time to break a block (seconds) to avoid division-by-zero or absurd speeds
            public const float MinTimeToBreak = 0.05f;

            // Optional global modifier applied to all harvest speeds (can be used for difficulty)
            public const float GlobalHarvestSpeedModifier = 1.0f;
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
