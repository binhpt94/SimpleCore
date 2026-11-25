using System;
using System.Collections.Generic;
using System.Threading;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Battlefield.Tests
{
    public class BattlefieldParticipantTests
    {
        private FakeRunFlow _run;
        private FakeViews _views;
        private BattlefieldParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRunFlow();
            _views = new FakeViews();
            _participant = new BattlefieldParticipant(_run, new BackdropLayout(BattlefieldTestAssets.Config()), _views);
            _participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Test]
        public void BattleSetUp_ShowsTheBackdropOnce_WithAllLayers()
        {
            _participant.Run();
            _run.Step = RunStep.Playing;
            _run.RaiseBattleStarted();
            _run.RaiseBattleStarted();

            Assert.That(_views.Created, Is.EqualTo(1));
            Assert.That(_views.Alive, Is.EqualTo(1));
            // sky, 2 far strips, 8 standing decor, ground, 10 flat decor
            Assert.That(_views.Last.Pieces.Count, Is.EqualTo(22));
        }

        [Test]
        public void SessionRunningMidBattle_ShowsTheBackdropAtOnce()
        {
            _run.Step = RunStep.Playing;

            _participant.Run();

            Assert.That(_views.Alive, Is.EqualTo(1));
        }

        [Test]
        public void HomeScreen_HasNoBackdrop()
        {
            _participant.Run();

            Assert.That(_views.Created, Is.EqualTo(0));
        }

        [Test]
        public void PauseCastleDestroyedAndRevive_LeaveTheBackdropUntouched()
        {
            _participant.Run();
            _run.RaiseBattleStarted();
            var view = _views.Last;

            _run.SetStep(RunStep.Paused);
            _run.SetStep(RunStep.Playing);
            _run.SetStep(RunStep.Ending);
            _run.RaiseBattleStopped();
            _run.SetStep(RunStep.Playing);

            Assert.That(_views.Created, Is.EqualTo(1));
            Assert.That(_views.Alive, Is.EqualTo(1));
            Assert.That(view.ShowCalls, Is.EqualTo(1));
        }

        [Test]
        public void LeavingTheBattle_RemovesTheBackdrop()
        {
            _participant.Run();
            _run.RaiseBattleStarted();

            _run.RaiseBattleCleared();

            Assert.That(_views.Alive, Is.EqualTo(0));
        }

        [Test]
        public void LeavingAndEnteringAgain_BuildsTheSameBackdrop()
        {
            _participant.Run();
            _run.RaiseBattleStarted();
            var first = _views.Last.Pieces;

            _run.RaiseBattleCleared();
            _run.RaiseBattleStarted();
            var second = _views.Last.Pieces;

            Assert.That(_views.Created, Is.EqualTo(2));
            Assert.That(_views.Alive, Is.EqualTo(1));
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
                Assert.That(second[i].Left, Is.EqualTo(first[i].Left));
                Assert.That(second[i].Bottom, Is.EqualTo(first[i].Bottom));
                Assert.That(second[i].Width, Is.EqualTo(first[i].Width));
                Assert.That(second[i].Height, Is.EqualTo(first[i].Height));
            }
        }

        [Test]
        public void SessionEnd_RemovesTheBackdrop_AndStopsListening()
        {
            _participant.Run();
            _run.RaiseBattleStarted();

            _participant.DisposeAsync().GetAwaiter().GetResult();
            _run.RaiseBattleStarted();

            Assert.That(_views.Alive, Is.EqualTo(0));
            Assert.That(_views.Created, Is.EqualTo(1));
        }

        [Test]
        public void ShownBackdrop_IsCountedAsAHeldView_AndNoJob()
        {
            _participant.Run();
            _run.RaiseBattleStarted();

            Assert.That(Usage.HeldViews, Is.EqualTo(1));
            Assert.That(Usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void LeavingTheBattle_ReleasesTheHeldView()
        {
            _participant.Run();
            _run.RaiseBattleStarted();

            _run.RaiseBattleCleared();

            Assert.That(Usage.HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void ShownThenSessionEnd_LeavesNoViewAndNoJob()
        {
            _participant.Run();
            _run.RaiseBattleStarted();
            _run.RaiseBattleCleared();
            _run.RaiseBattleStarted();

            _participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(Usage.HeldViews, Is.EqualTo(0));
            Assert.That(Usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void SessionStartingOnTheHomeScreen_ReportsNoViewOnRun_AndHoldsNone()
        {
            _participant.Run();

            Assert.That(Usage.OpensViewsOnRun, Is.False);
            Assert.That(Usage.HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void SessionRunningMidBattle_HoldsTheBackdropView()
        {
            _run.Step = RunStep.Playing;

            _participant.Run();

            Assert.That(Usage.HeldViews, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown() => _views.DestroyAll();

        private ISessionParticipantUsage Usage => _participant;

        private sealed class FakeView : IBackdropView
        {
            public readonly GameObject Object = new GameObject("FakeBackdrop");
            public IReadOnlyList<BackdropPiece> Pieces = Array.Empty<BackdropPiece>();
            public int ShowCalls;

            public UnityEngine.Object Root => Object;

            public void Show(IReadOnlyList<BackdropPiece> pieces)
            {
                Pieces = pieces;
                ShowCalls++;
            }
        }

        private sealed class FakeViews : IBackdropViewFactory
        {
            public int Created;
            public int Alive;
            public FakeView Last;
            private readonly List<FakeView> _all = new List<FakeView>();

            public IBackdropView Create()
            {
                Created++;
                Alive++;
                Last = new FakeView();
                _all.Add(Last);
                return Last;
            }

            // Only counts: the released handle, not a destroyed object, must bring the held views to 0.
            public void Destroy(IBackdropView view) => Alive--;

            public void DestroyAll()
            {
                foreach (var view in _all) UnityEngine.Object.DestroyImmediate(view.Object);
                _all.Clear();
            }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            public RunStep Step { get; set; } = RunStep.Home;
            public event Action<RunStep> StepChanged;
            public RunTeam Team => default;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
            public bool IsPaused => Step == RunStep.Paused;

            public void SetStep(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void RaiseBattleStarted()
            {
                RunStarted?.Invoke(default);
                BattleStarted?.Invoke();
            }

            public void RaiseBattleStopped() => BattleStopped?.Invoke();
            public void RaiseBattleCleared() => BattleCleared?.Invoke();

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
