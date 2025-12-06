namespace FaithburnEngine.Core
{
    public static class VisualConstants
    {
        // Pixels from sprite half-width to edge where hand sits
        public const float HandEdgePadding = 4f;
        // Vertical adjustment from halfway up the sprite; positive moves toward ground
        public const float HandYAdjust = 8f;
        // How many pixels closer to the body to shift the hand (toward center)
        public const float HandBodyOffset = 32f;

        // Default duration fallback for held item if item stat is missing
        public const float DefaultHeldItemDuration = 0.3f;
    }
}
