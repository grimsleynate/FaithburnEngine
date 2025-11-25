using System.Text.Json.Serialization;
using FaithburnEngine.Content.Models.Enums;

namespace FaithburnEngine.Content.Models
{
    public sealed class ItemStats
    {
        public int Damage { get; set; }
        public int HarvestPower { get; set; }
        public float Cooldown { get; set; }
    }

    public sealed class ItemDef
    {
        public string Id { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ItemType Type { get; set; } = ItemType.None;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ToolType ToolKind { get; set; } = ToolType.None;
        public int StackMax { get; set; } = 9999;
        public ItemStats Stats { get; set; } = new();
        public string? SpriteRef { get; set; }
    }
}