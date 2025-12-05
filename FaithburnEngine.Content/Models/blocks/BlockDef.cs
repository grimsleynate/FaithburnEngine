using System.Collections.Generic;

namespace FaithburnEngine.Content.Models
{
    /// <summary>
    /// A destroyable block in the game.
    /// </summary>
    public sealed class BlockDef
    {
        /// <summary>
        /// Unique id for lookup from JSON and in-game references.
        /// </summary>
        public string Id { get; set; } = "";
        /// <summary>
        /// Is this block solid (non-passable). Solid tiles participate in collision and pathing.
        /// </summary>
        public bool Solid { get; set; }
        /// <summary>
        /// How hard is this block to break (higher = harder). Used in harvest time calculation.
        /// </summary>
        public float Hardness { get; set; }
        /// <summary>
        /// What item should this block drop when broken.
        /// </summary>
        public string? DropItemId { get; set; }
        /// <summary>
        /// Reference to the sprite used for this block.
        /// </summary>
        public string? SpriteRef { get; set; }
        /// <summary>
        /// Extensibility hook for mods and content packs. Allows arbitrary metadata
        /// (e.g., light levels, biome affinities, lore tags) without schema changes.
        /// </summary>
        public Dictionary<string, object>? CustomProperties { get; set; }
    }

}