using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.SimClock;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// Proposal 2026-11-25-session-usage-tracking: the run reports its running jobs (clock listening, waits before
    /// the lose popup, the begin frame, the leftover popup check) and every count is 0 after the session ends.
    /// </summary>
    public class RunUsageTrackingTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeSimClock _clock;
        private FakeFrames _frames;
        private SimClockRunDelay _waits;
        private RunScore _score;
        private RunFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeSimClock();
            _frames = new FakeFrames();
            _waits = new SimClockRunDelay(_clock);
            _score = new RunScore(new FakeStore());
            _flow = new RunFlow(_score, _waits, 1f, 3, clock: new SimRunClock(_clock, _clock));
        }

        private static int Jobs(ISessionParticipant participant) => ((ISessionParticipantUsage)participant).RunningJobs;

        private static int Views(ISessionParticipant participant) => ((ISessionParticipantUsage)participant).HeldViews;

        private RunSession StartedSession()
        {
            var session = new RunSession(_flow, _score, _frames, clockWaits: _waits);
            session.LoadAsync(CancellationToken.None).Forget();
            session.Run();
            return session;
        }

        [Test]
        public void RunSessionCountsListeningToTheClockAndTheBeginWaitThenNothingAfterTheSessionEnds()
        {
            var session = StartedSession();
            Assert.AreEqual(2, Jobs(session), "Listening to the clock and waiting for the frame the run begins on.");

            _frames.NextFrameArrives();
            Assert.AreEqual(1, Jobs(session), "The begin wait is done; only the clock listening is left.");

            session.DisposeAsync().Forget();
            Assert.AreEqual(0, Jobs(session));
            Assert.AreEqual(0, _clock.Listening, "The run stopped listening to the clock.");
        }

        [Test]
        public void PendingLosePopupWaitIsCountedUntilItRuns()
        {
            var session = StartedSession();
            _frames.NextFrameArrives();
            _flow.Play();
            _flow.EnterBattle(Team);

            _flow.Lose();
            Assert.AreEqual(2, Jobs(session), "Clock listening plus the wait before the lose popup.");

            _clock.Step(40);
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            Assert.AreEqual(1, Jobs(session), "The wait ran and is no longer counted.");

            session.DisposeAsync().Forget();
            Assert.AreEqual(0, Jobs(session));
        }

        [Test]
        public void EndingTheSessionDuringTheLosePopupWaitDropsEveryJob()
        {
            var session = StartedSession();
            _frames.NextFrameArrives();
            _flow.Play();
            _flow.EnterBattle(Team);
            _flow.CountClearedWave(1);
            _flow.Lose();

            session.DisposeAsync().Forget();

            Assert.AreEqual(0, Jobs(session));
            Assert.AreEqual(0, Views(session), "The run holds no view.");
        }

        [Test]
        public void EndingTheSessionBeforeTheRunBeginsDropsTheBeginWait()
        {
            var session = StartedSession();

            session.DisposeAsync().Forget();

            Assert.AreEqual(0, Jobs(session));
            Assert.AreEqual(RunStep.Home, _flow.Step);
            _frames.NextFrameArrives();
            Assert.AreEqual(RunStep.Home, _flow.Step, "The dropped begin wait does nothing.");
        }

        [Test]
        public void LeftoverPopupCheckCountsItsWaitUntilTheCheckRunsAndNothingAfterTheSessionEnds()
        {
            var check = new LeftoverPopupCheck(_flow, () => Array.Empty<string>(), _frames, new FakeLog());
            check.Run();
            Assert.AreEqual(0, Jobs(check));

            _flow.Play();
            _flow.EnterBattle(Team);
            Assert.AreEqual(1, Jobs(check), "Waiting for the frame after the battle starts.");

            _frames.NextFrameArrives();
            Assert.AreEqual(0, Jobs(check));

            _flow.Lose();
            _clock.Step(40);
            _flow.Replay();
            _flow.BuildBattle(Team);
            Assert.AreEqual(1, Jobs(check));

            check.DisposeAsync().Forget();
            Assert.AreEqual(0, Jobs(check));
        }

        [Test]
        public void ReplayChecksStagePrototype_TenRunsInARowLeaveTheRunHoldingNoMoreThanAtTheStartOfTheFirst()
        {
            // feature-run/run-start/replay-checks#stage-prototype: 10 runs in a row in the Game scene; at the start of
            // run 10 the run holds the same jobs, clock listeners and queued actions as at the start of run 1.
            // Each run clears a wave, loses after the 1 s wait and plays again.
            var session = StartedSession();
            var check = new LeftoverPopupCheck(_flow, () => Array.Empty<string>(), _frames, new FakeLog());
            check.Run();
            _frames.NextFrameArrives();
            var battle = (IRunBattle)_flow;
            var playAgainHeard = 0;
            battle.BattleLeft += choice => { if (choice == RunLeaveChoice.PlayAgain) playAgainHeard++; };

            var atStart = new List<(int jobs, int checkJobs, int listening, int queued)>();
            for (var run = 1; run <= 10; run++)
            {
                battle.BuildBattle(Team);
                _frames.NextFrameArrives();
                atStart.Add((Jobs(session), Jobs(check), _clock.Listening, _clock.QueuedCount));

                _flow.CountClearedWave(1);
                _clock.Step(1);
                _flow.Lose();
                _clock.Step(40);
                Assert.AreEqual(RunStep.Lost, _flow.Step, $"Run {run} reached the lose popup.");

                var heardBefore = playAgainHeard;
                _flow.Replay();
                Assert.AreEqual(heardBefore + 1, playAgainHeard,
                    $"Run {run}: Play again reaches the screen flow within the same call, well under 0.5 s.");
            }

            Assert.AreEqual(atStart[0], atStart[9], "Start of run 10 holds exactly what the start of run 1 held.");
            Assert.AreEqual(1, atStart[0].jobs, "Only the clock listening runs at the start of a run.");
            Assert.AreEqual(0, atStart[0].queued, "No queued action is left over.");

            check.DisposeAsync().Forget();
            session.DisposeAsync().Forget();
            Assert.AreEqual(0, Jobs(session));
            Assert.AreEqual(0, Jobs(check));
            Assert.AreEqual(0, _clock.Listening);
        }

        /// <summary>The shared clock of feature-sim-clock, faked: every step 0.025 s, step count goes up before the callbacks.</summary>
        private sealed class FakeSimClock : ISimClock, ISimRunControl
        {
            private readonly List<Action<SimStep>> _onStep = new List<Action<SimStep>>();
            private readonly List<Action> _queue = new List<Action>();
            private bool _gameplay;

            public int Listening => _onStep.Count;
            public int QueuedCount => _queue.Count;
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * 0.025;
            public int Seed => 3;

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
                    if (_gameplay) GameplayStepCount++;
                    var step = new SimStep(StepCount, GameplayStepCount, 0.025f, _gameplay);
                    foreach (var onStep in _onStep.ToArray()) onStep(step);
                }
            }

            public void SetUpRun() { StepCount = 0; GameplayStepCount = 0; _queue.Clear(); }
            public void StartRun() { }
            public void SetGameplay(bool playing) => _gameplay = playing;
            public void ClearRun() => _queue.Clear();
        }

        /// <summary>Frame waits, faked: the next frame comes when the test says so.</summary>
        private sealed class FakeFrames : IRunDelay
        {
            private readonly List<Action> _next = new List<Action>();

            public IDisposable Schedule(float seconds, Action then) => throw new NotSupportedException("The run waits on the clock, not on frames.");

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
            private Action _cancel;
            public Cancel(Action cancel) => _cancel = cancel;
            public void Dispose() { _cancel?.Invoke(); _cancel = null; }
        }

        private sealed class FakeLog : IRunLog
        {
            public void Error(string message) { }
        }

        /// <summary>feature-save, faked: nothing saved yet, every write succeeds.</summary>
        private sealed class FakeStore : Game.Save.ISavedRecord
        {
            public bool TryRead(out int record)
            {
                record = 0;
                return false;
            }

            public bool Write(int record) => true;
        }
    }
}
