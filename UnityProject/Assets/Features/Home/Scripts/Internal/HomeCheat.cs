#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Run;

namespace Game.Home
{
    /// <summary>States of feature-home/cheat, dev builds only.</summary>
    internal enum HomeCheatState
    {
        None,
        /// <summary>The start screen shows with a best score of 15, whatever is saved on the device.</summary>
        WithRecord,
    }

    /// <summary>Builds the cheat states. The forced best score goes through the normal path: the screen still reads IRunScore.</summary>
    internal static class HomeCheat
    {
        public static readonly int WithRecordBest = 15;

        public static IRunScore Apply(HomeCheatState state, IRunScore score)
        {
            switch (state)
            {
                case HomeCheatState.WithRecord:
                    return new ForcedBestRunScore(score, WithRecordBest);
                default:
                    return score;
            }
        }

        /// <summary>The run's score with only the best score replaced.</summary>
        private sealed class ForcedBestRunScore : IRunScore
        {
            private readonly IRunScore _inner;

            public ForcedBestRunScore(IRunScore inner, int best)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                Best = best;
            }

            public int Score => _inner.Score;
            public int Best { get; }
            public bool IsNewBest => _inner.IsNewBest;

            public event Action ScoreChanged
            {
                add => _inner.ScoreChanged += value;
                remove => _inner.ScoreChanged -= value;
            }
        }
    }
}
#endif
