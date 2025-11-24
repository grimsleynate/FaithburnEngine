namespace FaithburnEngine.ModAPI
{
    public interface  IMod
    {
        //void Initialize(IRegistry registry);
        //void LoadContent(IContentBuilder builder);
        void RegisterHooks(IHookBus bus);
        void Shutdown();
    }
}
