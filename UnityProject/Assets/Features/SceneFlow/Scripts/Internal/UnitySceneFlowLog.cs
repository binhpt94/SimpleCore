using System;
using UnityEngine;

namespace Game.SceneFlow
{
    internal sealed class UnitySceneFlowLog : ISceneFlowLog
    {
        public void Warning(string message)
        {
            Debug.LogWarning("[SceneFlow] " + message);
        }

        public void Error(string message)
        {
            Debug.LogError("[SceneFlow] " + message);
        }

        public void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
