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
            // WHY 0.5f: Small inset prevents corner snagging while maintaining tight collision feel.
            public const float CollisionSkin = 0.5f;
            
            // WHY 2f: Shrinks horizontal collider width by this amount (1px each side) to allow
            // player to fit through gaps exactly matching their sprite width (64px player fits 64px gap).
            // Without this, floating point precision causes false collisions at tile boundaries.
            public const float HorizontalColliderShrink = 2f;
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

        // --- Item pickup and world items ---
        public static class Items
        {
            // WHY 96f: Approximately 3 tiles - matches Terraria's feel where items magnetize
            // from a reasonable distance without being too aggressive.
            public const float PickupMagnetRadius = 96f;
            
            // WHY 16f: Items must be very close to actually enter inventory, preventing
            // accidental pickup through walls.
            public const float PickupCollectRadius = 16f;
            
            // WHY 300f: Fast enough to feel responsive but slow enough to see the item
            // moving toward the player.
            public const float PickupMagnetSpeed = 300f;
            
            // WHY 1.5f: Items sit on ground briefly before becoming collectible,
            // preventing instant re-pickup when mining.
            public const float PickupDelay = 0.5f;
        }

        // --- Harvesting / Mining ---
        public static class Harvesting
        {
            // WHY this formula: Matches Terraria/Starbound feel where stronger tools
            // mine faster. BaseTime / (ToolPower / BlockHardness) gives intuitive scaling.
            // A tool with power 5 mining hardness 1 block = 1.0 / (5/1) = 0.2 seconds base.
            public const float BaseHarvestTime = 1.0f;
            
            // WHY 160f: Approximately 5 tiles - matches Terraria's mining reach.
            public const float MaxMiningRange = 160f;
            
            // WHY: Minimum time to break any block, prevents instant-mining exploits.
            public const float MinHarvestTime = 0.1f;
        }
    }
}
