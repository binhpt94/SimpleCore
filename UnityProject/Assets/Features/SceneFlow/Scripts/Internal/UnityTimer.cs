using System;
using Cysharp.Threading.Tasks;

namespace Game.SceneFlow
{
    internal sealed class UnityTimer : ITimer
    {
        public UniTask DelayAsync(TimeSpan delay)
        {
            // Real seconds: the loading screen's minimum time must not stretch with Time.timeScale.
            return UniTask.Delay(delay, ignoreTimeScale: true);
        }
    }
}
