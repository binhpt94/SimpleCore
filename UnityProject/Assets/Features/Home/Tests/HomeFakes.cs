using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;

namespace Game.Home.Tests
{
    /// <summary>Stand-in for the run's score.</summary>
    internal sealed class FakeRunScore : IRunScore
    {
        public int Score { get; set; }
        public int Best { get; set; }
        public bool IsNewBest { get; set; }
        public event Action ScoreChanged;

        public bool HasListeners => ScoreChanged != null;

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

        private UnityEngine.GameObject _root;

        /// <summary>Made on first use, destroyed by <see cref="Dispose"/>, like the real view's canvas.</summary>
        public UnityEngine.Object Root => _root ? _root : (_root = new UnityEngine.GameObject("FakeHomeScreen"));

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
            if (_root) UnityEngine.Object.DestroyImmediate(_root);
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
