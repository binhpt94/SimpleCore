using System;
using Game.Save;

namespace Game.Run
{
    /// <summary>
    /// Score of a run and the best (feature-run/run-score/score-rules): starts at 0, +1 per cleared wave
    /// (once per wave number), settled once when the run ends; a higher settled score becomes the best and is handed
    /// to feature-save at once. The run never reads or writes the device itself. A failed write keeps the best in
    /// memory, marked unsaved, and is tried again when the game comes back to the screen and at the next settle.
    /// </summary>
    internal sealed class RunScore : IRunScore
    {
        private readonly ISavedRecord _saved;
        private int _lastCountedWave;
        private bool _settled = true;

        public RunScore(ISavedRecord saved)
        {
            _saved = saved ?? throw new ArgumentNullException(nameof(saved));
        }

        public int Score { get; private set; }

        public int Best { get; private set; }

        public bool IsNewBest { get; private set; }

        /// <summary>The best changed but feature-save has not stored it yet (the last write failed).</summary>
        internal bool IsBestUnsaved { get; private set; }

        public event Action ScoreChanged;

        /// <summary>Asks feature-save for the saved best; "none yet" or a negative number counts as 0.</summary>
        public void LoadBest()
        {
            Best = _saved.TryRead(out var saved) && saved > 0 ? saved : 0;
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

        /// <summary>
        /// The run ended: settle the score once and update the best. A new best is handed to feature-save now;
        /// a best still unsaved from before is tried again now too, even when this run set no new best.
        /// </summary>
        public void Settle()
        {
            if (_settled) return;
            _settled = true;

            if (Score > Best)
            {
                Best = Score;
                IsNewBest = true;
                IsBestUnsaved = true;
            }

            WriteIfUnsaved();
            ScoreChanged?.Invoke();
        }

        /// <summary>The game is on the screen again: one more try for a best that is still unsaved.</summary>
        public void RetryUnsavedBest() => WriteIfUnsaved();

        private void WriteIfUnsaved()
        {
            if (!IsBestUnsaved) return;
            // A failed write is not shown to the player; the best stays right in memory (record-keep-on-fail).
            IsBestUnsaved = !_saved.Write(Best);
        }
    }
}
