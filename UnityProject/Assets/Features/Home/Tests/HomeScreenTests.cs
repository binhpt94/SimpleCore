using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Home.Tests
{
    /// <summary>feature-home/home-screen/home-screen-spec: what the start screen shows and how Play reacts.</summary>
    public sealed class HomeScreenTests
    {
        private HomeConfig _config;
        private FakeRunScore _score;
        private FakeHomeDisplay _display;
        private HomeScreenPresenter _screen;
        private int _playPressed;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<HomeConfig>();
            _score = new FakeRunScore();
            _display = new FakeHomeDisplay();
            _screen = new HomeScreenPresenter(_score, _config);
            _screen.Attach(_display);
            _playPressed = 0;
            _screen.PlayPressed += () => _playPressed++;
        }

        [TearDown]
        public void TearDown()
        {
            _screen.Detach();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TitleLineShowsGameName()
        {
            _screen.Show();

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
            _screen.Show();

            Assert.AreEqual("Kỉ lục: 7 đợt", _display.Content.Record);
        }

        [Test]
        public void RecordLineShowsZeroWhenNoBestYet()
        {
            _score.Best = 0;
            _screen.Show();

            Assert.AreEqual("Kỉ lục: 0 đợt", _display.Content.Record);
        }

        [Test]
        public void VeryLargeRecordShowsEveryDigit()
        {
            _score.Best = 999;
            _screen.Show();
            Assert.AreEqual("Kỉ lục: 999 đợt", _display.Content.Record);

            _score.SetBest(123456);
            Assert.AreEqual("Kỉ lục: 123456 đợt", _display.Content.Record);
        }

        [Test]
        public void PlayButtonSaysChoi()
        {
            _screen.Show();

            Assert.AreEqual("Chơi", _display.Content.PlayLabel);
        }

        [Test]
        public void FirstTapOnPlayReportsPlayPressed()
        {
            _screen.Show();

            _display.PressPlay();

            Assert.AreEqual(1, _playPressed);
        }

        [Test]
        public void MoreTapsWhileLeavingAreIgnored()
        {
            _screen.Show();

            _display.PressPlay();
            _display.PressPlay();
            _display.PressPlay();

            Assert.AreEqual(1, _playPressed);
        }

        [Test]
        public void PlayDoesNotHideOrLeaveTheScreenByItself()
        {
            _screen.Show();

            _display.PressPlay();

            Assert.IsTrue(_display.Visible, "Leaving the start screen is the screen flow's call.");
        }

        [Test]
        public void ScreenHidesWhenTheFlowHidesIt()
        {
            _screen.Show();
            _display.PressPlay();

            _screen.Hide();

            Assert.IsFalse(_display.Visible);
            _display.PressPlay();
            Assert.AreEqual(1, _playPressed);
        }

        [Test]
        public void ScreenStaysHiddenUntilTheFlowShowsIt()
        {
            Assert.IsFalse(_display.Visible);
            Assert.IsFalse(_screen.IsShown);

            _display.PressPlay();
            Assert.AreEqual(0, _playPressed);
        }

        [Test]
        public void ShowAskedBeforeTheViewIsBuiltAppearsWhenItIs()
        {
            var screen = new HomeScreenPresenter(new FakeRunScore { Best = 6 }, _config);
            var display = new FakeHomeDisplay();
            screen.Show();

            screen.Attach(display);

            Assert.IsTrue(display.Visible);
            Assert.AreEqual("Kỉ lục: 6 đợt", display.Content.Record);
            screen.Detach();
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
            _screen.Show();
            _display.PressPlay();
            _screen.Hide();

            _score.SetBest(9, raise: false);
            _screen.Show();

            Assert.IsTrue(_display.Visible);
            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
        }

        [Test]
        public void RecordSavedJustAfterReturningHomeStillShows()
        {
            _score.Best = 4;

            _screen.Show();
            _score.SetBest(9);

            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
        }

        [Test]
        public void ScreenNeverAnnouncesNewBest()
        {
            _score.Best = 9;
            _score.IsNewBest = true;
            _screen.Show();

            Assert.AreEqual("Kỉ lục: 9 đợt", _display.Content.Record);
            Assert.AreEqual("Thủ thành anh hùng", _display.Content.Title);
        }

        [Test]
        public void PlayTakesOneNewTapEachTimeScreenShowsAgain()
        {
            _screen.Show();
            _display.PressPlay();
            _screen.Hide();

            for (var i = 0; i < 3; i++)
            {
                _screen.Show();
                _display.PressPlay();
                _display.PressPlay();
                _screen.Hide();
            }

            Assert.AreEqual(4, _playPressed);
        }

        [Test]
        public void HiddenScreenLetsGoOfScoreEvents()
        {
            _screen.Show();

            _screen.Hide();

            Assert.IsFalse(_score.HasListeners);
            Assert.IsFalse(_display.Visible);
        }

        [Test]
        public void DetachedScreenIgnoresTapsOnTheOldView()
        {
            _screen.Show();

            _screen.Detach();
            _display.PressPlay();

            Assert.AreEqual(0, _playPressed);
            Assert.IsFalse(_score.HasListeners);
        }
    }
}
