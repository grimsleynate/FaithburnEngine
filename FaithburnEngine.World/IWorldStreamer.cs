using System.Drawing;

namespace FaithburnEngine.World
{
    public interface IWorldStreamer
    {
        void Request(Point chunkCoord);
        bool TryDequeueReady(out Chunk chunk);
    }
}
