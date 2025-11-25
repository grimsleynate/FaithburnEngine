using System.Text.Json.Serialization;

namespace FaithburnEngine.Content.Models
{
    public sealed class HarvestYield
    {
        public string ItemId { get; set; } = "";
        public int MinCount { get; set; } = 1;
        public int MaxCount { get; set; } = 1;
        public float Chance { get; set; } = 1.0f;
    }

    public sealed class HarvestRule
    {
        public string Id { get; set; } = "";
        public string TargetBlockId { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ToolType ToolRequired { get; set; } = ToolType.None; 
        public int MinHarvestPower { get; set; } = 0;

        public List<HarvestYield> Yields { get; set; } = new();
        public float HarvestTime { get; set; } = 1.0f;
    }
}