namespace FaithburnEngine.Components
{
    /// <summary>
    /// Represents current input-based transient state for an entity (e.g., whether jump is held).
    /// Stored as a component so systems that need input info can read it deterministically.
    /// </summary>
    public struct InputState
    {
        public bool JumpHeld;
    }
}
