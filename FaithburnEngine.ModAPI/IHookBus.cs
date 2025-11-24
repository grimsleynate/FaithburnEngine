namespace FaithburnEngine.ModAPI
{
    public interface IHookBus
    {
        void Subscribe<T>(Action<T> handler);
        void Publish<T>(T evt);
    }
}
