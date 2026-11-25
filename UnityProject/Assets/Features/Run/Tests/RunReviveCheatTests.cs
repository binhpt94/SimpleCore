using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Ads;
using Game.Castle;
using Game.Core;
using Game.Services.Popup;
using Game.SimClock;
using Game.Wave;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.Run.Tests
{
    /// <summary>
    /// Cheat state feature-popup-revive/cheat#waiting-ad, built by the run: battle at wave 8, the castle has just
    /// fallen for the first time, the revive popup waits for the ad. Wave, Ads and Castle are fakes.
    /// </summary>
    public class RunReviveCheatTests
    {
        private const string InstallerPath = "Assets/Features/Run/FeatureAssets/Public/RunInstaller.asset";
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axeman", "archer" });

        private List<string> _calls;
        private FakeSimClock _clock;
        private FakeFrames _frames;
        private SimClockRunDelay _waits;
        private RunFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _calls = new List<string>();
            _clock = new FakeSimClock();
            _frames = new FakeFrames();
            _waits = new SimClockRunDelay(_clock);
        }

        private RunFlow Flow(bool reviveOnce = true) =>
            new RunFlow(new RunScore(new FakeStore()), _waits, 1f, 3, reviveOnce: reviveOnce, pauseEnabled: true,
                clock: new SimRunClock(_clock, _clock));

        /// <summary>The run's session and the cheat, as the installer joins them, run; the next frame comes.</summary>
        private (RunSession session, ReviveWaitingAdCheat cheat) StartedSessionWithCheat(Func<IWaveCheat> wave = null)
        {
            var session = new RunSession(_flow, new RunScore(new FakeStore()), _frames, clockWaits: _waits);
            var castle = new FakeCastle(_calls, () => _flow.Lose());
            var cheat = new ReviveWaitingAdCheat(_flow, _clock, _frames, Team,
                wave ?? (() => new FakeWave(_calls)), () => new FakeAds(_calls), () => castle);
            session.LoadAsync(CancellationToken.None).Forget();
            session.Run();
            cheat.Run();
            _frames.NextFrameArrives();
            return (session, cheat);
        }

        [Test]
        public void WaitingAd_GoesIntoBattleFromTheStartScreenWithTheDefaultTeam()
        {
            _flow = Flow();

            StartedSessionWithCheat();

            Assert.AreEqual(RunStep.Playing, _flow.Step);
            CollectionAssert.AreEqual(Team.HeroCodes, _flow.Team.HeroCodes);
            CollectionAssert.IsEmpty(_calls, "Nothing is forced before the first gameplay step.");
        }

        [Test]
        public void WaitingAd_OnTheFirstGameplayStepStartsWave8HoldsTheAdThenFellsTheCastle()
        {
            _flow = Flow();
            StartedSessionWithCheat();

            _clock.Step(1);

            CollectionAssert.AreEqual(new[] { "StartWave:8", "HoldNextAd", "Fall" }, _calls);
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step, "The run's own rules: the first fall offers the revive.");
            Assert.IsTrue(_flow.ReviveOffered);
            Assert.IsFalse(_clock.Gameplay, "The battle stands still at wave 8.");
        }

        [Test]
        public void WaitingAd_TheRevivePopupStepComesAfterTheUsualOneSecond()
        {
            _flow = Flow();
            StartedSessionWithCheat();
            _clock.Step(1);

            _clock.Step(40);

            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step, "The revive popup opens, and its ad request is held.");
        }

        [Test]
        public void WaitingAd_IsBuiltOncePerSession()
        {
            _flow = Flow();
            StartedSessionWithCheat();
            _clock.Step(41);
            _flow.DeclineRevive();
            _flow.Replay();
            _flow.BuildBattle(Team);

            _clock.Step(5);

            Assert.AreEqual(1, _calls.FindAll(c => c == "Fall").Count);
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void WaitingAd_MissingWaveCheatFailsWhenTheSessionRunsNamingIt()
        {
            _flow = Flow();
            var cheat = new ReviveWaitingAdCheat(_flow, _clock, _frames, Team,
                () => throw new InvalidOperationException($"No {nameof(IWaveCheat)} in the registry."),
                () => new FakeAds(_calls), () => new FakeCastle(_calls, () => { }));

            var error = Assert.Throws<InvalidOperationException>(() => cheat.Run());
            StringAssert.Contains(nameof(IWaveCheat), error.Message);
        }

        [Test]
        public void WaitingAd_NeedsTheRunToOfferTheRevive()
        {
            _flow = Flow(reviveOnce: false);
            var cheat = new ReviveWaitingAdCheat(_flow, _clock, _frames, Team,
                () => new FakeWave(_calls), () => new FakeAds(_calls), () => new FakeCastle(_calls, () => { }));

            var error = Assert.Throws<InvalidOperationException>(() => cheat.Run());
            StringAssert.Contains("reviveOnce", error.Message);
        }

        [Test]
        public void WaitingAd_CountsItsJobUntilItIsBuiltAndNothingAfterTheSessionEnds()
        {
            _flow = Flow();
            var (session, cheat) = StartedSessionWithCheat();
            Assert.AreEqual(1, ((ISessionParticipantUsage)cheat).RunningJobs);

            _clock.Step(1);
            Assert.AreEqual(0, ((ISessionParticipantUsage)cheat).RunningJobs);

            cheat.DisposeAsync().Forget();
            session.DisposeAsync().Forget();
            Assert.AreEqual(0, ((ISessionParticipantUsage)cheat).RunningJobs);
            Assert.AreEqual(0, ((ISessionParticipantUsage)session).RunningJobs);
            Assert.AreEqual(0, _clock.Listening);
        }

        [Test]
        public void Params_RealInstallerAssetHasTheWaitingAdCheatOff()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");
            Assert.IsFalse(installer.CheatWaitingAd);
        }

        [Test]
        public void Params_InstallerWithTheWaitingAdCheatOnJoinsItToTheSession()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var installer = UnityEngine.Object.Instantiate(asset);
            try
            {
                var serialized = new SerializedObject(installer);
                serialized.FindProperty("cheatWaitingAd").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var session = new FakeSession();
                var registry = new ServiceRegistry();
                registry.Register<ISessionScene>(session);
                registry.Register<IPopupStack>(new FakePopupStack());
                registry.Register<ISimClock>(_clock);
                registry.Register<ISimRunControl>(_clock);
                installer.Install(registry);

                Assert.AreEqual(3, session.Joined.Count, "The run, its leftover popup check and the cheat.");
                Assert.IsInstanceOf<ReviveWaitingAdCheat>(session.Joined[2]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        private sealed class FakeWave : IWaveCheat
        {
            private readonly List<string> _calls;
            public FakeWave(List<string> calls) => _calls = calls;
            public void StartWave(int wave) => _calls.Add("StartWave:" + wave);
        }

        private sealed class FakeAds : IAdsCheat
        {
            private readonly List<string> _calls;
            public FakeAds(List<string> calls) => _calls = calls;
            public void HoldNextAd() => _calls.Add("HoldNextAd");
        }

        /// <summary>The castle's fall cheat: reports the fall to the run once, like a real fall.</summary>
        private sealed class FakeCastle : ICastleFallCheat
        {
            private readonly List<string> _calls;
            private readonly Action _reportFall;
            public FakeCastle(List<string> calls, Action reportFall) { _calls = calls; _reportFall = reportFall; }

            public void Fall()
            {
                _calls.Add("Fall");
                _reportFall();
            }
        }

        /// <summary>The shared clock, faked: subscribers called in the order they joined, every step 0.025 s.</summary>
        private sealed class FakeSimClock : ISimClock, ISimRunControl
        {
            private readonly List<Action<SimStep>> _onStep = new List<Action<SimStep>>();
            private readonly List<Action> _queue = new List<Action>();

            public bool Gameplay { get; private set; }
            public int Listening => _onStep.Count;
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * 0.025;
            public int Seed => 9;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                _onStep.Add(onStep);
                return new Cancel(() => _onStep.Remove(onStep));
            }

            public void Enqueue(string owner, Action apply) => _queue.Add(apply);

            public void Step(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    StepCount++;
                    var queued = _queue.ToArray();
                    _queue.Clear();
                    foreach (var apply in queued) apply();
                    if (Gameplay) GameplayStepCount++;
                    var step = new SimStep(StepCount, GameplayStepCount, 0.025f, Gameplay);
                    foreach (var onStep in _onStep.ToArray()) onStep(step);
                }
            }

            public void SetUpRun() { StepCount = 0; GameplayStepCount = 0; _queue.Clear(); }
            public void StartRun() { }
            public void SetGameplay(bool playing) => Gameplay = playing;
            public void ClearRun() => _queue.Clear();
        }

        private sealed class FakeFrames : IRunDelay
        {
            private readonly List<Action> _next = new List<Action>();

            public IDisposable Schedule(float seconds, Action then) => throw new NotSupportedException();

            public IDisposable NextFrame(Action then)
            {
                _next.Add(then);
                return new Cancel(() => _next.Remove(then));
            }

            public void NextFrameArrives()
            {
                var due = _next.ToArray();
                _next.Clear();
                foreach (var then in due) then();
            }
        }

        private sealed class Cancel : IDisposable
        {
            private Action _undo;
            public Cancel(Action undo) => _undo = undo;
            public void Dispose() { _undo?.Invoke(); _undo = null; }
        }

        private sealed class FakePopupStack : IPopupStack
        {
            public IReadOnlyList<PopupId> OpenIds => Array.Empty<PopupId>();
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int Load() => 0;
            public void Save(int best) { }
        }
    }
}
