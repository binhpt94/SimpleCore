using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>Holds the game's one scene switcher across scene loads (kept with DontDestroyOnLoad). Holds nothing else.</summary>
    internal sealed class SceneSwitchHost : MonoBehaviour, ISceneSwitchHost
    {
        public SceneSwitcher Switcher { get; private set; }

        void ISceneSwitchHost.Hold(SceneSwitcher switcher)
        {
            Switcher = switcher;
        }
    }
}
