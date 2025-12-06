using Microsoft.Xna.Framework;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// Component tracking active mining/harvesting progress on a specific tile.
    /// WHY per-entity: Allows multiple entities to mine simultaneously (multiplayer ready).
    /// Progress is tracked per-tile so switching targets resets progress (like Terraria).
    /// </summary>
    public struct MiningState
    {
        /// <summary>
        /// The tile coordinate currently being mined.
        /// </summary>
        public Point TargetTile;
        
        /// <summary>
        /// Accumulated mining progress in seconds.
        /// </summary>
        public float Progress;
        
        /// <summary>
        /// Total time required to break this block (calculated from tool power + block hardness).
        /// </summary>
        public float TimeRequired;
        
        /// <summary>
        /// The item ID of the tool being used (for validation).
        /// </summary>
        public string ToolItemId;
        
        /// <summary>
        /// Whether mining is currently active this frame.
        /// Reset each frame, set by input/interaction system.
        /// </summary>
        public bool IsActive;
    }
}
