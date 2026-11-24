namespace Game.PopupLose
{
    /// <summary>Where the feature reports problems it cannot throw for (inside another feature's event).</summary>
    internal interface ILoseLog
    {
        void Error(string message);
    }

    internal sealed class UnityLoseLog : ILoseLog
    {
        public void Error(string message) => UnityEngine.Debug.LogError($"[PopupLose] {message}");
    }
}
