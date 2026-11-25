using System.Linq;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Home.Tests
{
    /// <summary>feature-home/home-screen/home-screen-spec: what the start screen shows and how Play reacts.</summary>
    public sealed class HomeScreenTests
    {
        private HomeConfig _config;
        private FakeRunFlow _flow;
        private FakeRunScore _score;
        private FakeHomeDisplay _display;
        private HomeScreenPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<HomeConfig>();
            _flow = new FakeRunFlow();
            _score = new FakeRunScore();
            _display = new FakeHomeDisplay();
            _presenter = new HomeScreenPresenter(_flow, _score, _config, _display);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Stop();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TitleLineShowsGameName()
        {
            _presenter.Start();

            Assert.IsTrue(_display.Visible);
            Assert.AreEqual("Thủ thành anh hùng", _display.Content.Title);
        }

        [Test]
        public void TitleCannotBePressed()
        {
            var layout = HomeLayout.From(_config);

            var title = layout.Elements.Single(e => e.Id == HomeElementId.Title);
            Assert.AreEqual(HomeElementKind.Label, title.Kind);
        }

        [Test]
        public void RecordLineShowsCurrentBest()
        {
            _score.Best = 7;
            _presenter.Start();

            Assert.AreEqual("Kỉ lục: 7 đợt", _display.Content.Record);
        }

        [Test]
        public void RecordLineShowsZeroWhenNoBestYet()
        {
            _score.Best = 0;
            _presenter.Start();

            Assert.AreEqual("Kỉ lục: 0 đợt", _display.Content.Record);
        }

        [Test]
        public void VeryLargeRecordShowsEveryDigit()
        {
            _score.Best = 999;
            _presenter.Start();
            Assert.AreEqual("Kỉ lục: 999 đợt", _display.Content.Record);

            _score.SetBest(123456);
            Assert.AreEqual("Kỉ lục: 123456 đợt", _display.Content.Record);
        }

        [Test]
        public void PlayButtonSaysChoi()
        {
            _presenter.Start();

            Assert.AreEqual("Chơi", _display.Content.PlayLabel);
        }

        [Test]
        public void FirstTapOnPlayReportsPlayToTheRun()
        {
            _presenter.Start();

            _display.PressPlay();

            Assert.AreEqual(1, _flow.PlayCalls);
        }

        [Test]
        public void MoreTapsWhileLeavingAreIgnored()
        {
            _presenter.Start();

            _display.PressPlay();
            _display.PressPlay();
            _display.PressPlay();

            Assert.AreEqual(1, _flow.PlayCalls);
        }

        [Test]
        public void ScreenHidesWhenRunMovesToTeamSelect()
        {
            _presenter.Start();
            _display.PressPlay();

            _flow.GoTo(RunStep.TeamSelect);

            Assert.IsFalse(_display.Visible);
            _display.PressPlay();
            Assert.AreEqual(1, _flow.PlayCalls);
        }

        [Test]
        public void ScreenDoesNotShowOutsideHomeStep()
        {
            _flow.GoTo(RunStep.Playing);
            _presenter.Start();

            Assert.IsFalse(_display.Visible);
            _display.PressPlay();
            Assert.AreEqual(0, _flow.PlayCalls);
        }

        [Test]
        public void ScreenHasExactlyThreeElementsAndOnlyPlayCanBePressed()
        {
            var layout = HomeLayout.From(_config);

            Assert.AreEqual(3, layout.Elements.Count);
            var buttons = layout.Elements.Where(e => e.Kind == HomeElementKind.Button).ToList();
            Assert.AreEqual(1, buttons.Count);
            Assert.AreEqual(HomeElementId.Play, buttons[0].Id);
        }

        [Test]
        public void BackFromAbandonWithNewBestShowsNewRecord()
        {
            _score.Best = 4;
            _presenter.Start();
            _display.PressPlay();
            _flow.GoTo(RunStep.TeamSelect);
            _flow.GoTo(RunStep.Playing);
            _flow.GoTo(RunStep.Paused);

            _score.SetBest(9, raise: false);
            _flow.GoTo(RunStep.Home);

            Assert.IsTrue(_display.Visible);
            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
        }

        [Test]
        public void RecordSavedJustAfterReturningHomeStillShows()
        {
            _score.Best = 4;
            _flow.GoTo(RunStep.Paused);
            _presenter.Start();

            _flow.GoTo(RunStep.Home);
            _score.SetBest(9);

            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
        }

        [Test]
        public void ScreenNeverAnnouncesNewBest()
        {
            _score.Best = 9;
            _score.IsNewBest = true;
            _presenter.Start();

            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
            Assert.AreEqual("Thủ thành anh hùng", _display.Content.Title);
        }

        [Test]
        public void PlayTakesOneNewTapEachTimeScreenShowsAgain()
        {
            _presenter.Start();
            _display.PressPlay();
            _flow.GoTo(RunStep.TeamSelect);

            for (var i = 0; i < 3; i++)
            {
                _flow.GoTo(RunStep.Home);
                _display.PressPlay();
                _display.PressPlay();
                _flow.GoTo(RunStep.TeamSelect);
            }

            Assert.AreEqual(4, _flow.PlayCalls);
        }

        [Test]
        public void StoppedScreenLetsGoOfRunEvents()
        {
            _presenter.Start();

            _presenter.Stop();

            Assert.IsFalse(_flow.HasListeners);
            Assert.IsFalse(_display.Visible);
        }
    }
}
