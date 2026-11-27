using System;
using Cysharp.Threading.Tasks;

namespace Game.SceneFlow
{
    /// <summary>Waiting for real time to pass. Tests replace it with a fake they finish by hand.</summary>
    internal interface ITimer
    {
        UniTask DelayAsync(TimeSpan delay);
    }

    /// <summary>Everything this feature asks of Unity's scene system. Tests replace it with a fake.</summary>
    internal interface ISceneLoader
    {
        /// <summary>
        /// Loads the scene in place of every scene open now; those are removed. Throws when it cannot be loaded.
        /// Only the switch cover survives (it is kept across scenes).
        /// </summary>
        UniTask LoadReplacingAsync(string sceneName);

        void SetActive(string sceneName);

        /// <summary>The gate carried by an object of any loaded scene, or null when there is none.</summary>
        SceneGate FindGate();
    }

    /// <summary>Where this feature writes what went wrong. Tests replace it with a fake that keeps the lines.</summary>
    internal interface ISceneFlowLog
    {
        void Warning(string message);

        void Error(string message);

        void Exception(Exception exception);
    }

    /// <summary>The scene object that carries a gate so another scene can find it.</summary>
    internal interface ISceneGateCarrier
    {
        SceneGate Gate { get; }

        void Carry(SceneGate gate);
    }

    internal interface ISceneGateCarrierSource
    {
        ISceneGateCarrier Create();
    }

    /// <summary>The object, kept across scenes, that holds the one switcher of the game process.</summary>
    internal interface ISceneSwitchHost
    {
        SceneSwitcher Switcher { get; }

        void Hold(SceneSwitcher switcher);
    }

    internal interface ISceneSwitchHostSource
    {
        /// <summary>The host made by an earlier scene of this run of the game, or null when there is none yet.</summary>
        ISceneSwitchHost Find();

        /// <summary>Makes the host; it lives as long as the game.</summary>
        ISceneSwitchHost Create();
    }

    /// <summary>What the player sees while a scene gets ready: the loading screen, which is also the switch cover.</summary>
    internal interface ILoadingScreen
    {
        /// <summary>The Unity object of this screen, for the session's view count (not for the switch cover).</summary>
        UnityEngine.Object Root { get; }

        /// <summary>Fills the bar: 0 is nothing loaded, 1 is everything loaded.</summary>
        void ShowProgress(float fraction);

        void ShowError(string message);

        void Close();
    }

    internal interface ILoadingScreenSource
    {
        ILoadingScreen Create();
    }
}
