using System;
using Cysharp.Threading.Tasks;

namespace Game.Services.SessionScene
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
}
