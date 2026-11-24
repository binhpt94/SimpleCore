using System;
using System.Collections.Generic;

namespace Game.Run
{
    /// <summary>
    /// The steps of a run in the Game scene (feature-run/run-flow/flow-rules, feature-run/run-pause/pause-rules).
    /// Only this class decides the next step; requests that do not fit the current step, or that arrive while
    /// a step change is still running, are ignored.
    /// </summary>
    internal sealed class RunFlow : IRunFlow
    {
        private readonly RunScore _score;
        private readonly IRunDelay _delay;
        private readonly float _loseDelaySeconds;
        private readonly int _teamSize;

        private readonly bool _skipMenus;
        private readonly RunTeam _defaultTeam;

        private bool _changing;
        private bool _begun;
        private IDisposable _pendingLose;

        /// <param name="skipMenus">
        /// IDEA stage: <see cref="Begin"/> goes straight into battle with <paramref name="defaultTeam"/>, Play again starts a new
        /// battle with it, and there is no way back to the start screen (Back to start, Give up are ignored).
        /// </param>
        public RunFlow(RunScore score, IRunDelay delay, float loseDelaySeconds, int teamSize,
            bool skipMenus = false, RunTeam defaultTeam = default)
        {
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            if (loseDelaySeconds < 0f) throw new ArgumentOutOfRangeException(nameof(loseDelaySeconds));
            if (teamSize < 1) throw new ArgumentOutOfRangeException(nameof(teamSize));
            _loseDelaySeconds = loseDelaySeconds;
            _teamSize = teamSize;
            _skipMenus = skipMenus;
            _defaultTeam = defaultTeam;
            if (skipMenus)
            {
                try
                {
                    CheckTeam(defaultTeam);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"The default team (used while menus are skipped) is wrong: {e.Message}", nameof(defaultTeam), e);
                }
            }
        }

        /// <summary>The Game scene is playable: start on the start screen, or straight in battle when menus are skipped. Once.</summary>
        public void Begin()
        {
            if (_begun) return;
            _begun = true;
            if (!_skipMenus || !CanChange(RunStep.Home)) return;
            Change(() => StartBattle(_defaultTeam));
        }

        public RunStep Step { get; private set; } = RunStep.Home;

        public RunTeam Team { get; private set; }

        public bool IsPaused => Step == RunStep.Paused;

        public event Action<RunStep> StepChanged;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;

        public void Play()
        {
            if (!CanChange(RunStep.Home)) return;
            Change(() => SetStep(RunStep.TeamSelect));
        }

        public void EnterBattle(RunTeam team)
        {
            if (!CanChange(RunStep.TeamSelect)) return;
            CheckTeam(team);
            Change(() => StartBattle(team));
        }

        public void Pause()
        {
            if (!CanChange(RunStep.Playing)) return;
            Change(() => SetStep(RunStep.Paused));
        }

        public void Resume()
        {
            if (!CanChange(RunStep.Paused)) return;
            Change(() => SetStep(RunStep.Playing));
        }

        public void Abandon()
        {
            if (_skipMenus || !CanChange(RunStep.Paused)) return;
            Change(() =>
            {
                _score.Settle();
                BattleStopped?.Invoke();
                BattleCleared?.Invoke();
                SetStep(RunStep.Home);
            });
        }

        public void Lose()
        {
            // The castle can fall while a step change is running (e.g. during a BattleStarted handler), so no lock here.
            if (Step != RunStep.Playing && Step != RunStep.Paused) return;
            _score.Settle();
            SetStep(RunStep.Ending);
            BattleStopped?.Invoke();
            _pendingLose = _delay.Schedule(_loseDelaySeconds, ShowLost);
        }

        public void Replay()
        {
            if (!CanChange(RunStep.Lost)) return;
            Change(() =>
            {
                BattleCleared?.Invoke();
                if (_skipMenus) StartBattle(_defaultTeam);
                else SetStep(RunStep.TeamSelect);
            });
        }

        public void GoHome()
        {
            if (_skipMenus || !CanChange(RunStep.Lost)) return;
            Change(() =>
            {
                BattleCleared?.Invoke();
                SetStep(RunStep.Home);
            });
        }

        public void CountClearedWave(int wave)
        {
            if (Step != RunStep.Playing && Step != RunStep.Paused) return;
            _score.CountClearedWave(wave);
        }

        /// <summary>The scene session ends: drop the pending lose popup.</summary>
        public void Shutdown()
        {
            _pendingLose?.Dispose();
            _pendingLose = null;
        }

        private void StartBattle(RunTeam team)
        {
            Team = team;
            _score.StartRun();
            SetStep(RunStep.Playing);
            RunStarted?.Invoke(team);
            BattleStarted?.Invoke();
        }

        private void ShowLost()
        {
            _pendingLose = null;
            if (Step != RunStep.Ending) return;
            SetStep(RunStep.Lost);
        }

        private bool CanChange(RunStep from) => !_changing && Step == from;

        private void Change(Action change)
        {
            _changing = true;
            try
            {
                change();
            }
            finally
            {
                _changing = false;
            }
        }

        private void SetStep(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        private void CheckTeam(RunTeam team)
        {
            if (team.Count != _teamSize)
                throw new ArgumentException(
                    $"A team needs exactly {_teamSize} heroes to enter battle, got {team.Count}. Team select must lock Enter battle until the team is full.",
                    nameof(team));

            var seen = new HashSet<string>();
            foreach (var code in team.HeroCodes)
            {
                if (string.IsNullOrEmpty(code))
                    throw new ArgumentException("A team slot has no hero code.", nameof(team));
                if (!seen.Add(code))
                    throw new ArgumentException($"Hero '{code}' is in the team twice. A team has no duplicate heroes.", nameof(team));
            }
        }
    }
}
