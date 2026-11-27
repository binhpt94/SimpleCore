using System;
using Game.Core;

namespace Game.CheatPanel
{
    /// <summary>
    /// Holds the cheat overlay for one scene session: creates it when the session runs, destroys it when the session ends.
    /// Joined only when cheat_enabled is on.
    /// </summary>
    internal sealed class CheatOverlayParticipant : SessionParticipantBase
    {
        private readonly ICheatOverlaySpawner _spawner;
        private readonly CheatPanelConfig _config;

        public CheatOverlayParticipant(ICheatOverlaySpawner spawner, CheatPanelConfig config)
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
        }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var overlay = _spawner.Spawn(_config);
            if (overlay == null) throw new InvalidOperationException("CheatPanel: the overlay spawner returned no object.");
            var held = TrackView(overlay);
            WhenDisposed(() =>
            {
                held.Dispose();
                _spawner.Despawn(overlay);
            });
        }
    }
}
