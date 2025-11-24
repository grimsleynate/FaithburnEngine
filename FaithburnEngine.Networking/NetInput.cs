using MessagePack;
using Microsoft.Xna.Framework;

namespace FaithburnEngine.Networking
{
    [MessagePackObject]
    public struct NetInput
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Tick;
        [Key(2)] public byte Buttons;
        [Key(3)] public Vector2 Aim;
    }
}
