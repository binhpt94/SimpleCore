using Game.Core;

namespace Game.SceneFlow
{
    /// <summary>
    /// The Loading scene's only participant (design: feature-scene-flow/scene-list/boot-loading). As soon as the
    /// scene runs it asks for the switch to the Game scene; the switch shows the loading screen as its cover, so the
    /// player sees one screen from opening the game until the Game scene's first screen is ready.
    /// </summary>
    internal sealed class LoadingToGameFlow : SessionParticipantBase
    {
        private readonly ISessionScene session;
        private readonly SceneSwitcher switcher;
        private readonly string gameScene;

        internal LoadingToGameFlow(ISessionScene session, SceneSwitcher switcher, string gameScene)
        {
            this.session = session;
            this.switcher = switcher;
            this.gameScene = gameScene;
        }

        protected override void OnRun()
        {
            // The switch owns the cover and outlives this scene; nothing to undo here when the session ends.
            switcher.Request(gameScene, session.EndAsync);
        }
    }
}
