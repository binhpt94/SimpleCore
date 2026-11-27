using System;
using Game.Core;

namespace Game.SceneFlow
{
    /// <summary>
    /// One scene's <see cref="ISceneSwitch"/> (design: feature-cheat-panel/cheat-states/states-rules): direct loads
    /// for the cheat panel over the switcher every scene shares. Normal game flow keeps using
    /// <see cref="SceneSwitcher.Request"/>, where a request for the scene already running is ignored; only
    /// <see cref="Reload"/> loads it again. Registered in every scene and every build.
    /// <para>
    /// The switcher outlives the scene, so this scene's listeners are dropped once its session starts letting go:
    /// a listener of a scene that is gone is never called.
    /// </para>
    /// </summary>
    internal sealed class SceneSwitchService : ISceneSwitch
    {
        private readonly SceneSwitcher switcher;
        private readonly ISessionScene session;
        private readonly string firstScene;
        private bool attached;

        /// <param name="firstScene">The scene the game opens on (the Loading scene); <see cref="Restart"/> goes there.</param>
        internal SceneSwitchService(SceneSwitcher switcher, ISessionScene session, string firstScene)
        {
            this.switcher = switcher;
            this.session = session;
            this.firstScene = firstScene;

            switcher.SwitchingChanged += OnSwitchingChanged;
            session.StateChanged += OnSessionStateChanged;
            attached = true;
        }

        public bool IsSwitching => switcher.IsSwitching;

        public event Action SwitchingChanged;

        public void Reload(string scene)
        {
            if (switcher.IsSwitching) return;
            switcher.Reload(scene, session.EndAsync);
        }

        public void Restart()
        {
            if (switcher.IsSwitching) return;
            // As when the app was just opened: the first scene loads from its start and its own flow goes on from there.
            switcher.Reload(firstScene, session.EndAsync);
        }

        private void OnSwitchingChanged()
        {
            SwitchingChanged?.Invoke();
        }

        private void OnSessionStateChanged(SessionState state)
        {
            if (!attached || (state != SessionState.Disposing && state != SessionState.Disposed)) return;
            attached = false;
            switcher.SwitchingChanged -= OnSwitchingChanged;
            session.StateChanged -= OnSessionStateChanged;
            SwitchingChanged = null;
        }
    }
}
