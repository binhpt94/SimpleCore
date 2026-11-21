namespace Game.Services.SessionScene
{
    /// <summary>The scene object that turns Unity's Start and OnDestroy into session signals.</summary>
    internal interface ISessionDriver
    {
        void Drive(ISessionLifecycle session);
    }

    /// <summary>Creates the driver. Behind an interface so nothing else has to create Unity objects.</summary>
    internal interface ISessionDriverSource
    {
        ISessionDriver Create();
    }
}
