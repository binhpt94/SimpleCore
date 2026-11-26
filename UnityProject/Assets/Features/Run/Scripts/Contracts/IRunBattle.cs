using System;

namespace Game.Run
{
    /// <summary>How the player left a battle, chosen on the lose popup.</summary>
    public enum RunLeaveChoice
    {
        /// <summary>Lose popup: Play again.</summary>
        PlayAgain = 0,

        /// <summary>Lose popup: Back to start.</summary>
        BackToStart = 1,
    }

    /// <summary>
    /// The run's battle as seen by the screen flow (feature-game-flow, from PROTOTYPE): the flow asks for a battle
    /// with a team, and hears when it is built and when the player left it. The run itself shows no main screen.
    /// </summary>
    public interface IRunBattle
    {
        /// <summary>
        /// Builds a fresh battle for <paramref name="team"/> and starts the run. Answers with exactly one of
        /// <see cref="BattleBuilt"/> or <see cref="BuildFailed"/>, possibly before this call returns. Ignored while a
        /// battle is being built or is running.
        /// </summary>
        void BuildBattle(RunTeam team);

        /// <summary>The build ran past the flow's time limit: throw away whatever it made; no answer follows.</summary>
        void CancelBuild();

        /// <summary>The battle asked for is ready: the battle screen may show.</summary>
        event Action BattleBuilt;

        /// <summary>The battle could not be built; the text says why, for the log. Nothing of it is left.</summary>
        event Action<string> BuildFailed;

        /// <summary>
        /// The player left the battle with <see cref="RunLeaveChoice"/>: the run has already ended and cleared the
        /// battle, so the flow can show the next screen at once.
        /// </summary>
        event Action<RunLeaveChoice> BattleLeft;
    }
}
