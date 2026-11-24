using System;

namespace Game.Run
{
    /// <summary>
    /// Score of a run and the saved best (feature-run/run-score/score-rules): starts at 0, +1 per cleared wave
    /// (once per wave number), settled once when the run ends; a higher settled score becomes the best and is saved at once.
    /// </summary>
    internal sealed class RunScore : IRunScore
    {
        private readonly IBestScoreStore _store;
        private int _lastCountedWave;
        private bool _settled = true;

        public RunScore(IBestScoreStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int Score { get; private set; }

        public int Best { get; private set; }

        public bool IsNewBest { get; private set; }

        public event Action ScoreChanged;

        /// <summary>Reads the saved best; missing, negative or unreadable counts as 0.</summary>
        public void LoadBest()
        {
            int saved;
            try
            {
                saved = _store.Load();
            }
            catch (Exception)
            {
                saved = 0;
            }

            Best = saved < 0 ? 0 : saved;
            ScoreChanged?.Invoke();
        }

        /// <summary>A new run: score back to 0, no new best yet.</summary>
        public void StartRun()
        {
            Score = 0;
            IsNewBest = false;
            _lastCountedWave = 0;
            _settled = false;
            ScoreChanged?.Invoke();
        }

        /// <summary>Counts <paramref name="wave"/> as cleared, once per wave number and only before the score is settled.</summary>
        public void CountClearedWave(int wave)
        {
            if (_settled || wave <= _lastCountedWave) return;
            _lastCountedWave = wave;
            Score++;
            ScoreChanged?.Invoke();
        }

        /// <summary>The run ended: settle the score once and update the best.</summary>
        public void Settle()
        {
            if (_settled) return;
            _settled = true;

            if (Score > Best)
            {
                Best = Score;
                IsNewBest = true;
                try
                {
                    _store.Save(Best);
                }
                catch (Exception)
                {
                    // The best stays right for this session; the player is not told (score-rules).
                }
            }

            ScoreChanged?.Invoke();
        }
    }
}
