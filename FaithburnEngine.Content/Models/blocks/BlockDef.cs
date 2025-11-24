using System.Collections.Generic;

namespace FaithburnEngine.Content.Models
{
    public sealed class BlockDef
    {
        public string Id { get; set; } = "";
        public bool Solid { get; set; }
        public int Hardness { get; set; }
        public string? DropItemId { get; set; }
        public string? SpriteRef { get; set; }
    }

}