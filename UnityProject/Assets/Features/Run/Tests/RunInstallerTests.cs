using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using Game.Save;
using Game.Services.Popup;
using Game.SimClock;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.Run.Tests
{
    public class RunInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Run/FeatureAssets/Public/RunInstaller.asset";
        private const string ConfigPath = "Assets/Features/Run/FeatureAssets/Private/RunConfig.asset";

        /// <summary>Everything the run needs installed before it, faked.</summary>
        private static ServiceRegistry Registry(FakeSession session = null, FakeSimClock clock = null, FakeSave save = null)
        {
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session ?? new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            clock = clock ?? new FakeSimClock();
            registry.Register<ISimClock>(clock);
            registry.Register<ISimRunControl>(clock);
            registry.Register<ISavedRecord>(save ?? new FakeSave());
            return registry;
        }

        [Test]
        public void InstallerAssetPublishesTheRunAndJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var session = new FakeSession();
            var registry = Registry(session);

            installer.Install(registry);

            Assert.IsTrue(registry.TryResolve<IRunFlow>(out var flow));
            Assert.IsTrue(registry.TryResolve<IRunScore>(out _));
            Assert.IsTrue(registry.TryResolve<IRunBattle>(out var battle));
            Assert.AreSame(flow, battle, "IRunBattle must be the same object as IRunFlow.");
            Assert.AreEqual(RunStep.Home, flow.Step);
            Assert.AreEqual(2, session.Joined.Count, "The run and its leftover popup check join the session.");
        }

        private static readonly RunTeam ChosenTeam = new RunTeam(new[] { "knight", "axeman", "archer" });

        /// <summary>Installs the real asset and goes from the start screen into battle with a chosen team.</summary>
        private static RunFlow InstalledRunInBattle(FakeSimClock clock)
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");
            var registry = Registry(clock: clock);
            installer.Install(registry);
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            flow.Begin();
            flow.Play();
            flow.EnterBattle(ChosenTeam);
            return flow;
        }

        [Test]
        public void ConfigAssetHoldsThePrototypeRunValues()
        {
            // "Thông số theo giai đoạn", PROTOTYPE column of flow-rules, score-rules, start-values.
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");

            Assert.AreEqual(1f, config.LoseDelaySeconds, "flow-rules: 1 s before the lose popup.");
            Assert.AreEqual(3, config.TeamSize);
            Assert.IsFalse(config.SkipMenus, "flow-rules: after Loading comes the start screen; Play again goes to team select.");
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.DefaultTeam.HeroCodes,
                "feature-team-select/team-memory/memory-rules: default team (IDEA battle, dev cheats).");
        }

        [Test]
        public void InstalledPrototypeRunBuildsTheBattleTheScreenFlowAsksForAndHandsBackTheLeaveChoice()
        {
            // flow-rules "Thông số theo giai đoạn", PROTOTYPE: feature-game-flow picks the screens; the run builds,
            // clears and hands back the leave choice.
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var clock = new FakeSimClock();
            var registry = Registry(clock: clock);
            installer.Install(registry);
            var battle = registry.Resolve<IRunBattle>();
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            var answers = new List<string>();
            battle.BattleBuilt += () => answers.Add("built");
            battle.BuildFailed += _ => answers.Add("failed");
            battle.BattleLeft += choice => answers.Add("left:" + choice + ":" + flow.Step);

            flow.Begin();
            Assert.AreEqual(RunStep.Home, flow.Step, "The run does not go into battle on its own.");
            battle.BuildBattle(ChosenTeam);
            Assert.AreEqual(RunStep.Playing, flow.Step);

            flow.Lose();
            clock.Step(40);
            flow.Replay();

            CollectionAssert.AreEqual(new[] { "built", "left:PlayAgain:Home" }, answers);
            CollectionAssert.Contains(clock.Calls, "ClearRun", "The battle was cleared before the flow heard of it.");
            flow.Shutdown();
        }

        [Test]
        public void PrototypeRunHasBackToStart()
        {
            var clock = new FakeSimClock();
            var flow = InstalledRunInBattle(clock);

            flow.Lose();
            clock.Step(40);
            Assert.AreEqual(RunStep.Lost, flow.Step);
            flow.GoHome();
            Assert.AreEqual(RunStep.Home, flow.Step, "Back to start on the lose popup goes to the start screen.");
            flow.Shutdown();
        }

        [Test]
        public void ConfigAssetHasTheDevicePrefabThatHearsLeavingTheGame()
        {
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");
            Assert.IsNotNull(config.AppSignalsPrefab, "RunConfig has no app signals prefab.");
            Assert.IsNotNull(config.AppSignalsPrefab.GetComponent<IAppSignals>(),
                "The app signals prefab has no RunAppSignalsView.");
        }

        [Test]
        public void InstalledRunUsesTheConfigAndOpensOnTheStartScreen()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = Registry();
            installer.Install(registry);

            var flow = registry.Resolve<IRunFlow>() as RunFlow;
            Assert.IsNotNull(flow, "IRunFlow is not the run's RunFlow.");
            Assert.AreEqual(1f, flow.LoseDelaySeconds);
            Assert.AreEqual(3, flow.TeamSize);
            Assert.IsFalse(flow.SkipMenus);

            flow.Begin();
            Assert.AreEqual(RunStep.Home, flow.Step, "PROTOTYPE: the Game scene opens on the start screen.");
            flow.Play();
            Assert.AreEqual(RunStep.TeamSelect, flow.Step);
            flow.EnterBattle(ChosenTeam);
            Assert.AreEqual(RunStep.Playing, flow.Step);
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, flow.Team.HeroCodes,
                "The battle uses the team chosen in team select.");
            flow.Shutdown();
        }

        [Test]
        public void InstalledRunDrivesTheSharedClockAndWaitsOnItsSteps()
        {
            var clock = new FakeSimClock();
            var flow = InstalledRunInBattle(clock);
            CollectionAssert.AreEqual(new[] { "SetUpRun", "Gameplay:True", "StartRun" }, clock.Calls);

            flow.Lose();
            CollectionAssert.Contains(clock.Owners, "Run", "The run waits at its own place in the step order.");
            clock.Step(39);
            Assert.AreEqual(RunStep.Ending, flow.Step, "39 steps of 0.025 s are not yet the 1 s wait.");
            clock.Step(1);
            Assert.AreEqual(RunStep.Lost, flow.Step, "The lose popup shows after 1 s of clock steps.");
            flow.Shutdown();
            Assert.AreEqual("ClearRun", clock.Calls[clock.Calls.Count - 1]);
        }

        [Test]
        public void InstalledRunPlaysWithTheDefaultTeamOfTheConfig()
        {
            // start-values: the default team (Hiệp sĩ, Đấu sĩ rìu, Cung thủ) comes from RunConfig.asset into the run.
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var registry = Registry();
            installer.Install(registry);
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, flow.DefaultTeam.HeroCodes);
            flow.Shutdown();
        }

        [Test]
        public void InstalledRunReadsTheBestFromSaveAndHandsTheNewBestBackToIt()
        {
            // score-rules#params: the run asks feature-save; it never reads or writes the device itself.
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var clock = new FakeSimClock();
            var save = new FakeSave { Stored = 0 };
            var registry = Registry(clock: clock, save: save);
            installer.Install(registry);
            var score = (RunScore)registry.Resolve<IRunScore>();
            var flow = (RunFlow)registry.Resolve<IRunFlow>();

            score.LoadBest();
            Assert.AreEqual(0, score.Best);
            flow.Begin();
            flow.Play();
            flow.EnterBattle(ChosenTeam);
            flow.CountClearedWave(1);
            clock.Step(1);
            flow.Lose();

            Assert.AreEqual(1, score.Best);
            CollectionAssert.AreEqual(new[] { 1 }, save.Writes, "Handed to feature-save before the lose popup.");
            flow.Shutdown();
        }

        [Test]
        public void InstallWithoutSaveFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            var clock = new FakeSimClock();
            registry.Register<ISimClock>(clock);
            registry.Register<ISimRunControl>(clock);
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ISavedRecord), error.Message);
        }

        [Test]
        public void InstallWithoutSessionSceneFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new ServiceRegistry()));
            StringAssert.Contains(nameof(ISessionScene), error.Message);
        }

        [Test]
        public void InstallWithoutPopupStackFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IPopupStack), error.Message);
        }

        [Test]
        public void InstallWithoutTheSharedClockFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ISimClock), error.Message);
        }

        /// <summary>The shared clock of feature-sim-clock, faked: records the run's calls, steps by hand at 0.025 s.</summary>
        private sealed class FakeSimClock : ISimClock, ISimRunControl
        {
            public readonly List<string> Calls = new List<string>();
            public readonly List<string> Owners = new List<string>();
            private readonly List<Action<SimStep>> _onStep = new List<Action<SimStep>>();
            private readonly List<Action> _queue = new List<Action>();
            private bool _gameplay;

            public int QueuedCount => _queue.Count;
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * 0.025;
            public int Seed => 1;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                Owners.Add(owner);
                _onStep.Add(onStep);
                return new Unsubscribe(() => _onStep.Remove(onStep));
            }

            public void Enqueue(string owner, Action apply) => _queue.Add(apply);

            public void RunQueued()
            {
                var queued = _queue.ToArray();
                _queue.Clear();
                foreach (var apply in queued) apply();
            }

            public void Step(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    StepCount++;
                    RunQueued();
                    if (_gameplay) GameplayStepCount++;
                    var step = new SimStep(StepCount, GameplayStepCount, 0.025f, _gameplay);
                    foreach (var onStep in _onStep.ToArray()) onStep(step);
                }
            }

            public void SetUpRun() { Calls.Add("SetUpRun"); StepCount = 0; GameplayStepCount = 0; _queue.Clear(); }
            public void StartRun() => Calls.Add("StartRun");
            public void SetGameplay(bool playing) { _gameplay = playing; Calls.Add("Gameplay:" + playing); }
            public void ClearRun() { Calls.Add("ClearRun"); _queue.Clear(); }

            private sealed class Unsubscribe : IDisposable
            {
                private Action _undo;
                public Unsubscribe(Action undo) => _undo = undo;
                public void Dispose() { _undo?.Invoke(); _undo = null; }
            }
        }

        /// <summary>feature-save, faked. Stored null: "none yet".</summary>
        private sealed class FakeSave : ISavedRecord
        {
            public int? Stored;
            public readonly List<int> Writes = new List<int>();

            public bool TryRead(out int record)
            {
                record = Stored ?? 0;
                return Stored.HasValue;
            }

            public bool Write(int record)
            {
                Stored = record;
                Writes.Add(record);
                return true;
            }
        }

        private sealed class FakePopupStack : IPopupStack
        {
            public IReadOnlyList<PopupId> OpenIds => Array.Empty<PopupId>();
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
