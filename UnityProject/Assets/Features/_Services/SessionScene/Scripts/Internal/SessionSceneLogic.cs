using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Services.SessionScene
{
    /// <summary>The session state machine. Plain C#: it never touches Unity objects.</summary>
    internal sealed class SessionSceneLogic : ISessionScene, ISessionLoadProgress, ISessionLifecycle
    {
        private readonly List<ISessionParticipant> participants = new List<ISessionParticipant>();
        private readonly CancellationTokenSource loadCancellation = new CancellationTokenSource();
        private readonly UniTaskCompletionSource finished = new UniTaskCompletionSource();
        private readonly Action<Exception> reportError;
        private readonly ILoadTimer timer;
        private readonly TimeSpan loadLimit;

        /// <summary>Null outside dev builds: nothing is counted after the dispose step.</summary>
        private readonly IParticipantUsage usage;

        private bool endRequested;
        private bool startingParticipants;

        /// <summary>How many participants, from the first, must be disposed when the session ends or fails.</summary>
        private int toDispose;

        internal SessionSceneLogic(
            Action<Exception> reportError, ILoadTimer timer, TimeSpan loadLimit, IParticipantUsage usage = null)
        {
            this.usage = usage;
            this.reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            if (loadLimit <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(loadLimit), loadLimit, "The load time limit must be positive.");
            }

            this.loadLimit = loadLimit;
        }

        public SessionState State { get; private set; } = SessionState.Init;

        public event Action<SessionState> StateChanged;

        public int ParticipantCount => participants.Count;

        public int LoadedCount { get; private set; }

        public string FailedParticipant { get; private set; }

        public event Action LoadProgressChanged;

        public void Join(ISessionParticipant participant)
        {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            if (State != SessionState.Init)
            {
                throw new InvalidOperationException(
                    $"ISessionScene.Join is allowed only during Install (state Init); the session is already {State}.");
            }

            if (participants.Contains(participant))
            {
                throw new InvalidOperationException(
                    $"Session participant {participant.GetType().Name} joined twice; each participant joins once.");
            }

            participants.Add(participant);
        }

        public UniTask EndAsync()
        {
            if (!endRequested)
            {
                endRequested = true;
                if (State == SessionState.Init)
                {
                    toDispose = participants.Count;
                    ShutDownAsync().Forget();
                }
                else if (State == SessionState.Running && !startingParticipants)
                {
                    ShutDownAsync().Forget();
                }
                else if (State == SessionState.Loading)
                {
                    // BeginAsync notices endRequested once the participant now loading has settled.
                    loadCancellation.Cancel();
                }
            }

            return finished.Task;
        }

        void ISessionLifecycle.Begin()
        {
            BeginAsync().Forget();
        }

        void ISessionLifecycle.RequestEnd()
        {
            EndAsync().Forget();
        }

        /// <summary>Init to Loading to Running. Does nothing when the session was already asked to end.</summary>
        internal async UniTask BeginAsync()
        {
            if (endRequested) return;
            if (State != SessionState.Init)
            {
                throw new InvalidOperationException($"The session can begin only once, from Init; it is {State}.");
            }

            Set(SessionState.Loading);
            var failure = await LoadInTurnAsync();
            if (failure != null)
            {
                await FailAsync(failure);
                return;
            }

            if (endRequested)
            {
                await ShutDownAsync();
                return;
            }

            // Set before the state change so an EndAsync from a listener or a participant waits for the loop.
            startingParticipants = true;
            toDispose = participants.Count;
            Set(SessionState.Running);
            foreach (var participant in participants)
            {
                try
                {
                    participant.Run();
                }
                catch (Exception e)
                {
                    failure = Describe(participant, "run", e);
                    break;
                }
            }

            startingParticipants = false;

            if (failure != null) await FailAsync(failure);
            else if (endRequested) await ShutDownAsync();
        }

        /// <summary>
        /// Loads one participant at a time, in join order. Null when every participant loaded, or when loading
        /// stopped because the session was asked to end.
        /// </summary>
        private async UniTask<Exception> LoadInTurnAsync()
        {
            foreach (var participant in participants)
            {
                if (endRequested) return null;

                // The one now loading is disposed too: it may hold part of what it loaded.
                toDispose++;
                var failure = await LoadOneAsync(participant);
                if (endRequested) return null;

                if (failure != null)
                {
                    FailedParticipant = FeatureNameOf(participant.GetType());
                    LoadProgressChanged?.Invoke();
                    return failure;
                }

                LoadedCount++;
                LoadProgressChanged?.Invoke();
            }

            return null;
        }

        /// <summary>Null when the participant loaded within the limit; otherwise why it did not.</summary>
        private async UniTask<Exception> LoadOneAsync(ISessionParticipant participant)
        {
            using (var load = CancellationTokenSource.CreateLinkedTokenSource(loadCancellation.Token))
            using (var limit = new CancellationTokenSource())
            {
                // The limit token is cancelled only after the load won, so the timer never wins by being cancelled.
                var (loadedFirst, failure) = await UniTask.WhenAny(
                    CatchAsync(participant, load.Token),
                    timer.WaitAsync(loadLimit, limit.Token));

                if (loadedFirst)
                {
                    limit.Cancel();
                    return failure;
                }

                // Over the limit: tell the participant to stop, and do not wait for it any longer.
                load.Cancel();
                return Describe(participant, "load",
                    new TimeoutException($"loading took longer than {loadLimit.TotalSeconds:0.#} seconds"));
            }
        }

        private static async UniTask<Exception> CatchAsync(ISessionParticipant participant, CancellationToken token)
        {
            try
            {
                await participant.LoadAsync(token);
                return null;
            }
            catch (Exception e)
            {
                return Describe(participant, "load", e);
            }
        }

        private async UniTask FailAsync(Exception failure)
        {
            reportError(failure);
            Set(SessionState.Failed);
            await DisposeReachedAsync();
            ReportLeftovers();
            finished.TrySetResult();
        }

        private async UniTask ShutDownAsync()
        {
            Set(SessionState.Disposing);
            await DisposeReachedAsync();
            ReportLeftovers();
            Set(SessionState.Disposed);
            finished.TrySetResult();
        }

        /// <summary>Disposes in reverse join order, one at a time, only participants the session got to.</summary>
        private async UniTask DisposeReachedAsync()
        {
            for (var i = toDispose - 1; i >= 0; i--)
            {
                try
                {
                    await participants[i].DisposeAsync();
                }
                catch (Exception e)
                {
                    // One participant failing to clean up must not keep the others from doing so.
                    reportError(Describe(participants[i], "dispose", e));
                }
            }
        }

        /// <summary>
        /// After everyone reached was disposed: each participant that still holds views or running jobs is
        /// reported by feature name with what it left. Nothing is cleaned up on its behalf.
        /// </summary>
        private void ReportLeftovers()
        {
            if (usage == null) return;

            for (var i = toDispose - 1; i >= 0; i--)
            {
                var participant = participants[i];
                ParticipantUsage left;
                try
                {
                    left = usage.CountFor(participant);
                }
                catch (Exception e)
                {
                    reportError(Describe(participant, "be counted after the dispose step", e));
                    continue;
                }

                if (left.IsEmpty) continue;

                var type = participant.GetType();
                reportError(new InvalidOperationException(
                    $"Session participant {FeatureNameOf(type)} ({type.Name}) left {left.Views} view(s) and " +
                    $"{left.RunningJobs} running job(s) after the dispose step."));
            }
        }

        private void Set(SessionState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        /// <summary>
        /// Short feature name taken from the participant type's namespace: "Game.Wallet" gives "Wallet",
        /// "Game.Services.Popup" gives "Popup". Falls back to the type name outside that layout.
        /// </summary>
        internal static string FeatureNameOf(Type type)
        {
            return FeatureNameOf(type.Namespace, type.Name);
        }

        internal static string FeatureNameOf(string typeNamespace, string typeName)
        {
            if (string.IsNullOrEmpty(typeNamespace)) return typeName;

            var parts = typeNamespace.Split('.');
            var index = 0;
            if (parts[index] == "Game") index++;
            if (index < parts.Length && parts[index] == "Services") index++;
            return index < parts.Length && index > 0 ? parts[index] : typeName;
        }

        private static Exception Describe(ISessionParticipant participant, string phase, Exception cause)
        {
            return new InvalidOperationException(
                $"Session participant {FeatureNameOf(participant.GetType())} ({participant.GetType().Name}) " +
                $"failed to {phase}: {cause.Message}", cause);
        }
    }
}
