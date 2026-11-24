using System;
using Game.Core;
using Game.Run;

namespace Game.PopupLose
{
    /// <summary>
    /// States of node feature-popup-lose/cheat, picked on the installer asset. The type stays in every build so the
    /// asset serializes the same; release builds ignore it (the code below is dev only).
    /// </summary>
    internal enum LoseCheatState
    {
        None,

        /// <summary>Lose popup open, the run just set a new best: score 16, best 16, new-best flag on.</summary>
        NewRecord,
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>The forced values of one cheat state, read by the popup exactly like the run's real score.</summary>
    internal sealed class FixedRunScore : IRunScore
    {
        internal FixedRunScore(int score, int best, bool isNewBest)
        {
            Score = score;
            Best = best;
            IsNewBest = isNewBest;
        }

        public int Score { get; }
        public int Best { get; }
        public bool IsNewBest { get; }

        public event Action ScoreChanged { add { } remove { } }

        internal static FixedRunScore For(LoseCheatState state) => state switch
        {
            LoseCheatState.NewRecord => new FixedRunScore(16, 16, true),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "no forced values for this cheat state"),
        };
    }

    /// <summary>Opens the lose popup as soon as the session runs, without playing until the castle falls.</summary>
    internal sealed class LoseCheatParticipant : SessionParticipantBase
    {
        private readonly LosePopupParticipant _popup;

        internal LoseCheatParticipant(LosePopupParticipant popup)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
        }

        protected override void OnRun() => _popup.Open();
    }
#endif
}
