using System.Text.Json.Serialization;
using FaithburnEngine.Content.Models.Enums;
using System.Collections.Generic;

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

        // Mod-friendly keys
        public string? SpriteKey { get; set; }
        public string? HeldAnim { get; set; } // e.g., "swing", "thrust", "cast", "hold"
        public bool ContinuousUse { get; set; } = true; // if false, require re-click between uses

        /// <summary>
        /// Extensibility hook for mods and content packs. Allows arbitrary metadata
        /// (e.g., lore tags, elemental types, custom behaviors) without schema changes.
        /// </summary>
        public Dictionary<string, object>? CustomProperties { get; set; }
    }
}