#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class CheatSelectionTests
    {
        [Test]
        public void New_HasNoPickAndNothingRunning()
        {
            var s = new CheatSelection();

            Assert.That(s.HasPick, Is.False);
            Assert.That(s.PickedKey, Is.Null);
            Assert.That(s.RunningKey, Is.Null);
        }

        [Test]
        public void Pick_KeepsKeyAndSetsHasPick()
        {
            var s = new CheatSelection();

            s.Pick("PopupLose/NewRecord");

            Assert.That(s.HasPick, Is.True);
            Assert.That(s.PickedKey, Is.EqualTo("PopupLose/NewRecord"));
        }

        [Test]
        public void PickNull_IsNormalPlay_HasPickWithNoKey()
        {
            var s = new CheatSelection();
            s.Pick("PopupLose/NewRecord");

            s.Pick(null);

            Assert.That(s.HasPick, Is.True);
            Assert.That(s.PickedKey, Is.Null);
        }

        [Test]
        public void ReportRunning_SetsRunningKey()
        {
            var s = new CheatSelection();

            s.ReportRunning("Run/Wave5");

            Assert.That(s.RunningKey, Is.EqualTo("Run/Wave5"));
        }

        [Test]
        public void BeginScene_ClearsRunningKeyButKeepsPick()
        {
            var s = new CheatSelection();
            s.Pick("Run/Wave5");
            s.ReportRunning("Run/Wave5");

            s.BeginScene();

            Assert.That(s.RunningKey, Is.Null);
            Assert.That(s.HasPick, Is.True);
            Assert.That(s.PickedKey, Is.EqualTo("Run/Wave5"));
        }

        [Test]
        public void Asset_StartsEmpty_KeepsOneSelection_AndIsNotUnloaded()
        {
            var asset = ScriptableObject.CreateInstance<CheatSelectionAsset>();
            try
            {
                Assert.That(asset.Selection, Is.Not.Null);
                Assert.That(asset.Selection.HasPick, Is.False);
                Assert.That(asset.Selection, Is.SameAs(asset.Selection));
                Assert.That((asset.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0, Is.True);

                asset.Selection.Pick("Run/Wave5");
                asset.Selection.BeginScene();
                Assert.That(asset.Selection.PickedKey, Is.EqualTo("Run/Wave5"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
#endif
