using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Services.SessionScene;

namespace Game.SceneFlow
{
    /// <summary>
    /// The Loading scene's side: shows the loading screen, loads the Game scene on top, fills the bar
    /// with the Game scene's load progress, waits until that scene can be played and the screen has
    /// been up long enough, then ends the Loading session and removes the Loading scene.
    /// </summary>
    internal sealed class LoadingToGameFlow : ISessionParticipant
    {
        private readonly ISessionScene session;
        private readonly ISceneLoader scenes;
        private readonly ILoadingScreenSource screens;
        private readonly ITimer timer;
        private readonly SceneFlowSettings settings;
        private readonly Action<Exception> reportError;

        private ILoadingScreen screen;
        private ISessionLoadProgress progress;
        private bool disposed;
        private bool gameFailed;

        internal LoadingToGameFlow(ISessionScene session, ISceneLoader scenes, ILoadingScreenSource screens,
            ITimer timer, SceneFlowSettings settings, Action<Exception> reportError)
        {
            this.session = session;
            this.scenes = scenes;
            this.screens = screens;
            this.timer = timer;
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
            screen.ShowProgress(0f);
            // The minimum time counts from the moment the screen shows, not from when loading ends.
            var shownLongEnough = timer.DelayAsync(settings.MinLoadingScreenTime);
            SwitchToGameAsync(shownLongEnough).Forget();
        }

        public UniTask DisposeAsync()
        {
            disposed = true;
            StopFollowingProgress();
            screen?.Close();
            screen = null;
            return UniTask.CompletedTask;
        }

        private async UniTask SwitchToGameAsync(UniTask shownLongEnough)
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

                FollowProgress(gate.Progress);

                var playable = await gate.WaitAsync();
                if (disposed) return;
                if (!playable)
                {
                    // The bar stays where it stopped; the error names the feature that failed. Still listening:
                    // the name may arrive just after the failure. The Loading scene stays so the player sees this.
                    gameFailed = true;
                    ShowGameFailure();
                    return;
                }

                await shownLongEnough;
                if (disposed) return;

                await session.EndAsync();
                await scenes.UnloadAsync(settings.LoadingScene);
            }
            catch (Exception e)
            {
                StopFollowingProgress();
                reportError(e);
                screen?.ShowError(settings.LoadFailedMessage);
            }
        }

        private void FollowProgress(ISessionLoadProgress source)
        {
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{settings.GameScene}' gate carries no load progress.");
            }

            progress = source;
            progress.LoadProgressChanged += OnProgressChanged;
            OnProgressChanged();
        }

        private void StopFollowingProgress()
        {
            if (progress == null) return;
            progress.LoadProgressChanged -= OnProgressChanged;
            progress = null;
        }

        private void OnProgressChanged()
        {
            if (screen == null || progress == null) return;

            if (gameFailed)
            {
                ShowGameFailure();
                return;
            }

            var total = progress.ParticipantCount;
            screen.ShowProgress(total > 0 ? (float)progress.LoadedCount / total : 0f);
        }

        private void ShowGameFailure()
        {
            screen?.ShowError(settings.FailedMessageFor(progress?.FailedParticipant));
        }
    }
}
