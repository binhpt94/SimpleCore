using Game.Core;

namespace Game.SceneFlow
{
    /// <summary>
    /// The Loading scene's only participant (design: feature-scene-flow/scene-list/boot-loading). As soon as the
    /// scene runs it asks for the switch to the Game scene; the switch shows the loading screen as its cover, so the
    /// player sees one screen from opening the game until the Game scene's first screen is ready.
    /// <para>
    /// When the Loading scene was itself reached by a switch (a restart from the cheat panel), that switch ends when
    /// this scene runs: the gate opens, and the request for the Game scene goes once the switch is over, as when the
    /// app was just opened.
    /// </para>
    /// </summary>
    internal sealed class LoadingToGameFlow : SessionParticipantBase
    {
        private readonly ISessionScene session;
        private readonly SceneSwitcher switcher;
        private readonly SceneGate gate;
        private readonly string gameScene;

        internal LoadingToGameFlow(ISessionScene session, SceneSwitcher switcher, SceneGate gate, string gameScene)
        {
            this.session = session;
            this.switcher = switcher;
            this.gate = gate;
            this.gameScene = gameScene;
        }

        protected override void OnRun()
        {
            // The switch owns the cover and outlives this scene; nothing to undo here when the session ends.
            // The cover is the scene-transition cover, so it is not tracked as a view, and the switch's waits are
            // the switch's, not this session's, so they are not tracked as jobs.
            if (!switcher.IsSwitching)
            {
                gate.Open();
                switcher.Request(gameScene, session.EndAsync);
                return;
            }

            switcher.SwitchingChanged += OnSwitchingChanged;
            WhenDisposed(() => switcher.SwitchingChanged -= OnSwitchingChanged);
            gate.Open();
        }

        private void OnSwitchingChanged()
        {
            if (switcher.IsSwitching) return;
            switcher.SwitchingChanged -= OnSwitchingChanged;
            switcher.Request(gameScene, session.EndAsync);
        }
    }
}
