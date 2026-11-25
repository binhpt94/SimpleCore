using System;
using System.Collections.Generic;
using Game.Ads;
using Game.Run;
using Game.Services.Popup;
using Game.SimClock;
using NUnit.Framework;

namespace Game.PopupRevive.Tests
{
    public class RevivePopupTests
    {
        private List<string> _log;
        private FakeRun _run;
        private FakeAds _ads;
        private FakePopups _popups;
        private FakeView _view;
        private RevivePopupPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _run = new FakeRun(_log);
            _ads = new FakeAds(_log);
            _view = new FakeView();
            _popups = new FakePopups(_log, _view);
            _clock = new FakeClock(_log);
            _presenter = new RevivePopupPresenter(_run, _run, _ads, _popups, _popups, _clock);
            _presenter.Start();
        }

        private FakeClock _clock;

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private void Offer() => _run.SetStep(RunStep.ReviveOffer);

        // revive-spec#popup-id

        [Test]
        public void PopupIdIsPopupReviveMain()
        {
            Assert.AreEqual("PopupRevive/Main", PopupReviveIds.Main.Value);
        }

        [Test]
        public void PopupOpensThroughPopupServiceWhenRunOffersRevive()
        {
            Offer();

            Assert.AreEqual(new List<PopupId> { PopupReviveIds.Main }, _popups.Shown);
            Assert.IsTrue(_presenter.IsOpen);
        }

        [Test]
        public void PopupDoesNotOpenWhileRunStillWaitsBeforeOffer()
        {
            _run.SetStep(RunStep.ReviveWait);

            Assert.IsEmpty(_popups.Shown);
        }

        [Test]
        public void PopupDoesNotOpenWhenCastleFallEndsTheRunWithoutOffer()
        {
            _run.SetStep(RunStep.Ending);
            _run.SetStep(RunStep.Lost);

            Assert.IsEmpty(_popups.Shown);
        }

        [Test]
        public void PopupOpensAtStartWhenRunAlreadyOffersRevive()
        {
            _presenter.Dispose();
            _run.SetStep(RunStep.ReviveOffer);
            _popups.Shown.Clear();

            _presenter = new RevivePopupPresenter(_run, _run, _ads, _popups, _popups, _clock);
            _presenter.Start();

            Assert.AreEqual(1, _popups.Shown.Count);
        }

        // revive-spec#title-text, #gain-text

        [Test]
        public void TitleSaysCastleWasDestroyed()
        {
            Offer();

            Assert.AreEqual("Thành đã bị phá", _view.Title);
        }

        [Test]
        public void GainLineSaysCastleGetsThirtyPercentAndPlayContinues()
        {
            Offer();

            Assert.AreEqual("Xem quảng cáo để thành hồi 30% máu và chơi tiếp", _view.Gain);
        }

        [Test]
        public void ButtonsSayWatchAdAndEndRun()
        {
            Offer();

            Assert.AreEqual("Xem quảng cáo", _view.Watch);
            Assert.AreEqual("Kết thúc lượt", _view.End);
        }

        // revive-spec#wait-state

        [Test]
        public void PopupOpensWaitingForChoiceWithWaitLineHidden()
        {
            Offer();

            Assert.IsFalse(_view.Waiting);
            Assert.AreEqual("Đang chờ quảng cáo...", _view.WaitingText);
        }

        [Test]
        public void WatchTapShowsWaitLineAndLocksBothButtons()
        {
            Offer();

            _view.TapWatch();

            Assert.IsTrue(_view.Waiting);
            Assert.IsTrue(_presenter.IsWaitingAd);
        }

        [Test]
        public void WatchTapSendsExactlyOneAdRequestWithAnOfferId()
        {
            Offer();

            _view.TapWatch();

            Assert.AreEqual(1, _ads.Requests.Count);
            Assert.AreEqual(_presenter.CurrentOfferId, _ads.Requests[0]);
            Assert.Greater(_ads.Requests[0], 0);
        }

        [Test]
        public void ResultClosesPopupAtOnceWithoutGoingBackToChoosing()
        {
            Offer();
            _view.TapWatch();

            _ads.Reply(AdResult.Watched);

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _popups.Closed.Count);
        }

        // revive-spec#one-tap-each, #edge-cham-hai-nut-gan-nhu-cung-luc

        [Test]
        public void TapsWhileWaitingForAdDoNothing()
        {
            Offer();
            _view.TapWatch();

            _view.TapWatch();
            _view.TapEnd();
            _clock.Step();

            Assert.AreEqual(1, _ads.Requests.Count);
            Assert.IsEmpty(_run.Calls);
            Assert.IsEmpty(_popups.Closed);
        }

        [Test]
        public void EndTapThenWatchTapInSameMomentOnlyEndsRun()
        {
            Offer();

            _view.TapEnd();
            _view.TapWatch();
            _clock.Step();

            Assert.AreEqual(new List<string> { "DeclineRevive" }, _run.Calls);
            Assert.IsEmpty(_ads.Requests);
        }

        [Test]
        public void WatchTapThenEndTapInSameMomentOnlyRequestsAd()
        {
            Offer();

            _view.TapWatch();
            _view.TapEnd();
            _clock.Step();

            Assert.AreEqual(1, _ads.Requests.Count);
            Assert.IsEmpty(_run.Calls);
        }

        // Choice reported to the run, popup closed first (revive-popup; order-values popup side).
        // The report goes through the shared clock's queue (feature-sim-clock#revive-result-queued).

        [Test]
        public void EndTapClosesPopupThenReportsNoRevive()
        {
            Offer();

            _view.TapEnd();
            _clock.Step();

            Assert.AreEqual(
                new List<string> { "Close PopupRevive/Main", "Clock.Enqueue PopupRevive", "Clock.Step", "Run.DeclineRevive" },
                Tail(4));
        }

        [Test]
        public void WatchedAdClosesPopupThenReportsRevive()
        {
            Offer();
            _view.TapWatch();

            _ads.Reply(AdResult.Watched);
            _clock.Step();

            Assert.AreEqual(
                new List<string> { "Close PopupRevive/Main", "Clock.Enqueue PopupRevive", "Clock.Step", "Run.Revive" },
                Tail(4));
        }

        [Test]
        public void NotWatchedAdClosesPopupThenReportsFailedRevive()
        {
            Offer();
            _view.TapWatch();

            _ads.Reply(AdResult.NotWatched);
            _clock.Step();

            Assert.AreEqual(
                new List<string> { "Close PopupRevive/Main", "Clock.Enqueue PopupRevive", "Clock.Step", "Run.FailRevive" },
                Tail(4));
        }

        [Test]
        public void ReviveResultReachesTheRunOnlyWhenTheNextStepStarts()
        {
            Offer();
            _view.TapWatch();

            _ads.Reply(AdResult.Watched);

            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsEmpty(_run.Calls);
            Assert.AreEqual(new List<string> { RevivePopupPresenter.ClockOwner }, _clock.QueuedOwners);

            _clock.Step();

            Assert.AreEqual(new List<string> { "Revive" }, _run.Calls);
        }

        [Test]
        public void EndChoiceReachesTheRunOnlyWhenTheNextStepStarts()
        {
            Offer();

            _view.TapEnd();

            Assert.IsEmpty(_run.Calls);
            Assert.AreEqual(1, _clock.QueuedOwners.Count);

            _clock.Step();

            Assert.AreEqual(new List<string> { "DeclineRevive" }, _run.Calls);
        }

        [Test]
        public void ReviveResultIsQueuedUnderThePopupReviveOwner()
        {
            Assert.AreEqual("PopupRevive", RevivePopupPresenter.ClockOwner);
        }

        [Test]
        public void BusyAdProviderReplyingInsideTheRequestStillReportsFailedReviveOnce()
        {
            Offer();
            _ads.ReplyAtOnce = AdResult.NotWatched;

            _view.TapWatch();
            _clock.Step();

            Assert.AreEqual(new List<string> { "FailRevive" }, _run.Calls);
            Assert.AreEqual(1, _popups.Closed.Count);
        }

        [Test]
        public void ExactlyOneChoiceIsReportedPerPopup()
        {
            Offer();
            _view.TapWatch();
            _ads.Reply(AdResult.Watched);

            _ads.Reply(AdResult.NotWatched);
            _view.TapEnd();
            _clock.Step();
            _clock.Step();

            Assert.AreEqual(new List<string> { "Revive" }, _run.Calls);
        }

        // revive-spec#edge-ket-qua-toi-khi-popup-da-dong-khong

        [Test]
        public void ResultWithAnOldOfferIdIsIgnored()
        {
            Offer();
            _view.TapWatch();

            _ads.ReplyWith(_presenter.CurrentOfferId + 1, AdResult.Watched);
            _clock.Step();

            Assert.IsEmpty(_run.Calls);
            Assert.IsTrue(_presenter.IsWaitingAd);
        }

        [Test]
        public void ResultAfterRunLeftTheOfferIsDroppedAndRequestCancelled()
        {
            Offer();
            _view.TapWatch();
            var offerId = _presenter.CurrentOfferId;

            _run.SetStep(RunStep.Home);
            _ads.ReplyWith(offerId, AdResult.Watched);
            _clock.Step();

            Assert.AreEqual(new List<int> { offerId }, _ads.Cancelled);
            Assert.IsEmpty(_run.Calls);
            Assert.IsFalse(_presenter.IsOpen);
        }

        // revive-spec#edge-roi-game-khi-dang-cho-quang-cao, #edge-roi-game-khi-dang-cho-chon

        [Test]
        public void PopupKeepsWaitingForAdUntilResultArrivesHoweverLong()
        {
            Offer();
            _view.TapWatch();

            // Leaving the app: the run keeps the step (pause is not opened during the offer), no reply yet.
            _run.SetStep(RunStep.ReviveOffer);

            Assert.IsTrue(_presenter.IsWaitingAd);
            Assert.IsTrue(_view.Waiting);
            Assert.AreEqual(1, _popups.Shown.Count);
            Assert.IsEmpty(_popups.Closed);
        }

        [Test]
        public void PopupStaysAsItWasWhileWaitingForChoice()
        {
            Offer();

            _run.SetStep(RunStep.ReviveOffer);

            Assert.IsTrue(_presenter.IsOpen);
            Assert.IsFalse(_view.Waiting);
            Assert.AreEqual(1, _popups.Shown.Count);
            Assert.IsEmpty(_popups.Closed);
            Assert.IsEmpty(_run.Calls);
        }

        // Session end

        [Test]
        public void SessionEndClosesPopupAndCancelsAdWithoutReporting()
        {
            Offer();
            _view.TapWatch();

            _presenter.Dispose();
            _clock.Step();

            Assert.AreEqual(1, _ads.Cancelled.Count);
            Assert.AreEqual(1, _popups.Closed.Count);
            Assert.IsEmpty(_run.Calls);
        }

        // order-values, popup side: the run owns the timeline, the popup only follows its steps.

        [Test]
        public void PopupOpensOnceWhenTheOfferComesAfterTheWait()
        {
            _run.SetStep(RunStep.ReviveWait);
            Assert.IsEmpty(_popups.Shown);

            _run.SetStep(RunStep.ReviveOffer);

            Assert.AreEqual(1, _popups.Shown.Count);
            Assert.IsTrue(_presenter.IsOpen);
        }

        [Test]
        public void PopupDoesNotOpenAgainWhileCastleStandsUpOrWhenBattleRunsAgain()
        {
            Offer();
            _view.TapWatch();
            _ads.Reply(AdResult.Watched);
            _clock.Step();

            _run.SetStep(RunStep.Reviving);
            _run.SetStep(RunStep.Playing);

            Assert.AreEqual(1, _popups.Shown.Count);
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(new List<string> { "Revive" }, _run.Calls);
        }

        [Test]
        public void SecondCastleFallAfterReviveGoesToLoseWithoutRevivePopup()
        {
            Offer();
            _view.TapWatch();
            _ads.Reply(AdResult.Watched);
            _clock.Step();
            _run.SetStep(RunStep.Reviving);
            _run.SetStep(RunStep.Playing);

            _run.SetStep(RunStep.Ending);
            _run.SetStep(RunStep.Lost);

            Assert.AreEqual(1, _popups.Shown.Count);
            Assert.IsFalse(_presenter.IsOpen);
        }

        [Test]
        public void DeclineReachesTheRunAtTheFirstStepAfterThePopupClosed()
        {
            Offer();
            _view.TapEnd();

            _clock.Step();

            Assert.AreEqual(1, _clock.StepCount);
            Assert.AreEqual(new List<string> { "DeclineRevive" }, _run.Calls);
        }

        [Test]
        public void PopupWithoutViewIsClosedAndNamedInTheError()
        {
            _popups.View = null;

            var error = Assert.Throws<InvalidOperationException>(Offer);

            StringAssert.Contains("PopupRevive/Main", error.Message);
            Assert.AreEqual(1, _popups.Closed.Count);
        }

        private List<string> Tail(int count) => _log.GetRange(_log.Count - count, count);

        // ---- fakes ----

        private sealed class FakeRun : IRunFlow, IRunRevive
        {
            private readonly List<string> _log;

            public FakeRun(List<string> log) => _log = log;

            public List<string> Calls { get; } = new List<string>();

            public RunStep Step { get; private set; } = RunStep.Playing;

            public event Action<RunStep> StepChanged;

            public void SetStep(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public RunTeam Team => default;
            public bool IsPaused => Step != RunStep.Playing;
            public bool LostAfterFailedRevive => false;

            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }
            public event Action ReviveStarted { add { } remove { } }

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
            public void ReviveShown() { }

            public void Revive() => Record("Revive");
            public void DeclineRevive() => Record("DeclineRevive");
            public void FailRevive() => Record("FailRevive");

            private void Record(string call)
            {
                Calls.Add(call);
                _log.Add("Run." + call);
            }
        }

        /// <summary>Holds queued actions until the test runs the next step, like the shared clock.</summary>
        private sealed class FakeClock : ISimClock
        {
            private readonly List<string> _log;
            private readonly List<Action> _queue = new List<Action>();

            public FakeClock(List<string> log) => _log = log;

            public List<string> QueuedOwners { get; } = new List<string>();

            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount => 0;
            public double GameplaySeconds => 0;

            public IDisposable OnStep(string owner, Action<SimStep> onStep) =>
                throw new InvalidOperationException("The revive popup does not step.");

            public void Enqueue(string owner, Action apply)
            {
                QueuedOwners.Add(owner);
                _queue.Add(apply);
                _log.Add("Clock.Enqueue " + owner);
            }

            public void Step()
            {
                StepCount++;
                _log.Add("Clock.Step");
                var queued = _queue.ToArray();
                _queue.Clear();
                QueuedOwners.Clear();
                foreach (var apply in queued) apply();
            }
        }

        private sealed class FakeAds : IRewardedAds
        {
            private readonly List<string> _log;
            private Action<AdReply> _onReply;

            public FakeAds(List<string> log) => _log = log;

            public List<int> Requests { get; } = new List<int>();
            public List<int> Cancelled { get; } = new List<int>();
            public AdResult? ReplyAtOnce { get; set; }

            public void Show(int offerId, Action<AdReply> onReply)
            {
                Requests.Add(offerId);
                _log.Add("Ads.Show " + offerId);
                _onReply = onReply;
                if (ReplyAtOnce.HasValue) onReply(new AdReply(offerId, ReplyAtOnce.Value));
            }

            public void Cancel(int offerId) => Cancelled.Add(offerId);

            public void Reply(AdResult result)
            {
                if (Requests.Count == 0) return;
                ReplyWith(Requests[Requests.Count - 1], result);
            }

            public void ReplyWith(int offerId, AdResult result) => _onReply?.Invoke(new AdReply(offerId, result));
        }

        private sealed class FakePopups : IPopupService, IPopupViews
        {
            private readonly List<string> _log;
            private readonly HashSet<int> _open = new HashSet<int>();
            private readonly Dictionary<int, PopupId> _ids = new Dictionary<int, PopupId>();
            private int _next;

            public FakePopups(List<string> log, FakeView view)
            {
                _log = log;
                View = view;
            }

            public FakeView View { get; set; }
            public List<PopupId> Shown { get; } = new List<PopupId>();
            public List<PopupHandle> Closed { get; } = new List<PopupHandle>();

            public PopupHandle Show(PopupId id)
            {
                Shown.Add(id);
                _log.Add("Show " + id);
                var handle = new PopupHandle(++_next);
                _open.Add(handle.Id);
                _ids[handle.Id] = id;
                return handle;
            }

            public void Close(PopupHandle handle)
            {
                if (!_open.Remove(handle.Id)) return;
                Closed.Add(handle);
                _log.Add("Close " + _ids[handle.Id]);
            }

            public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
            {
                view = _open.Contains(handle.Id) ? View as T : null;
                return view != null;
            }

            public bool IsOnTop(PopupHandle handle) => _open.Contains(handle.Id);
        }

        private sealed class FakeView : IRevivePopupView
        {
            public event Action WatchTapped;
            public event Action EndTapped;

            public string Title { get; private set; }
            public string Gain { get; private set; }
            public string WaitingText { get; private set; }
            public string Watch { get; private set; }
            public string End { get; private set; }
            public bool Waiting { get; private set; }

            public void SetTexts(string title, string gain, string waiting, string watch, string end)
            {
                Title = title;
                Gain = gain;
                WaitingText = waiting;
                Watch = watch;
                End = end;
            }

            public void SetWaiting(bool waiting) => Waiting = waiting;

            // Raises even while locked, so the presenter's own one-tap guard is what is tested.
            public void TapWatch() => WatchTapped?.Invoke();

            public void TapEnd() => EndTapped?.Invoke();
        }
    }
}
