using UnityEngine;

namespace Game.GameFlow
{
    /// <summary>Writes the flow's warnings and errors to the Unity console.</summary>
    internal sealed class UnityFlowLog : IFlowLog
    {
        public void Warning(string message) => Debug.LogWarning(message);

        public void Error(string message) => Debug.LogError(message);
    }
}
