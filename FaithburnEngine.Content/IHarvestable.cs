using Microsoft.Xna.Framework;

namespace FaithburnEngine.Content
{
    /// <summary>
    /// Optional block behavior hook invoked when a block is harvested.
    /// Keep signature lightweight to avoid cross-project references.
    /// </summary>
    public interface IHarvestable
    {
        void OnHarvested(Point tile);
    }
}
