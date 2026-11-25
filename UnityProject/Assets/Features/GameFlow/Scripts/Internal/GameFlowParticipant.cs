using System;
using Game.Core;

namespace Game.GameFlow
{
    /// <summary>
    /// Joins the Game scene's session. The session runs its participants once everything has loaded,
    /// which is the "Game scene ready" moment: the router then shows the start screen.
    /// </summary>
    internal sealed class GameFlowParticipant : SessionParticipantBase
    {
        private readonly ScreenRouter _router;

        public GameFlowParticipant(ScreenRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        protected override void OnRun() => _router.SceneReady();
    }
}
