using System.Collections.Generic;

namespace FaithburnEngine.Content.Models
{
    /// <summary>
    /// A destroyable block in the game.
    /// </summary>
    public sealed class BlockDef
    {
        public string Id { get; set; } = "";
        //Is this block solid (non-passable)
        public bool Solid { get; set; }
        //How hard is this block to break (higher = harder)
        public float Hardness { get; set; }
        //What item should this block drop when broken
        public string? DropItemId { get; set; }
        //Reference to the sprite used for this block
        public string? SpriteRef { get; set; }
    }

}