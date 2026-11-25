using UnityEngine;

namespace Game.Run
{
    internal sealed class UnityRunLog : IRunLog
    {
        public void Error(string message) => Debug.LogError("[Run] " + message);
    }
}
