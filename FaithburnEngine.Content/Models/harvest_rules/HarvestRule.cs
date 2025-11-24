using System.Collections.Generic;

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
        public string ToolRequired { get; set; } = "any"; // "any", "pick", "axe"
        public int MinHarvestPower { get; set; } = 0;

        public Dictionary<string, int> Yields { get; set; } = new();
        public float HarvestTime { get; set; } = 1.0f; // in seconds
    }
}