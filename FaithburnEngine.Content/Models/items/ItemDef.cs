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
        public int Damage { get; set; }
        //How strong this tool is for harvesting.
        public int HarvestPower { get; set; }
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
    }
}