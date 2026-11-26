using System;

namespace Game.Run
{
    /// <summary>
    /// The run's steps and its battle signals. Screens and popups show themselves from <see cref="Step"/>
    /// and only report what the player pressed; battle features reset, stop and clear on the events below,
    /// and report a lost run or a cleared wave through <see cref="Lose"/> and <see cref="CountClearedWave"/>.
    /// The run depends on no other feature. Requests that do not fit the current step are ignored.
    /// </summary>
    public interface IRunFlow
    {
        RunStep Step { get; }

        /// <summary>Raised after <see cref="Step"/> changed, with the new step.</summary>
        event Action<RunStep> StepChanged;

        /// <summary>The team of the current or last run; empty before the first run.</summary>
        RunTeam Team { get; }

        /// <summary>A new run begins: every battle feature puts itself back to its start values for this team.</summary>
        event Action<RunTeam> RunStarted;

        /// <summary>Raised after every RunStarted handler ran: the battle may now start (the first wave).</summary>
        event Action BattleStarted;

        /// <summary>The castle was destroyed and the run ended: nothing on the battlefield acts any more.</summary>
        event Action BattleStopped;

        /// <summary>The player leaves the battle: remove everything the battle created.</summary>
        event Action BattleCleared;

        /// <summary>Start screen: the player pressed Play.</summary>
        void Play();

        /// <summary>Team select: the player pressed Enter battle with a full team.</summary>
        void EnterBattle(RunTeam team);

        /// <summary>Lose popup: the player chose Play again.</summary>
        void Replay();

        /// <summary>Lose popup: the player chose Back to start.</summary>
        void GoHome();

        /// <summary>
        /// In battle: the castle fell. The run is lost and the battle stops at once. Ignored while the run is
        /// already ending.
        /// </summary>
        void Lose();

        /// <summary>
        /// In battle: wave <paramref name="wave"/> was cleared. Adds one to the score, once per wave number,
        /// and only while the run is not over.
        /// </summary>
        void CountClearedWave(int wave);
    }
}
