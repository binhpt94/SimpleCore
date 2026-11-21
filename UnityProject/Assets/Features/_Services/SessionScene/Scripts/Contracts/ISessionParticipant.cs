using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.SessionScene
{
    /// <summary>One member of a session. Each call happens at most once per session.</summary>
    public interface ISessionParticipant
    {
        /// <summary>
        /// Loads what is needed before play. All participants load at the same time.
        /// Throwing fails the whole session. <paramref name="cancellation"/> fires when another
        /// participant failed or the session was asked to end.
        /// </summary>
        UniTask LoadAsync(CancellationToken cancellation);

        /// <summary>Called after every participant loaded, in join order. Open views here.</summary>
        void Run();

        /// <summary>
        /// Lets go of events, views and files. Called in reverse join order, one at a time,
        /// also when the session failed or ended before <see cref="Run"/>.
        /// </summary>
        UniTask DisposeAsync();
    }
}
