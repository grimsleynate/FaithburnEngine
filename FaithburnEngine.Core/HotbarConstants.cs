namespace FaithburnEngine.Core
{
    public static class HotbarConstants
    {
        // Number of hotbar slots shown (typically 10: 1..9,0)
        public const int DisplayCount = 10;

        // Size in pixels of each hotbar slot (visual size)
        public const int SlotSize = 96;

        // Padding in pixels between slots
        public const int Padding = 12;

        // Vertical offset from bottom of screen (kept for compatibility if needed)
        public const int BottomOffset = 10;

        // Left and top padding when placing the hotbar in the upper-left
        public const int LeftPadding = 12;
        public const int TopPadding = 12;

        // Font scale applied when drawing slot labels (1 = native font size)
        public const float FontScale = 1.25f;

        // Pixel padding from the slot's horizontal (left) for labels (used for fallback positioning)
        public const int FontPaddingX = 12;

        // Pixel padding from the slot's vertical (top) for labels — increased for more top spacing
        public const int FontPaddingY = 12;
    }
}