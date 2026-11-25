using System;
using System.Collections.Generic;
using System.Threading;
using Game.Run;
using NUnit.Framework;

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
            Assert.That(_views.Last.Pieces.Count, Is.EqualTo(13));
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

        private sealed class FakeView : IBackdropView
        {
            public IReadOnlyList<BackdropPiece> Pieces = Array.Empty<BackdropPiece>();
            public int ShowCalls;

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

            public IBackdropView Create()
            {
                Created++;
                Alive++;
                Last = new FakeView();
                return Last;
            }

            public void Destroy(IBackdropView view) => Alive--;
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
