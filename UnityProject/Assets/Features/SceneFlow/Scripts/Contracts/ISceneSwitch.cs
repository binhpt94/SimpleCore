using System;

namespace Game.SceneFlow
{
    /// <summary>
    /// Direct scene loads for the cheat panel (node feature-cheat-panel/cheat-states/states-rules): reload a scene
    /// from its start even when it is the one running, or restart the game from its first scene. Registered in
    /// every scene. Normal game flow does not use it; it keeps going through the usual scene requests.
    /// </summary>
    public interface ISceneSwitch
    {
        /// <summary>True from the moment a scene switch starts until the new scene is ready.</summary>
        bool IsSwitching { get; }

        /// <summary>Raised each time <see cref="IsSwitching"/> changes.</summary>
        event Action SwitchingChanged;

        /// <summary>
        /// Loads <paramref name="scene"/> from its start through the usual loading screen, also when it is the scene
        /// running now. Does nothing while <see cref="IsSwitching"/>.
        /// </summary>
        void Reload(string scene);

        /// <summary>
        /// Runs the game again from its first scene, as when the app was just opened. Does nothing while
        /// <see cref="IsSwitching"/>.
        /// </summary>
        void Restart();
    }
}
