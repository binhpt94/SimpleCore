using NUnit.Framework;
using UnityEngine;

namespace Game.Run.Tests
{
    /// <summary>The best score as saved on the device (feature-run/run-score/score-rules), with the real store.</summary>
    public class RunBestScoreStoreTests
    {
        private const string Key = PlayerPrefsBestScoreStore.Key;

        private bool _had;
        private int _savedInt;
        private string _savedString;

        [SetUp]
        public void SetUp()
        {
            // Keep whatever the editor already has under the key and put it back after the test.
            _had = PlayerPrefs.HasKey(Key);
            _savedInt = PlayerPrefs.GetInt(Key, int.MinValue);
            _savedString = PlayerPrefs.GetString(Key, null);
            PlayerPrefs.DeleteKey(Key);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Key);
            if (_had)
            {
                if (_savedInt != int.MinValue) PlayerPrefs.SetInt(Key, _savedInt);
                else if (_savedString != null) PlayerPrefs.SetString(Key, _savedString);
            }
            PlayerPrefs.Save();
        }

        private static RunScore LoadedScore()
        {
            var score = new RunScore(new PlayerPrefsBestScoreStore());
            score.LoadBest();
            return score;
        }

        [Test]
        public void NothingSavedOnTheDeviceMeansTheBestIsZero()
        {
            Assert.AreEqual(0, LoadedScore().Best);
        }

        [Test]
        public void NegativeSavedBestCountsAsZero()
        {
            PlayerPrefs.SetInt(Key, -5);
            Assert.AreEqual(0, LoadedScore().Best);
        }

        [Test]
        public void SavedBestThatIsNotANumberCountsAsZero()
        {
            PlayerPrefs.SetString(Key, "abc");
            Assert.AreEqual(0, LoadedScore().Best);
        }

        [Test]
        public void SavedBestThatIsANegativeNumberWrittenAsTextCountsAsZero()
        {
            PlayerPrefs.SetString(Key, "-5");
            Assert.AreEqual(0, LoadedScore().Best);
        }

        [Test]
        public void SavedBestThatStartsWithDigitsButIsNotANumberCountsAsZero()
        {
            PlayerPrefs.SetString(Key, "12abc");
            Assert.AreEqual(0, LoadedScore().Best);
        }

        [Test]
        public void NewBestIsWrittenToTheDeviceAndReadBackNextTime()
        {
            var score = LoadedScore();
            score.StartRun();
            score.CountClearedWave(1);
            score.CountClearedWave(2);
            score.Settle();

            Assert.AreEqual("2", PlayerPrefs.GetString(Key, null));
            Assert.AreEqual(2, LoadedScore().Best);
        }
    }
}
