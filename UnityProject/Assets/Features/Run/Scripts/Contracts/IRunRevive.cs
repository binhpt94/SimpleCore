using System;

namespace Game.Run
{
    /// <summary>
    /// The run's revive flow, once per run: the castle's first fall freezes the battle (<see cref="RunStep.ReviveWait"/>),
    /// the revive popup shows itself from <see cref="RunStep.ReviveOffer"/> and reports the player's choice here, and
    /// the castle stands up on <see cref="ReviveStarted"/>. Same object as <see cref="IRunFlow"/>. Requests that do
    /// not fit the current step are ignored.
    /// </summary>
    public interface IRunRevive
    {
        /// <summary>
        /// Raised when the run enters <see cref="RunStep.Reviving"/>: the castle stands up again, then calls
        /// <see cref="ReviveShown"/>. The battle was not stopped, so nothing resets.
        /// </summary>
        event Action ReviveStarted;

        /// <summary>
        /// True in <see cref="RunStep.Lost"/> when the run was lost because the revive ad was not watched
        /// (the lose popup says so); false otherwise.
        /// </summary>
        bool LostAfterFailedRevive { get; }

        /// <summary>Revive popup: the ad was watched. Goes to <see cref="RunStep.Reviving"/>.</summary>
        void Revive();

        /// <summary>Revive popup: the player chose End run. The run is lost now, without a further wait.</summary>
        void DeclineRevive();

        /// <summary>Revive popup: the ad was not watched. Like <see cref="DeclineRevive"/>, and sets <see cref="LostAfterFailedRevive"/>.</summary>
        void FailRevive();

        /// <summary>Castle: it stood up again after <see cref="ReviveStarted"/>. The battle runs again.</summary>
        void ReviveShown();
    }
}
