using System;
using System.Collections.Generic;
using Game.Save;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// The best as handed to and read from feature-save (feature-run/run-score/score-rules): the run decides the value
    /// and when to write; feature-save is faked here.
    /// </summary>
    public class RunSavedBestTests
    {
        private FakeSave _save;
        private RunScore _score;

        [SetUp]
        public void SetUp()
        {
            _save = new FakeSave();
            _score = new RunScore(_save);
        }

        /// <summary>One run that clears <paramref name="waves"/> waves and ends (castle destroyed).</summary>
        private void PlayRun(int waves)
        {
            _score.StartRun();
            for (var w = 1; w <= waves; w++) _score.CountClearedWave(w);
            _score.Settle();
        }

        [Test]
        public void SaveSayingNoneYetMeansTheBestIsZero()
        {
            // record-default-zero: first launch or unreadable data, feature-save says "none yet" (record is not used).
            _save.Stored = null;
            _save.NoneYetRecord = 7;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);
        }

        [Test]
        public void SavedDataThatIsNotANumberCountsAsZeroAndTheNextNewBestOverwritesIt()
        {
            // edge-du-lieu-luu-khong-phai-so: feature-save reports such data as "none yet".
            _save.Stored = null;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);

            PlayRun(1);
            Assert.IsTrue(_score.IsNewBest);
            CollectionAssert.AreEqual(new[] { 1 }, _save.Writes, "The next new best replaces the broken data.");
        }

        [Test]
        public void NegativeSavedBestCountsAsZero()
        {
            // edge-du-lieu-luu-la-so-am: feature-save hands back the negative number; the run treats it as 0.
            _save.Stored = -5;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);

            PlayRun(0);
            Assert.IsFalse(_score.IsNewBest, "A score of 0 never sets a new best.");
            CollectionAssert.IsEmpty(_save.Writes);

            PlayRun(1);
            Assert.AreEqual(1, _score.Best);
            CollectionAssert.AreEqual(new[] { 1 }, _save.Writes);
        }

        [Test]
        public void NewBestIsHandedToSaveTheMomentTheRunIsSettled()
        {
            // record-save-now: written inside Settle, i.e. before the lose popup can show.
            _save.Stored = 1;
            _score.LoadBest();
            _score.StartRun();
            _score.CountClearedWave(1);
            _score.CountClearedWave(2);
            CollectionAssert.IsEmpty(_save.Writes, "Nothing is written while the run is still going.");

            _score.Settle();
            CollectionAssert.AreEqual(new[] { 2 }, _save.Writes);
        }

        [Test]
        public void FailedWriteKeepsTheNewBestInMemoryWithoutTellingThePlayer()
        {
            // record-keep-on-fail, edge-feature-save-ghi-that-bai.
            _save.FailWrites = true;
            Assert.DoesNotThrow(() => PlayRun(3));

            Assert.AreEqual(3, _score.Best, "Start screen and lose popup still see the new best.");
            Assert.IsTrue(_score.IsNewBest);
            Assert.IsTrue(_score.IsBestUnsaved);

            PlayRun(1);
            Assert.AreEqual(3, _score.Best, "The best stays for as long as the game is open.");
        }

        [Test]
        public void UnsavedBestIsWrittenAgainWhenTheGameComesBackToTheScreen()
        {
            // record-retry: one try each time the game is on the screen again; once written, no more tries.
            var signals = new FakeSignals();
            using (new RunInterrupts(signals, new FakeHold(), _score.RetryUnsavedBest))
            {
                _save.FailWrites = true;
                PlayRun(2);
                Assert.AreEqual(1, _save.Attempts);

                signals.Leave();
                signals.Return();
                Assert.AreEqual(2, _save.Attempts, "Coming back tries once.");
                Assert.IsTrue(_score.IsBestUnsaved);

                _save.FailWrites = false;
                signals.Leave();
                signals.Return();
                Assert.AreEqual(3, _save.Attempts);
                Assert.AreEqual(2, _save.Stored);
                Assert.IsFalse(_score.IsBestUnsaved);

                signals.Leave();
                signals.Return();
                Assert.AreEqual(3, _save.Attempts, "Written: no more tries.");
            }
        }

        [Test]
        public void UnsavedBestIsWrittenAgainAtTheNextSettleEvenWithoutANewBest()
        {
            // record-retry: the next run sets no new best and still tries once.
            _save.FailWrites = true;
            PlayRun(4);
            Assert.AreEqual(1, _save.Attempts);

            _save.FailWrites = false;
            PlayRun(1);
            Assert.IsFalse(_score.IsNewBest);
            Assert.AreEqual(2, _save.Attempts);
            Assert.AreEqual(4, _save.Stored);
            Assert.IsFalse(_score.IsBestUnsaved);

            PlayRun(1);
            Assert.AreEqual(2, _save.Attempts, "Nothing left to write.");
        }

        [Test]
        public void ComingBackWithTheBestAlreadySavedWritesNothing()
        {
            var signals = new FakeSignals();
            using (new RunInterrupts(signals, new FakeHold(), _score.RetryUnsavedBest))
            {
                PlayRun(2);
                signals.Leave();
                signals.Return();
                Assert.AreEqual(1, _save.Attempts);
            }
        }

        [Test]
        public void OnlyTheBestIsHandedToSave()
        {
            // replay-checks#only-record-saved: score, waves and team are never written; only a changed best is.
            _score.LoadBest();
            PlayRun(2);
            PlayRun(1);
            PlayRun(3);
            CollectionAssert.AreEqual(new[] { 2, 3 }, _save.Writes);
        }

        /// <summary>feature-save, faked. Stored null: "none yet".</summary>
        private sealed class FakeSave : ISavedRecord
        {
            public int? Stored;
            public int NoneYetRecord;
            public bool FailWrites;
            public int Attempts;
            public readonly List<int> Writes = new List<int>();

            public bool TryRead(out int record)
            {
                record = Stored ?? NoneYetRecord;
                return Stored.HasValue;
            }

            public bool Write(int record)
            {
                Attempts++;
                if (FailWrites) return false;
                Stored = record;
                Writes.Add(record);
                return true;
            }
        }

        private sealed class FakeSignals : IAppSignals
        {
            public event Action LeftApp;
            public event Action CameBack;

            public void Leave() => LeftApp?.Invoke();

            public void Return() => CameBack?.Invoke();

            public void Dispose() { }
        }

        private sealed class FakeHold : IRunWaitHold
        {
            public void Hold() { }

            public void Release() { }
        }
    }
}
