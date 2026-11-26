using System;
using System.Collections.Generic;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupRating.Tests
{
    /// <summary>Stand-ins for Run, the popup service, the store page and the clock.</summary>
    internal static class RatingFakes
    {
        internal sealed class Rig : IDisposable
        {
            public readonly FakeFlow Flow = new FakeFlow();
            public readonly FakeScore Score = new FakeScore { Score = 5, IsNewBest = true };
            public readonly FakePopups Popups = new FakePopups();
            public readonly FakeStore Store = new FakeStore();
            public readonly FakeClock Clock = new FakeClock();
            public readonly FakeLog Log = new FakeLog();
            public readonly RatingConfig Config = UnityEngine.ScriptableObject.CreateInstance<RatingConfig>();
            public readonly RatingLaunchMemory Memory = new RatingLaunchMemory();
            public RatingParticipant Participant;

            public Rig() => Participant = Make();

            private RatingParticipant Make() =>
                new RatingParticipant(Flow, Score, Popups, Popups, Store, Clock, Log, Config, Memory);

            /// <summary>Runs the participant, the run set a new best, and the battle stopped.</summary>
            public void NewBest()
            {
                Participant.Run();
                Score.IsNewBest = true;
                Flow.Stop();
            }

            public RatingParticipant NewSessionSameLaunch()
            {
                Participant.DisposeAsync().GetAwaiter().GetResult();
                Participant = Make();
                return Participant;
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Config);
        }

        internal sealed class FakeFlow : IRunFlow
        {
            public RunStep Step => RunStep.Lost;
            public event Action<RunStep> StepChanged { add { } remove { } }
            public RunTeam Team => default;
            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped;
            public event Action BattleCleared;
            public void Stop() => BattleStopped?.Invoke();
            public void Clear() => BattleCleared?.Invoke();
            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }

        internal sealed class FakeScore : IRunScore
        {
            public int Score { get; set; }
            public int Best { get; set; }
            public bool IsNewBest { get; set; }
            public event Action ScoreChanged { add { } remove { } }
        }

        internal sealed class FakeView : IRatingView
        {
            public event Action RatePressed;
            public event Action LaterPressed;
            public event Action BackPressed;
            public string Text, Rate, Later;
            public bool Locked;

            public void ShowText(string inviteText, string rateLabel, string laterLabel)
            {
                Text = inviteText;
                Rate = rateLabel;
                Later = laterLabel;
            }

            public void LockButtons() => Locked = true;
            public void PressRate() { if (!Locked) RatePressed?.Invoke(); }
            public void PressLater() { if (!Locked) LaterPressed?.Invoke(); }
            public void PressBack() => BackPressed?.Invoke();
            public void RaiseRateIgnoringLock() => RatePressed?.Invoke();
            public void RaiseLaterIgnoringLock() => LaterPressed?.Invoke();
        }

        internal sealed class FakePopups : IPopupService, IPopupViews
        {
            public readonly List<PopupId> Shown = new List<PopupId>();
            private readonly List<int> _open = new List<int>();
            private int _next;
            private int _loseId;
            public bool Refuse;
            public bool OnTop = true;
            public int Closes;
            public FakeView View { get; private set; }
            public int OpenCount => _open.Count;
            public bool TopIsInvite => _open.Count > 0 && _open[_open.Count - 1] != _loseId;

            public void ShowLoseBelow()
            {
                _loseId = ++_next;
                _open.Add(_loseId);
            }

            public PopupHandle Show(PopupId id)
            {
                if (Refuse) return default;
                Shown.Add(id);
                var handle = new PopupHandle(++_next);
                _open.Add(handle.Id);
                View = new FakeView();
                return handle;
            }

            public void Close(PopupHandle handle)
            {
                if (_open.Remove(handle.Id)) Closes++;
            }

            public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
            {
                view = _open.Contains(handle.Id) ? View as T : null;
                return view != null;
            }

            public bool IsOnTop(PopupHandle handle) =>
                OnTop && _open.Count > 0 && _open[_open.Count - 1] == handle.Id;
        }

        internal sealed class FakeStore : IStorePage
        {
            public bool AppWorks = true;
            public bool BrowserWorks = true;
            public readonly List<string> AppPackages = new List<string>();
            public readonly List<string> BrowserPackages = new List<string>();
            public Action OnAppTry;
            public int AppTries => AppPackages.Count;
            public int BrowserTries => BrowserPackages.Count;
            public string PackageName => "com.test.game";

            public bool TryOpenApp(string packageName)
            {
                OnAppTry?.Invoke();
                AppPackages.Add(packageName);
                return AppWorks;
            }

            public bool TryOpenBrowser(string packageName)
            {
                BrowserPackages.Add(packageName);
                return BrowserWorks;
            }
        }

        internal sealed class FakeClock : IRatingClock
        {
            private Action<float> _tick;
            private Action<bool> _active;
            public bool Running => _tick != null;

            public IDisposable Start(Action<float> tick, Action<bool> appActive)
            {
                _tick = tick;
                _active = appActive;
                return new Stopper(this);
            }

            public void Tick(float seconds) => _tick?.Invoke(seconds);
            public void SetActive(bool active) => _active?.Invoke(active);

            private sealed class Stopper : IDisposable
            {
                private FakeClock _clock;
                public Stopper(FakeClock clock) => _clock = clock;
                public void Dispose()
                {
                    if (_clock == null) return;
                    _clock._tick = null;
                    _clock._active = null;
                    _clock = null;
                }
            }
        }

        internal sealed class FakeLog : IRatingLog
        {
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) => Errors.Add(message);
        }
    }
}
