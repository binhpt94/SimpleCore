using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Core
{
    /// <summary>
    /// Life of one feature inside a scene session. Subclasses load in <see cref="OnLoadAsync"/>, open views and
    /// subscribe in <see cref="OnRun"/>, and register each "undo" with <see cref="WhenDisposed"/>; undos run in
    /// reverse order when the session ends, then <see cref="OnDisposeAsync"/> runs. No game rules here.
    /// </summary>
    public abstract class SessionParticipantBase : ISessionParticipant
    {
        private readonly List<Action> _undos = new List<Action>();
        private bool _disposed;

        public UniTask LoadAsync(CancellationToken cancellation) => OnLoadAsync(cancellation);

        public void Run()
        {
            if (_disposed) return;
            OnRun();
        }

        public async UniTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            for (var i = _undos.Count - 1; i >= 0; i--) _undos[i]();
            _undos.Clear();
            await OnDisposeAsync();
        }

        protected virtual UniTask OnLoadAsync(CancellationToken cancellation) => UniTask.CompletedTask;

        protected abstract void OnRun();

        protected virtual UniTask OnDisposeAsync() => UniTask.CompletedTask;

        /// <summary>Runs <paramref name="undo"/> when the session ends (unsubscribe, destroy a view, close a popup...).</summary>
        protected void WhenDisposed(Action undo)
        {
            if (undo == null) throw new ArgumentNullException(nameof(undo));
            _undos.Add(undo);
        }
    }
}
