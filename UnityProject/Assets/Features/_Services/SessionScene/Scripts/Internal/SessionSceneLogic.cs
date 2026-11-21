using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.SessionScene
{
    /// <summary>The session state machine. Plain C#: it never touches Unity objects.</summary>
    internal sealed class SessionSceneLogic : ISessionScene, ISessionLifecycle
    {
        private readonly List<ISessionParticipant> participants = new List<ISessionParticipant>();
        private readonly CancellationTokenSource loadCancellation = new CancellationTokenSource();
        private readonly UniTaskCompletionSource finished = new UniTaskCompletionSource();
        private readonly Action<Exception> reportError;

        private bool endRequested;
        private bool startingParticipants;

        internal SessionSceneLogic(Action<Exception> reportError)
        {
            this.reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
        }

        public SessionState State { get; private set; } = SessionState.Init;

        public event Action<SessionState> StateChanged;

        public void Join(ISessionParticipant participant)
        {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            if (State != SessionState.Init)
            {
                throw new InvalidOperationException(
                    $"ISessionScene.Join is allowed only during Install (state Init); the session is already {State}.");
            }

            participants.Add(participant);
        }

        public UniTask EndAsync()
        {
            if (!endRequested)
            {
                endRequested = true;
                if (State == SessionState.Init || (State == SessionState.Running && !startingParticipants))
                {
                    ShutDownAsync().Forget();
                }
                else if (State == SessionState.Loading)
                {
                    // BeginAsync notices endRequested once the loads have settled.
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
            var failure = await LoadAllAsync();
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

        /// <summary>Null when every participant loaded, or when loading stopped because the session was asked to end.</summary>
        private async UniTask<Exception> LoadAllAsync()
        {
            var loads = new List<UniTask<Exception>>(participants.Count);
            foreach (var participant in participants)
            {
                loads.Add(LoadOneAsync(participant));
            }

            var results = await UniTask.WhenAll(loads);

            Exception cancelled = null;
            foreach (var result in results)
            {
                if (result == null) continue;
                if (!(result.InnerException is OperationCanceledException)) return result;
                cancelled = cancelled ?? result;
            }

            // A participant that gives up on its own, with nobody asking, did not load.
            return endRequested ? null : cancelled;
        }

        private async UniTask<Exception> LoadOneAsync(ISessionParticipant participant)
        {
            try
            {
                await participant.LoadAsync(loadCancellation.Token);
                return null;
            }
            catch (OperationCanceledException e)
            {
                return Describe(participant, "load", e);
            }
            catch (Exception e)
            {
                loadCancellation.Cancel();
                return Describe(participant, "load", e);
            }
        }

        private async UniTask FailAsync(Exception failure)
        {
            reportError(failure);
            Set(SessionState.Failed);
            await DisposeAllAsync();
            finished.TrySetResult();
        }

        private async UniTask ShutDownAsync()
        {
            Set(SessionState.Disposing);
            await DisposeAllAsync();
            Set(SessionState.Disposed);
            finished.TrySetResult();
        }

        private async UniTask DisposeAllAsync()
        {
            for (var i = participants.Count - 1; i >= 0; i--)
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

        private void Set(SessionState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        private static Exception Describe(ISessionParticipant participant, string phase, Exception cause)
        {
            return new InvalidOperationException(
                $"Session participant {participant.GetType().Name} failed to {phase}: {cause.Message}", cause);
        }
    }
}
