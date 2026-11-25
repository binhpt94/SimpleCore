using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;

namespace Game.Home.Tests
{
    /// <summary>Stand-in for the run's flow: the test moves the step and counts Play.</summary>
    internal sealed class FakeRunFlow : IRunFlow
    {
        public RunStep Step { get; private set; } = RunStep.Home;
        public event Action<RunStep> StepChanged;
        public RunTeam Team => default;
        public event Action<RunTeam> RunStarted { add { } remove { } }
        public event Action BattleStarted { add { } remove { } }
        public event Action BattleStopped { add { } remove { } }
        public event Action BattleCleared { add { } remove { } }
        public bool IsPaused => false;

        public int PlayCalls { get; private set; }
        public bool HasListeners => StepChanged != null;

        public void GoTo(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        public void Play() => PlayCalls++;
        public void EnterBattle(RunTeam team) { }
        public void Pause() { }
        public void Resume() { }
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() { }
        public void CountClearedWave(int wave) { }
    }

    /// <summary>Stand-in for the run's score.</summary>
    internal sealed class FakeRunScore : IRunScore
    {
        public int Score { get; set; }
        public int Best { get; set; }
        public bool IsNewBest { get; set; }
        public event Action ScoreChanged;

        public void SetBest(int best, bool raise = true)
        {
            Best = best;
            if (raise) ScoreChanged?.Invoke();
        }
    }

    /// <summary>Records what the presenter shows; the test presses Play.</summary>
    internal sealed class FakeHomeDisplay : IHomeScreenDisplay
    {
        public event Action PlayPressed;
        public bool Visible { get; private set; }
        public bool Disposed { get; private set; }
        public HomeScreenContent Content { get; private set; }
        public int ShowCalls { get; private set; }

        public void Show(HomeScreenContent content)
        {
            Content = content;
            Visible = true;
            ShowCalls++;
        }

        public void Hide() => Visible = false;

        public void Dispose()
        {
            Disposed = true;
            Visible = false;
        }

        public void PressPlay() => PlayPressed?.Invoke();
    }

    internal sealed class FakeHomeFactory : IHomeScreenFactory
    {
        public FakeHomeDisplay Display { get; } = new FakeHomeDisplay();
        public HomeLayout Layout { get; private set; }

        public IHomeScreenDisplay Create(HomeLayout layout)
        {
            Layout = layout;
            return Display;
        }
    }

    /// <summary>Stand-in for the scene session: keeps who joined.</summary>
    internal sealed class FakeSessionScene : ISessionScene
    {
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged { add { } remove { } }
        public List<ISessionParticipant> Joined { get; } = new List<ISessionParticipant>();
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }
}
