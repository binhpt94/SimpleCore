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
    /// <para>
    /// For the session's clean-up check, subclasses report what they hold with <see cref="TrackView"/> and
    /// <see cref="TrackJob"/> and release the handle when they let go of it; <see cref="ISessionParticipantUsage"/>
    /// reads the counts. The scene-transition cover is not tracked. Counting runs only in the Editor and
    /// development builds; in a release build the handles do nothing and the counts are 0.
    /// </para>
    /// </summary>
    public abstract class SessionParticipantBase : ISessionParticipant, ISessionParticipantUsage
    {
        private readonly List<Action> _undos = new List<Action>();
        private bool _disposed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly List<ViewHandle> _views = new List<ViewHandle>();
        private int _runningJobs;
#endif

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

        /// <summary>
        /// Override with true when <see cref="OnRun"/> opens views (and tracks them with <see cref="TrackView"/>):
        /// the session then checks right after the run step that this participant holds at least one view.
        /// Leave false when the participant has no view or opens its views only later.
        /// </summary>
        protected virtual bool OpensViewsOnRun => false;

        bool ISessionParticipantUsage.OpensViewsOnRun => OpensViewsOnRun;

        /// <summary>Runs <paramref name="undo"/> when the session ends (unsubscribe, destroy a view, close a popup...).</summary>
        protected void WhenDisposed(Action undo)
        {
            if (undo == null) throw new ArgumentNullException(nameof(undo));
            _undos.Add(undo);
        }

        /// <summary>
        /// Counts <paramref name="view"/> as held until the returned handle is disposed or the object is destroyed.
        /// Do not track the scene-transition cover.
        /// </summary>
        protected IDisposable TrackView(UnityEngine.Object view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var handle = new ViewHandle(view);
            _views.Add(handle);
            return handle;
#else
            return Released.Instance;
#endif
        }

        /// <summary>Counts one running job (timer, pending wait, per-frame loop) until the returned handle is disposed.</summary>
        protected IDisposable TrackJob()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _runningJobs++;
            return new JobHandle(this);
#else
            return Released.Instance;
#endif
        }

        int ISessionParticipantUsage.HeldViews
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _views.RemoveAll(v => !v.IsHeld);
                return _views.Count;
#else
                return 0;
#endif
            }
        }

        int ISessionParticipantUsage.RunningJobs
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return _runningJobs;
#else
                return 0;
#endif
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private sealed class ViewHandle : IDisposable
        {
            private readonly UnityEngine.Object _view;
            private bool _released;

            public ViewHandle(UnityEngine.Object view) => _view = view;

            // Unity's == reports a destroyed object as null.
            public bool IsHeld => !_released && _view != null;

            public void Dispose() => _released = true;
        }

        private sealed class JobHandle : IDisposable
        {
            private SessionParticipantBase _owner;

            public JobHandle(SessionParticipantBase owner) => _owner = owner;

            public void Dispose()
            {
                if (_owner == null) return;
                _owner._runningJobs--;
                _owner = null;
            }
        }
#else
        private sealed class Released : IDisposable
        {
            public static readonly Released Instance = new Released();

            public void Dispose() { }
        }
#endif
    }
}
