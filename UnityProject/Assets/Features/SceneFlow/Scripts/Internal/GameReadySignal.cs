using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.SceneFlow
{
    /// <summary>
    /// The Game scene's side: makes its scene the active one before any feature opens a view, opens
    /// the gate once every other feature of the scene is running, or marks it failed when the
    /// session fails. Install it last so its Run comes after all the others.
    /// Also tells the scene's features, through <see cref="IGameSceneReady"/>, when that gate opens: once per load.
    /// </summary>
    internal sealed class GameReadySignal : SessionParticipantBase, IGameSceneReady
    {
        private readonly ISessionScene session;
        private readonly SceneGate gate;
        private readonly ISceneLoader scenes;
        private readonly string gameScene;

        internal GameReadySignal(ISessionScene session, SceneGate gate, ISceneLoader scenes, string gameScene)
        {
            this.session = session;
            this.gate = gate;
            this.scenes = scenes;
            this.gameScene = gameScene;
            session.StateChanged += OnStateChanged;
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellation)
        {
            // Unity puts newly created objects in the active scene. While the Loading scene is still
            // the active one, views opened in Run would land there and vanish when it is removed.
            // Every LoadAsync is called before any Run, so this is early enough.
            scenes.SetActive(gameScene);
            return UniTask.CompletedTask;
        }

        public bool IsReady { get; private set; }

        public event Action Ready;

        protected override void OnRun()
        {
            gate.Open();
            if (IsReady) return;
            IsReady = true;
            Ready?.Invoke();
        }

        // Subscribed in the constructor, not in Run: the session can fail while still loading, before any Run.
        protected override UniTask OnDisposeAsync()
        {
            session.StateChanged -= OnStateChanged;
            Ready = null;
            return UniTask.CompletedTask;
        }

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Failed) gate.Fail();
        }
    }
}
