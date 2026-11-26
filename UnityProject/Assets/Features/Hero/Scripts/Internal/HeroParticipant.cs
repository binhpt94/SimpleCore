using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Combat;
using Game.Core;
using Game.Run;

namespace Game.Hero
{
    /// <summary>
    /// The hero feature inside the Game scene's session: follows the run's signals, moves the battle on as a combat actor
    /// (aim then strike, once per combat tick, which runs once per gameplay step of the shared clock) and redraws the
    /// heroes each frame.
    /// </summary>
    internal sealed class HeroParticipant : SessionParticipantBase, ICombatActor
    {
        private readonly IRunFlow _run;
        private readonly HeroSquad _squad;
        private readonly HeroPresenter _presenter;
        private readonly ICombat _combat;
        private readonly IFrameClock _frames;
        private bool _joinedCombat;
        private bool _running;
        private bool _battleActive;

        public HeroParticipant(IRunFlow run, HeroSquad squad, HeroPresenter presenter, ICombat combat, IFrameClock frames)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _squad = squad ?? throw new ArgumentNullException(nameof(squad));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
        }

        private bool Playing => _running && _battleActive;

        protected override void OnRun()
        {
            _presenter.Attach();
            _run.RunStarted += OnRunStarted;
            _run.BattleStopped += OnBattleStopped;
            _run.BattleCleared += OnBattleCleared;
            _running = true;
            // Combat keeps its actors for the scene; after the session ends the hero's aim and strike do nothing.
            if (!_joinedCombat)
            {
                _joinedCombat = true;
                _combat.AddActor(this);
            }

            var loop = new CancellationTokenSource();
            FrameLoop(loop.Token).Forget();

            WhenDisposed(() =>
            {
                _running = false;
                loop.Cancel();
                loop.Dispose();
                _run.RunStarted -= OnRunStarted;
                _run.BattleStopped -= OnBattleStopped;
                _run.BattleCleared -= OnBattleCleared;
                _squad.Clear();
                _presenter.Dispose();
            });
        }

        /// <summary>First part of a combat tick: heroes respawn, count their waits, pick targets and walk.</summary>
        public void Aim(float stepSeconds)
        {
            if (Playing) _squad.Aim(stepSeconds);
        }

        /// <summary>Second part of a combat tick: heroes in range whose wait is over hit, through combat.</summary>
        public void Strike(float stepSeconds)
        {
            if (Playing) _squad.Strike();
        }

        /// <summary>One frame of the view: draws the state after the frame's last step.</summary>
        internal void RefreshView(float frameSeconds) => _presenter.Refresh(Playing ? frameSeconds : 0f);

        private async UniTaskVoid FrameLoop(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var cancelled = await _frames.NextFrame(cancellation).SuppressCancellationThrow();
                if (cancelled) return;
                RefreshView(_frames.DeltaSeconds);
            }
        }

        private void OnRunStarted(RunTeam team)
        {
            // A team short of (or over) one hero per slot is an error of the team select screen: no hero enters.
            if (team.Count != _squad.TeamSize)
            {
                _battleActive = false;
                _squad.Clear();
                UnityEngine.Debug.LogError(
                    $"[Hero] The run brought {team.Count} heroes; a team needs {_squad.TeamSize}. No hero enters the battle.");
                return;
            }
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
