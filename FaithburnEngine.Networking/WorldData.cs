using MessagePack;

namespace FaithburnEngine.Networking
{
    [MessagePackObject]
    public struct WorldData
    {
        [Key(0)] public int Tick;
        [Key(1)] public byte[] ChangedChunks; //compressed blobs 
    }
}
