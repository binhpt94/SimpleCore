using System;
using Cysharp.Threading.Tasks;

namespace Game.Core
{
    /// <summary>The life of the current scene's session: Init, Loading, Running, Disposing, Disposed, or Failed.</summary>
    public interface ISessionScene
    {
        SessionState State { get; }

        /// <summary>Raised after <see cref="State"/> changed, with the new state.</summary>
        event Action<SessionState> StateChanged;

        /// <summary>
        /// Adds a participant. Allowed only while the state is Init, that is, during Install;
        /// throws afterwards.
        /// </summary>
        void Join(ISessionParticipant participant);

        /// <summary>
        /// Asks the session to end. Completes once the session is Disposed, or once a Failed session
        /// finished cleaning up. Safe to call more than once and from inside a participant.
        /// </summary>
        UniTask EndAsync();
    }

    /// <summary>Where one scene's session is in its life. Disposed and Failed are final.</summary>
    public enum SessionState
    {
        /// <summary>Installers are running; features may still join.</summary>
        Init,

        /// <summary>Every participant is loading what it needs before play.</summary>
        Loading,

        /// <summary>Every participant loaded and was told to run; the player can act.</summary>
        Running,

        /// <summary>Participants are letting go of what they hold.</summary>
        Disposing,

        /// <summary>The session ended normally.</summary>
        Disposed,

        /// <summary>A participant could not load or run; the session never became playable.</summary>
        Failed
    }
}
