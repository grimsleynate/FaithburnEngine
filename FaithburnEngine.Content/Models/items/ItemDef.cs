using System.Collections.Generic;

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
        public string Type { get; set; } = ""; // material, consumable, tool, weapon, block
        public int StackMax { get; set; } = 9999;
        public ItemStats Stats { get; set; } = new();
        public string? SpriteRef { get; set; }
    }
}