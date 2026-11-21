using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.SessionScene;

namespace Game.SceneFlow
{
    /// <summary>
    /// The Loading scene's side: shows the loading screen, loads the Game scene on top, waits until
    /// that scene can be played, then ends the Loading session and removes the Loading scene.
    /// </summary>
    internal sealed class LoadingToGameFlow : ISessionParticipant
    {
        private readonly ISessionScene session;
        private readonly ISceneLoader scenes;
        private readonly ILoadingScreenSource screens;
        private readonly SceneFlowSettings settings;
        private readonly Action<Exception> reportError;

        private ILoadingScreen screen;
        private bool disposed;

        internal LoadingToGameFlow(ISessionScene session, ISceneLoader scenes, ILoadingScreenSource screens,
            SceneFlowSettings settings, Action<Exception> reportError)
        {
            this.session = session;
            this.scenes = scenes;
            this.screens = screens;
            this.settings = settings;
            this.reportError = reportError;
        }

        public UniTask LoadAsync(CancellationToken cancellation)
        {
            return UniTask.CompletedTask;
        }

        public void Run()
        {
            screen = screens.Create();
            SwitchToGameAsync().Forget();
        }

        public UniTask DisposeAsync()
        {
            disposed = true;
            screen?.Close();
            screen = null;
            return UniTask.CompletedTask;
        }

        private async UniTask SwitchToGameAsync()
        {
            try
            {
                await scenes.LoadAdditiveAsync(settings.GameScene);
                if (disposed) return;

                var gate = scenes.FindGate();
                if (gate == null)
                {
                    throw new InvalidOperationException(
                        $"Scene '{settings.GameScene}' has no SceneFlow gate: add the SceneFlow game installer " +
                        "as the last installer of its Boot.");
                }

                var playable = await gate.WaitAsync();
                if (disposed) return;
                if (!playable)
                {
                    // The Game session already reported why; the Loading scene stays so the player sees this.
                    screen.ShowError(settings.LoadFailedMessage);
                    return;
                }

                await session.EndAsync();
                await scenes.UnloadAsync(settings.LoadingScene);
            }
            catch (Exception e)
            {
                reportError(e);
                screen?.ShowError(settings.LoadFailedMessage);
            }
        }
    }
}
