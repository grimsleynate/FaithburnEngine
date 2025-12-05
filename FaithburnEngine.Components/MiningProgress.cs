using Microsoft.Xna.Framework;

namespace FaithburnEngine.Components
{
    /// <summary>
    /// ECS component representing an active mining operation targeting a tile.
    /// </summary>
    public struct MiningProgress
    {
        public Point Tile;
        public string ToolId;
        public float TimeLeft;
        public float TotalTime;
    }
}
