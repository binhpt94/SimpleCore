using System;
using System.Collections.Generic;

namespace Game.Run
{
    /// <summary>
    /// The steps of a run in the Game scene (feature-run/run-flow/flow-rules, feature-run/run-pause/pause-rules).
    /// Only this class decides the next step; requests that do not fit the current step, or that arrive while
    /// a step change is still running, are ignored.
    /// </summary>
    internal sealed class RunFlow : IRunFlow, IRunRevive
    {
        private readonly RunScore _score;
        private readonly IRunDelay _delay;
        private readonly float _loseDelaySeconds;
        private readonly int _teamSize;

        private readonly bool _skipMenus;
        private readonly RunTeam _defaultTeam;
        private readonly bool _reviveOnce;

        // Set when the revive is offered (the castle's first fall); cleared when a new run starts.
        private bool _reviveOffered;
        private bool _lostAfterFailedRevive;

        private bool _changing;
        private bool _begun;
        private IDisposable _pendingLose;

        // Waves reported cleared in the current frame; they count on the next frame unless the castle falls first.
        private readonly List<int> _clearedThisFrame = new List<int>();
        private IDisposable _pendingClears;

        /// <param name="skipMenus">
        /// IDEA stage: <see cref="Begin"/> goes straight into battle with <paramref name="defaultTeam"/>, Play again starts a new
        /// battle with it, and there is no way back to the start screen (Back to start, Give up are ignored).
        /// </param>
        /// <param name="reviveOnce">
        /// The castle's first fall in a run offers a revive (feature-popup-revive/revive-order/order-values); the popup
        /// shows after <paramref name="loseDelaySeconds"/>. Off: every fall loses the run.
        /// </param>
        public RunFlow(RunScore score, IRunDelay delay, float loseDelaySeconds, int teamSize,
            bool skipMenus = false, RunTeam defaultTeam = default, bool reviveOnce = false)
        {
            _reviveOnce = reviveOnce;
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

        internal float LoseDelaySeconds => _loseDelaySeconds;

        internal int TeamSize => _teamSize;

        internal bool SkipMenus => _skipMenus;

        internal RunTeam DefaultTeam => _defaultTeam;

        internal bool ReviveOnce => _reviveOnce;

        public RunTeam Team { get; private set; }

        public bool IsPaused =>
            Step == RunStep.Paused || Step == RunStep.ReviveWait || Step == RunStep.ReviveOffer || Step == RunStep.Reviving;

        public bool LostAfterFailedRevive => Step == RunStep.Lost && _lostAfterFailedRevive;

        public event Action ReviveStarted;

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
                CommitClearedWaves();
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
            if (_reviveOnce && !_reviveOffered)
            {
                OfferRevive();
                return;
            }
            // The castle falling wins over a wave cleared in the same frame: that wave does not count.
            DropClearedWaves();
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

        public void Revive()
        {
            if (!CanChange(RunStep.ReviveOffer)) return;
            Change(() =>
            {
                SetStep(RunStep.Reviving);
                ReviveStarted?.Invoke();
            });
        }

        public void DeclineRevive() => EndAfterRevive(failed: false);

        public void FailRevive() => EndAfterRevive(failed: true);

        public void ReviveShown()
        {
            // No lock: the castle may report it is shown from inside its ReviveStarted handler.
            if (Step != RunStep.Reviving) return;
            SetStep(RunStep.Playing);
            // Waves cleared in the frame the castle fell count now that the run goes on.
            CommitClearedWaves();
        }

        public void CountClearedWave(int wave)
        {
            if (Step == RunStep.ReviveWait)
            {
                // Same frame as the castle's first fall: held until the revive is decided.
                _clearedThisFrame.Add(wave);
                return;
            }
            if (Step != RunStep.Playing && Step != RunStep.Paused) return;
            _clearedThisFrame.Add(wave);
            if (_pendingClears == null) _pendingClears = _delay.NextFrame(OnNextFrameAfterClear);
        }

        /// <summary>The scene session ends: drop the pending lose popup and uncounted waves.</summary>
        public void Shutdown()
        {
            _pendingLose?.Dispose();
            _pendingLose = null;
            DropClearedWaves();
        }

        private void OnNextFrameAfterClear()
        {
            _pendingClears = null;
            CommitClearedWaves();
        }

        private void CommitClearedWaves()
        {
            _pendingClears?.Dispose();
            _pendingClears = null;
            var waves = _clearedThisFrame.ToArray();
            _clearedThisFrame.Clear();
            if (Step != RunStep.Playing && Step != RunStep.Paused) return;
            foreach (var wave in waves) _score.CountClearedWave(wave);
        }

        private void DropClearedWaves()
        {
            _pendingClears?.Dispose();
            _pendingClears = null;
            _clearedThisFrame.Clear();
        }

        private void OfferRevive()
        {
            _reviveOffered = true;
            // Keep the waves reported this frame, but do not count them yet: the revive decides.
            _pendingClears?.Dispose();
            _pendingClears = null;
            SetStep(RunStep.ReviveWait);
            _pendingLose = _delay.Schedule(_loseDelaySeconds, ShowReviveOffer);
        }

        private void ShowReviveOffer()
        {
            _pendingLose = null;
            if (Step != RunStep.ReviveWait) return;
            SetStep(RunStep.ReviveOffer);
        }

        private void EndAfterRevive(bool failed)
        {
            if (!CanChange(RunStep.ReviveOffer)) return;
            Change(() =>
            {
                // Lost first: the held waves do not count.
                DropClearedWaves();
                _lostAfterFailedRevive = failed;
                _score.Settle();
                BattleStopped?.Invoke();
                SetStep(RunStep.Lost);
            });
        }

        private void StartBattle(RunTeam team)
        {
            DropClearedWaves();
            _reviveOffered = false;
            _lostAfterFailedRevive = false;
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
