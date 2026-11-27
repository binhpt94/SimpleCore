#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Core
{
    /// <summary>
    /// Plain in-memory <see cref="ICheatSelection"/>. Holds the pick and the running state; writes nothing to the
    /// device. Not static: <see cref="CheatSelectionAsset"/> keeps the one instance Boot registers. Dev builds only.
    /// </summary>
    public sealed class CheatSelection : ICheatSelection
    {
        public bool HasPick { get; private set; }

        public string PickedKey { get; private set; }

        public string RunningKey { get; private set; }

        public void Pick(string key)
        {
            PickedKey = string.IsNullOrEmpty(key) ? null : key;
            HasPick = true;
        }

        public void ReportRunning(string key)
        {
            RunningKey = string.IsNullOrEmpty(key) ? null : key;
        }

        public void BeginScene()
        {
            RunningKey = null;
        }
    }
}
#endif
