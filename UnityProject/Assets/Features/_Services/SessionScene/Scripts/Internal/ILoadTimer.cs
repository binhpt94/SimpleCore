using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.SessionScene
{
    /// <summary>Waits out one participant's load time limit. Behind an interface so tests need not wait for real.</summary>
    internal interface ILoadTimer
    {
        /// <summary>Completes once <paramref name="limit"/> of play time has passed; throws when cancelled.</summary>
        UniTask WaitAsync(TimeSpan limit, CancellationToken cancellation);
    }
}
