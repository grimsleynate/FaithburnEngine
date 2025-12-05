namespace FaithburnEngine.Components
{
    public enum AnimKind
    {
        Idle,
        Walk,
        Jump,
        Attack
    }

    public struct AnimationState
    {
        public AnimKind Kind;
        public int FrameIndex;
        public float Time;
    }
}
