using System.Globalization;
using UnityEngine;

namespace Game.Run
{
    /// <summary>Keeps the one best score of the game on the device.</summary>
    internal sealed class PlayerPrefsBestScoreStore : IBestScoreStore
    {
        internal const string Key = "Run/Best";

        // Returned by GetString on platforms that know the key holds no string.
        private const string NoString = "\u0000no-string";

        // The best is saved as text and read back as text. PlayerPrefs does not check the stored
        // type on every platform (the Windows editor turns the string "abc" into GetInt 6513249),
        // so reading text and parsing it is the only way to tell a real number from anything else.
        // Text that is not a whole number counts as 0; negative values are clamped by RunScore.
        public int Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return 0;
            var text = PlayerPrefs.GetString(Key, NoString);
            if (text == NoString) return PlayerPrefs.GetInt(Key, 0);
            return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var best)
                ? best
                : 0;
        }

        public void Save(int best)
        {
            PlayerPrefs.SetString(Key, best.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
