using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;

namespace Game.Hero
{
    /// <summary>
    /// The hero feature inside the Game scene's session: follows the run's signals, shows the heroes and
    /// steps the battle every frame while it is playing.
    /// </summary>
    internal sealed class HeroParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _run;
        private readonly HeroSquad _squad;
        private readonly HeroPresenter _presenter;
        private readonly IFrameClock _clock;
        private bool _battleActive;

        public HeroParticipant(IRunFlow run, HeroSquad squad, HeroPresenter presenter, IFrameClock clock)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _squad = squad ?? throw new ArgumentNullException(nameof(squad));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override void OnRun()
        {
            _presenter.Attach();
            _run.RunStarted += OnRunStarted;
            _run.BattleStopped += OnBattleStopped;
            _run.BattleCleared += OnBattleCleared;

            var loop = new CancellationTokenSource();
            FrameLoop(loop.Token).Forget();

            WhenDisposed(() =>
            {
                loop.Cancel();
                loop.Dispose();
                _run.RunStarted -= OnRunStarted;
                _run.BattleStopped -= OnBattleStopped;
                _run.BattleCleared -= OnBattleCleared;
                _squad.Clear();
                _presenter.Dispose();
            });
        }

        /// <summary>One frame: the battle moves on unless it is stopped or paused; the view always refreshes.</summary>
        internal void Step(float deltaSeconds)
        {
            var playing = _battleActive && !_run.IsPaused;
            if (playing) _squad.Tick(deltaSeconds);
            _presenter.Refresh(playing ? deltaSeconds : 0f);
        }

        private async UniTaskVoid FrameLoop(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var cancelled = await _clock.NextFrame(cancellation).SuppressCancellationThrow();
                if (cancelled) return;
                Step(_clock.DeltaSeconds);
            }
        }

        private void OnRunStarted(RunTeam team)
        {
            _squad.Begin(team);
            _battleActive = true;
        }

        private void OnBattleStopped() => _battleActive = false;

        private void OnBattleCleared()
        {
            _battleActive = false;
            _squad.Clear();
        }
    }
}
