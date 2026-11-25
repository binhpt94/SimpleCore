using UnityEngine;

namespace Game.Run
{
    /// <summary>Keeps the one best score of the game on the device.</summary>
    internal sealed class PlayerPrefsBestScoreStore : IBestScoreStore
    {
        internal const string Key = "Run/Best";

        // A value saved under another type reads back as the default (0).
        public int Load() => PlayerPrefs.GetInt(Key, 0);

        public void Save(int best)
        {
            PlayerPrefs.SetInt(Key, best);
            PlayerPrefs.Save();
        }
    }
}
