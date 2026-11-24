using System;

namespace Game.Services.SessionScene
{
    /// <summary>
    /// How far the current scene's Loading step is: how many participants finished loading out of how many joined.
    /// Read by whoever shows a loading bar. Counts only; it never tells which participant is which.
    /// </summary>
    public interface ISessionLoadProgress
    {
        /// <summary>Participants that joined this session.</summary>
        int ParticipantCount { get; }

        /// <summary>Participants whose LoadAsync completed. Equals ParticipantCount once loading is over.</summary>
        int LoadedCount { get; }

        /// <summary>
        /// Short name of the feature whose participant failed or timed out while loading (for example "Wallet"),
        /// for the loading screen's error line; null while nothing failed.
        /// </summary>
        string FailedParticipant { get; }

        /// <summary>Raised after LoadedCount or FailedParticipant changed.</summary>
        event Action LoadProgressChanged;
    }
}
