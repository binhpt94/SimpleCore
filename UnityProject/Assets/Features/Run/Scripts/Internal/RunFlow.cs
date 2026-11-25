using System;
using System.Collections.Generic;

namespace Game.Run
{
    /// <summary>
    /// The steps of a run in the Game scene (feature-run/run-flow/flow-rules, feature-run/run-pause/pause-rules).
    /// Only this class decides the next step; requests that do not fit the current step, or that arrive while
    /// a step change is still running, are ignored.
    /// </summary>
    internal sealed class RunFlow : IRunFlow, IRunRevive, IRunBattle
    {
        private readonly RunScore _score;
        private readonly IRunDelay _delay;
        private readonly float _loseDelaySeconds;
        private readonly int _teamSize;

        private readonly bool _skipMenus;
        private readonly RunTeam _defaultTeam;
        private readonly bool _reviveOnce;
        private readonly bool _pauseEnabled;
        private readonly IRunClock _clock;

        // True between the clock's SetUpRun and ClearRun of the current battle.
        private bool _clockRun;

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
        /// <param name="clock">
        /// The shared clock (feature-sim-clock): set up, started, gameplay on and off, cleared with the battle; pause and
        /// continue go through its queue. Null: no clock, pause and continue apply at once (tests of the run alone).
        /// </param>
        public RunFlow(RunScore score, IRunDelay delay, float loseDelaySeconds, int teamSize,
            bool skipMenus = false, RunTeam defaultTeam = default, bool reviveOnce = false, bool pauseEnabled = true,
            IRunClock clock = null)
        {
            _reviveOnce = reviveOnce;
            _pauseEnabled = pauseEnabled;
            _clock = clock ?? new ImmediateRunClock();
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

        internal bool PauseEnabled => _pauseEnabled;

        /// <summary>
        /// feature-popup-revive/revive-order/order-values#offer-flag: on from the castle's first fall of the run (the
        /// revive is offered), off only when a new run starts. A fall while it is on loses the run.
        /// </summary>
        internal bool ReviveOffered => _reviveOffered;

        public RunTeam Team { get; private set; }

        public bool IsPaused =>
            Step == RunStep.Paused || Step == RunStep.ReviveWait || Step == RunStep.ReviveOffer || Step == RunStep.Reviving;

        public bool LostAfterFailedRevive => Step == RunStep.Lost && _lostAfterFailedRevive;

        public event Action ReviveStarted;

        public event Action BattleBuilt;
        public event Action<string> BuildFailed;
        public event Action<RunLeaveChoice> BattleLeft;

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

        /// <summary>
        /// The screen flow asks for a battle (from PROTOTYPE). Allowed only out of battle (start screen or team select,
        /// as the run's step); answers with exactly one of BattleBuilt or BuildFailed before returning. A wrong team,
        /// or a battle feature failing while it builds, is a BuildFailed and leaves nothing of the battle.
        /// </summary>
        public void BuildBattle(RunTeam team)
        {
            if (_changing || (Step != RunStep.Home && Step != RunStep.TeamSelect)) return;
            try
            {
                CheckTeam(team);
            }
            catch (ArgumentException e)
            {
                BuildFailed?.Invoke(e.Message);
                return;
            }

            string failure = null;
            Change(() =>
            {
                try
                {
                    StartBattle(team);
                }
                catch (Exception e)
                {
                    failure = $"Building the battle failed: {e.Message}";
                    ThrowAwayBattle();
                }
            });
            if (failure != null) BuildFailed?.Invoke(failure);
            else BattleBuilt?.Invoke();
        }

        /// <summary>
        /// The screen flow gave up waiting for the build: whatever battle there is goes, with no answer and no
        /// BattleLeft. The build answers before BuildBattle returns, so this only finds a battle built just before.
        /// </summary>
        public void CancelBuild()
        {
            if (_changing || Step != RunStep.Playing) return;
            Change(ThrowAwayBattle);
        }

        /// <summary>The battle goes without settling a score: stopped, clock cleared, battle cleared, back on the start screen.</summary>
        private void ThrowAwayBattle()
        {
            DropClearedWaves();
            _pendingLose?.Dispose();
            _pendingLose = null;
            if (_clockRun)
            {
                BattleStopped?.Invoke();
                ClearClockRun();
                BattleCleared?.Invoke();
            }
            SetStep(RunStep.Home);
        }

        public void Pause()
        {
            // pause-rules "Thông số theo giai đoạn": IDEA has no pause, every pause request is ignored.
            if (!_pauseEnabled || Step != RunStep.Playing) return;
            // A player action: applied at the start of the next step of the shared clock, checked again then.
            _clock.Enqueue(ApplyPause);
        }

        public void Resume()
        {
            if (Step != RunStep.Paused) return;
            _clock.Enqueue(ApplyResume);
        }

        private void ApplyPause()
        {
            if (!_pauseEnabled || !CanChange(RunStep.Playing)) return;
            Change(() => SetStep(RunStep.Paused));
        }

        private void ApplyResume()
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
                ClearClockRun();
                BattleCleared?.Invoke();
                SetStep(RunStep.Home);
            });
            // The battle is gone: the screen flow shows the start screen.
            BattleLeft?.Invoke(RunLeaveChoice.Abandon);
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
                ClearClockRun();
                BattleCleared?.Invoke();
                // PROTOTYPE: the run leaves the battle step and goes out of battle (Home is the run's only
                // out-of-battle step); feature-game-flow shows team select from BattleLeft. IDEA: straight into a new battle.
                if (_skipMenus) StartBattle(_defaultTeam);
                else SetStep(RunStep.Home);
            });
            // Menus skipped (IDEA): the run already started the next battle itself, no screen flow to tell.
            if (!_skipMenus) BattleLeft?.Invoke(RunLeaveChoice.PlayAgain);
        }

        public void GoHome()
        {
            if (_skipMenus || !CanChange(RunStep.Lost)) return;
            Change(() =>
            {
                ClearClockRun();
                BattleCleared?.Invoke();
                SetStep(RunStep.Home);
            });
            BattleLeft?.Invoke(RunLeaveChoice.BackToStart);
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
            ClearClockRun();
        }

        /// <summary>The clock clears the run's queue, left-over time and streams before the battle is reported cleared.</summary>
        private void ClearClockRun()
        {
            if (!_clockRun) return;
            _clockRun = false;
            _clock.ClearRun();
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
            // The clock resets for the new run before anyone hears of it, and runs its first gameplay step only once
            // the battle is built: building the battle spends no spawn or cooldown time.
            ClearClockRun();
            _clock.SetUpRun();
            _clockRun = true;
            Team = team;
            _score.StartRun();
            SetStep(RunStep.Playing);
            RunStarted?.Invoke(team);
            BattleStarted?.Invoke();
            _clock.StartRun();
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
            // The battle plays only while Playing: paused, waiting for or showing the revive, ending and lost stand still.
            if (_clockRun) _clock.SetGameplay(step == RunStep.Playing);
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
