using System.Text.Json.Serialization;
using FaithburnEngine.Content.Models.Enums;

namespace FaithburnEngine.Content.Models
{
    /// <summary>
    /// An items base stats
    /// </summary>
    public sealed class ItemStats
    {
        //The base damage of this weapon.
        public float Damage { get; set; }
        //How strong this tool is for harvesting.
        public float HarvestPower { get; set; }
        //The cooldown time between uses (in seconds).
        public float Cooldown { get; set; }
    }

    /// <summary>
    /// Defines an item in the game.
    /// </summary>
    public sealed class ItemDef
    {
        public string Id { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        //The type of item. Decoration, tool, block, etc.
        public ItemType Type { get; set; } = ItemType.None;
        //If this is a tool, what kind is it?
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ToolType ToolKind { get; set; } = ToolType.None;
        //Maximum number of items that can be stacked in one inventory slot.
        public int StackMax { get; set; } = 9999;
        //This item's stats.
        public ItemStats Stats { get; set; } = new();
        //This item's sprite reference.
        public string? SpriteRef { get; set; }

        // Visual metadata (optional)
        // If true, flip the item's native artwork horizontally so it faces the same
        // direction as the player. This avoids hardcoding item ids in renderer.
        public bool FlipToFacePlayer { get; set; } = false;

        // Optional pivot in texture pixels (if null, pivot will be computed as bottom-left)
        public int? PivotX { get; set; }
        public int? PivotY { get; set; }

        // Optional hand offset override in pixels relative to player's feet (if null, computed)
        public int? HandOffsetX { get; set; }
        public int? HandOffsetY { get; set; }

        // Hitbox metadata for active use (optional)
        // Hitbox size in pixels
        public int? HitboxWidth { get; set; }
        public int? HitboxHeight { get; set; }
        // Offset from the pivot/hand point (in world pixels) where hitbox should be placed
        public int? HitboxOffsetX { get; set; }
        public int? HitboxOffsetY { get; set; }
        // How long the hitbox should exist (seconds)
        public float? HitboxLifetime { get; set; }
        // Optional damage multiplier applied to item base damage
        public float? HitboxDamageMultiplier { get; set; }
    }
}