using System.Text.Json.Serialization;

namespace FaithburnEngine.Content.Models
{
    /// <summary>
    /// Represents a possible yield from harvesting a block.
    /// </summary>
    public sealed class HarvestYield
    {
        public string ItemId { get; set; } = "";
        //Minimum possible count of this item yielded
        public int MinCount { get; set; } = 1;
        //Maximum possible count of this item yielded
        public int MaxCount { get; set; } = 1;
        //Chance (0.0 to 1.0) of this yield occurring
        public float Chance { get; set; } = 1.0f;
    }

    /// <summary>
    /// Represents a rule that defines how a specific block can be harvested, including the required tool, minimum
    /// power, and resulting yields.
    /// </summary>
    public sealed class HarvestRule
    {
        public string Id { get; set; } = "";
        // The BlockId that this harvest rule applies to
        public string TargetBlockId { get; set; } = "";
        // The type of tool required to harvest the block
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ToolType ToolRequired { get; set; } = ToolType.None;
        // The minimum harvest power required to successfully harvest the block
        public int MinHarvestPower { get; set; } = 0;
        // The list of possible yields from harvesting the block
        public List<HarvestYield> Yields { get; set; } = new();
        // The time in seconds it takes to harvest the block
        // TODO: assimilate this and the hardness stat on blocks. The hardness of a block + the strength of the harvesting tool should dictate harvest time.
        public float HarvestTime { get; set; } = 1.0f;
    }
}