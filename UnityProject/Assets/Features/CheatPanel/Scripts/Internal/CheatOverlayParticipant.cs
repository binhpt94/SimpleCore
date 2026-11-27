using System;
using Game.Core;
using Game.SceneFlow;

namespace Game.CheatPanel
{
    /// <summary>
    /// Holds the cheat overlay for one scene session: creates it when the session runs, binds its view to the panel
    /// rules, destroys it when the session ends (so a scene change closes the panel). Joined only when cheat_enabled is on.
    /// </summary>
    internal sealed class CheatOverlayParticipant : SessionParticipantBase
    {
        internal const string MissingSceneSwitchMessage =
            "CheatPanel: ISceneSwitch is missing. SceneFlow registers it in every scene during boot.";

        private readonly ICheatOverlaySpawner _spawner;
        private readonly CheatPanelConfig _config;
        private readonly ICheatStates _states;
        private readonly Func<ISceneSwitch> _findSwitch;
        private readonly bool _openAtStart;

        public CheatOverlayParticipant(ICheatOverlaySpawner spawner, CheatPanelConfig config, ICheatStates states,
            Func<ISceneSwitch> findSwitch, bool openAtStart)
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _states = states ?? throw new ArgumentNullException(nameof(states));
            _findSwitch = findSwitch ?? throw new ArgumentNullException(nameof(findSwitch));
            _openAtStart = openAtStart;
        }

        /// <summary>The panel rules of this session; null until the overlay with its view is created.</summary>
        internal CheatPanelPresenter Presenter { get; private set; }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var overlay = _spawner.Spawn(_config);
            if (overlay == null) throw new InvalidOperationException("CheatPanel: the overlay spawner returned no object.");
            var held = TrackView(overlay);
            WhenDisposed(() =>
            {
                Presenter?.Dispose();
                Presenter = null;
                held.Dispose();
                _spawner.Despawn(overlay);
            });

            // An overlay without a view shows nothing and takes no tap, so it needs no rules.
            var display = overlay.GetComponent<ICheatPanelDisplay>();
            if (display == null) return;
            var sceneSwitch = _findSwitch() ?? throw new InvalidOperationException(MissingSceneSwitchMessage);
            Presenter = new CheatPanelPresenter(_config, _states, sceneSwitch, _openAtStart);
            Presenter.Attach(display);
        }
    }
}
