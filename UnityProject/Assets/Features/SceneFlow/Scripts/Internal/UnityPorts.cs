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
        /// <summary>Loads the scene on top of the ones already open. Throws when it cannot be loaded.</summary>
        UniTask LoadAdditiveAsync(string sceneName);

        UniTask UnloadAsync(string sceneName);

        void SetActive(string sceneName);

        /// <summary>The gate carried by an object of any loaded scene, or null when there is none.</summary>
        SceneGate FindGate();
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

    /// <summary>What the player sees while the Game scene gets ready.</summary>
    internal interface ILoadingScreen
    {
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
