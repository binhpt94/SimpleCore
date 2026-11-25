#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Home.Tests
{
    /// <summary>feature-home/cheat: the with-record state.</summary>
    public sealed class HomeCheatTests
    {
        [Test]
        public void WithRecordShowsStartScreenWithRecordFifteen()
        {
            var config = ScriptableObject.CreateInstance<HomeConfig>();
            try
            {
                var saved = new FakeRunScore { Best = 3 };
                var score = HomeCheat.Apply(HomeCheatState.WithRecord, saved);
                var factory = new FakeHomeFactory();
                var screen = new HomeScreenPresenter(score, config);
                var participant = new HomeParticipant(screen, config, factory);

                participant.Run();
                screen.Show();

                var shown = factory.Display.Content;
                Assert.IsTrue(factory.Display.Visible);
                Assert.AreEqual("Thủ thành anh hùng", shown.Title);
                Assert.AreEqual("Kỉ lục: 15 đợt", shown.Record);
                Assert.AreEqual("Chơi", shown.PlayLabel);
                Assert.AreEqual(3, factory.Layout.Elements.Count);
                Assert.AreEqual(3, saved.Best, "The saved best score is not touched.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void WithRecordKeepsTheRunsOtherScoreValues()
        {
            var saved = new FakeRunScore { Best = 3, Score = 2, IsNewBest = true };

            var score = HomeCheat.Apply(HomeCheatState.WithRecord, saved);

            Assert.AreEqual(15, score.Best);
            Assert.AreEqual(2, score.Score);
            Assert.IsTrue(score.IsNewBest);
        }

        [Test]
        public void NoCheatLeavesTheScoreAlone()
        {
            var saved = new FakeRunScore { Best = 3 };

            Assert.AreSame(saved, HomeCheat.Apply(HomeCheatState.None, saved));
        }
    }
}
#endif
