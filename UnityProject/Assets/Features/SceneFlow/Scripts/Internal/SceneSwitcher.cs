using System;
using Cysharp.Threading.Tasks;
using Game.Services.SessionScene;

namespace Game.SceneFlow
{
    /// <summary>
    /// Carries out a switch from the running scene to another (design: feature-scene-flow/scene-request/switch-rules),
    /// in six steps: take the name, show the cover, end the old scene's session, load the new scene in its place,
    /// let it run, take the cover away. The cover is the loading screen: it belongs to neither scene and is the only
    /// thing of this feature that lives through the switch. Lives as long as the game, not as long as a scene.
    /// </summary>
    internal sealed class SceneSwitcher
    {
        private readonly ISceneLoader scenes;
        private readonly ILoadingScreenSource covers;
        private readonly ITimer timer;
        private readonly SceneFlowSettings settings;
        private readonly ISceneFlowLog log;

        private string currentScene;
        private ILoadingScreen cover;
        private ISessionLoadProgress progress;
        private bool switching;
        private bool failed;

        internal SceneSwitcher(string currentScene, ISceneLoader scenes, ILoadingScreenSource covers, ITimer timer,
            SceneFlowSettings settings, ISceneFlowLog log)
        {
            this.currentScene = currentScene;
            this.scenes = scenes;
            this.covers = covers;
            this.timer = timer;
            this.settings = settings;
            this.log = log;
        }

        /// <summary>The scene running now (the target of the last switch once it is loaded).</summary>
        internal string CurrentScene => currentScene;

        /// <summary>True from the moment the cover shows until it is taken away. Stays true when the switch failed.</summary>
        internal bool IsSwitching => switching;

        /// <summary>
        /// Step 1: checks the request and, when it is valid, starts the switch. Returns false for a refused request:
        /// an unknown scene (logged as an error), a switch already going (logged as a warning, no queue), or the
        /// scene already running (ignored). A refused request changes nothing.
        /// </summary>
        /// <param name="endCurrentSession">Ends the running scene's session: every participant lets go of what it holds.</param>
        internal bool Request(string sceneName, Func<UniTask> endCurrentSession)
        {
            if (!settings.IsKnownScene(sceneName))
            {
                log.Error($"Switch refused: there is no scene named '{sceneName}'. The current scene keeps running.");
                return false;
            }

            if (switching)
            {
                log.Warning($"Switch to '{sceneName}' ignored: a switch is already going.");
                return false;
            }

            if (sceneName == currentScene) return false;

            switching = true;
            failed = false;

            // Step 2, at once: from here every tap lands on the cover.
            cover = covers.Create();
            cover.ShowProgress(0f);

            // The minimum time counts from the moment the cover shows, not from when loading ends.
            var shownLongEnough = timer.DelayAsync(settings.MinLoadingScreenTime);
            WarnIfSlowAsync(sceneName).Forget();
            SwitchAsync(sceneName, endCurrentSession, shownLongEnough).Forget();
            return true;
        }

        private async UniTask SwitchAsync(string sceneName, Func<UniTask> endCurrentSession, UniTask shownLongEnough)
        {
            try
            {
                // Step 3. A feature of the old scene failing to let go is the session's to log; the switch goes on.
                try
                {
                    await endCurrentSession();
                }
                catch (Exception e)
                {
                    log.Exception(e);
                }

                // Step 4.
                await scenes.LoadReplacingAsync(sceneName);
                currentScene = sceneName;

                var gate = scenes.FindGate();
                if (gate == null)
                {
                    throw new InvalidOperationException(
                        $"Scene '{sceneName}' has no SceneFlow gate: add the SceneFlow game installer " +
                        "as the last installer of its Boot.");
                }

                FollowProgress(gate.Progress, sceneName);

                // Step 5: the gate opens once every feature of the new scene has run (made its views).
                var playable = await gate.WaitAsync();
                if (!playable)
                {
                    // The cover stays, the bar stays where it stopped, the error names the feature that failed.
                    // Still listening: the name may arrive just after the failure. No way back: the old scene is gone.
                    failed = true;
                    ShowFailure();
                    return;
                }

                await shownLongEnough;

                // Step 6, at once. From here a new switch may be asked for.
                StopFollowingProgress();
                cover.Close();
                cover = null;
                switching = false;
            }
            catch (Exception e)
            {
                failed = true;
                StopFollowingProgress();
                log.Exception(e);
                cover?.ShowError(settings.LoadFailedMessage);
            }
        }

        private async UniTask WarnIfSlowAsync(string sceneName)
        {
            await timer.DelayAsync(settings.SlowSwitchWarningTime);
            if (!switching || failed) return;
            log.Warning($"Switch to '{sceneName}' still going after {settings.SlowSwitchWarningTime.TotalSeconds:0.#} s " +
                        "(performance target). It keeps going.");
        }

        private void FollowProgress(ISessionLoadProgress source, string sceneName)
        {
            if (source == null)
            {
                throw new InvalidOperationException($"Scene '{sceneName}' gate carries no load progress.");
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
            if (cover == null || progress == null) return;

            if (failed)
            {
                ShowFailure();
                return;
            }

            var total = progress.ParticipantCount;
            cover.ShowProgress(total > 0 ? (float)progress.LoadedCount / total : 0f);
        }

        private void ShowFailure()
        {
            cover?.ShowError(settings.FailedMessageFor(progress?.FailedParticipant));
        }
    }
}
