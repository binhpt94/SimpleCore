namespace Game.Services.SessionScene
{
    /// <summary>What the scene tells the session: all installers are done, and the scene is going away.</summary>
    internal interface ISessionLifecycle
    {
        void Begin();

        void RequestEnd();
    }
}
