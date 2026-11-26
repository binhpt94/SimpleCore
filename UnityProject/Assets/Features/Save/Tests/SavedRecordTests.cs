using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Game.Core;
using NUnit.Framework;
using UnityEditor;

namespace Game.Save.Tests
{
    /// <summary>In-memory device; can be told to refuse writes like a full disk.</summary>
    internal sealed class FakeStore : IRecordStore
    {
        public string Text;
        public bool Full;
        public int Saves;

        public bool TryLoad(out string text)
        {
            text = Text;
            return Text != null;
        }

        public bool TrySave(string text, out string reason)
        {
            if (Full) { reason = "disk full"; return false; }
            Saves++;
            Text = text;
            reason = null;
            return true;
        }
    }

    public class SavedRecordTests
    {
        private FakeStore store;
        private List<string> warnings;
        private SavedRecord save;

        [SetUp]
        public void SetUp()
        {
            store = new FakeStore();
            warnings = new List<string>();
            save = new SavedRecord(store, warnings.Add);
        }

        // rules-version-one
        [Test]
        public void EveryWrite_StoresDataVersionOne()
        {
            Assert.That(SavedRecord.DataVersion, Is.EqualTo(1));
            save.Write(7);
            Assert.That(store.Text.Split('\n')[0], Is.EqualTo("1"));
            save.Write(8);
            Assert.That(store.Text, Is.EqualTo("1\n8"));
        }

        // rules-record-int
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-5)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void WrittenRecord_IsReadBackExactly_EvenNegative(int record)
        {
            Assert.That(save.Write(record), Is.True);
            Assert.That(save.TryRead(out var read), Is.True);
            Assert.That(read, Is.EqualTo(record));
        }

        // rules-read-cases
        [Test]
        public void NothingSaved_ReadsNoneYet()
        {
            Assert.That(save.TryRead(out var record), Is.False);
            Assert.That(record, Is.EqualTo(0));
        }

        [TestCase("")]
        [TestCase("1")]
        [TestCase("1\n")]
        [TestCase("0\n5")]
        [TestCase("-1\n5")]
        [TestCase("abc\n5")]
        [TestCase("1\nabc")]
        [TestCase("1\n3.5")]
        [TestCase("2\n5")]
        [TestCase("1\n5\nextra")]
        [TestCase("1\n99999999999")]
        public void AnythingButVersionOneWithIntegerRecord_ReadsNoneYet(string onDevice)
        {
            store.Text = onDevice;
            Assert.That(save.TryRead(out _), Is.False);
        }

        [Test]
        public void VersionOneWithIntegerRecord_ReadsTheRecord()
        {
            store.Text = "1\n42";
            Assert.That(save.TryRead(out var record), Is.True);
            Assert.That(record, Is.EqualTo(42));
        }

        // rules-instant
        [Test]
        public void ReadAndWrite_FinishWithinTheCall_OnTheRealFile()
        {
            var dir = Path.Combine(Path.GetTempPath(), "SaveTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                var real = new SavedRecord(new FileRecordStore(Path.Combine(dir, "record.save")), null);
                var watch = Stopwatch.StartNew();
                Assert.That(real.Write(12), Is.True);
                Assert.That(real.TryRead(out var record), Is.True, "the written record is readable right after the call returns");
                watch.Stop();
                Assert.That(record, Is.EqualTo(12));
                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(200), "io_time_budget is under 10 ms; loose bound for CI");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        // rules-newer-reject
        [Test]
        public void NewerVersionOnDevice_EveryWriteFails_AndRecordStaysUntouched()
        {
            store.Text = "2\n77";
            Assert.That(save.Write(5), Is.False);
            Assert.That(save.Write(6), Is.False);
            Assert.That(store.Text, Is.EqualTo("2\n77"));
            Assert.That(store.Saves, Is.EqualTo(0));
            Assert.That(save.TryRead(out _), Is.False);
        }

        // rules-broken-kept
        [Test]
        public void BrokenRecord_IsNotDeletedByReading_AndIsReplacedBySuccessfulWrite()
        {
            store.Text = "1\n3.5";
            save.TryRead(out _);
            save.TryRead(out _);
            Assert.That(store.Text, Is.EqualTo("1\n3.5"));
            Assert.That(store.Saves, Is.EqualTo(0));

            Assert.That(save.Write(4), Is.True);
            Assert.That(save.TryRead(out var record), Is.True);
            Assert.That(record, Is.EqualTo(4));
        }

        [Test]
        public void BrokenVersionZero_DoesNotBlockWrites()
        {
            store.Text = "0\n9";
            Assert.That(save.Write(3), Is.True);
            Assert.That(store.Text, Is.EqualTo("1\n3"));
        }

        // rules-fail-silent
        [Test]
        public void DeviceRefusesWrite_ReturnsFailure_WithoutThrowing_AndLogsOneWarningWithReason()
        {
            store.Text = "1\n10";
            store.Full = true;
            bool result = true;
            Assert.DoesNotThrow(() => result = save.Write(11));
            Assert.That(result, Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("disk full"));
            Assert.That(save.TryRead(out var record), Is.True);
            Assert.That(record, Is.EqualTo(10), "failed write leaves the previous record");
        }

        [Test]
        public void RefusedNewerVersionWrite_LogsWarningWithReason()
        {
            store.Text = "2\n1";
            save.Write(5);
            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("version 2"));
        }

        [Test]
        public void SuccessfulWrite_LogsNothing()
        {
            save.Write(1);
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void TwoWritesInARow_LastOneRemains()
        {
            save.Write(3);
            save.Write(2);
            Assert.That(save.TryRead(out var record), Is.True);
            Assert.That(record, Is.EqualTo(2));
        }
    }

    public class FileRecordStoreTests
    {
        private string dir;
        private string file;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "SaveTests_" + Guid.NewGuid().ToString("N"));
            file = Path.Combine(dir, "record.save");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        // rules-write-whole
        [Test]
        public void GameKilledMidWrite_NextLaunchReadsTheOldRecord_NotNoneYet()
        {
            var store = new FileRecordStore(file);
            new SavedRecord(store, null).Write(5);

            // A write cut off after part of the temp file was written: the main file was never replaced.
            File.WriteAllText(store.TempPath, "1\n");

            var nextLaunch = new SavedRecord(new FileRecordStore(file), null);
            Assert.That(nextLaunch.TryRead(out var record), Is.True);
            Assert.That(record, Is.EqualTo(5));
        }

        [Test]
        public void WriteAfterInterruptedWrite_ReplacesWholeRecord()
        {
            var store = new FileRecordStore(file);
            var save = new SavedRecord(store, null);
            save.Write(5);
            File.WriteAllText(store.TempPath, "garbage");

            Assert.That(save.Write(9), Is.True);
            Assert.That(File.ReadAllText(file), Is.EqualTo("1\n9"));
            Assert.That(File.Exists(store.TempPath), Is.False);
        }

        [Test]
        public void FileThatCannotBeWritten_FailsWithReason_AndOldFileStays()
        {
            var store = new FileRecordStore(file);
            new SavedRecord(store, null).Write(5);
            Directory.CreateDirectory(store.TempPath); // temp path blocked, like a device refusing the write

            var warnings = new List<string>();
            Assert.That(new SavedRecord(store, warnings.Add).Write(6), Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(File.ReadAllText(file), Is.EqualTo("1\n5"));
        }
    }

    public class SaveInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Save/FeatureAssets/Public/SaveInstaller.asset";

        [Test]
        public void RealInstallerAsset_PublishesSavedRecord()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            Assert.That(installer, Is.Not.Null, "missing " + InstallerPath);
            var registry = new ServiceRegistry();

            installer.Install(registry);

            Assert.That(registry.Resolve<ISavedRecord>(), Is.Not.Null);
        }
    }
}
