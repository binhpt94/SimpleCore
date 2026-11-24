using System;

namespace Game.Run
{
    /// <summary>The score of the current run (waves cleared) and the saved best score.</summary>
    public interface IRunScore
    {
        /// <summary>Waves cleared in the current or last run.</summary>
        int Score { get; }

        /// <summary>The best score ever saved on this device.</summary>
        int Best { get; }

        /// <summary>True when the last finished run set a new best score.</summary>
        bool IsNewBest { get; }

        /// <summary>Raised after Score, Best or IsNewBest changed.</summary>
        event Action ScoreChanged;
    }
}
