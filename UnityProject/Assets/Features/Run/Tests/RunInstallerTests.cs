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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void InstalledRunPublishesACheatStartWithNoBattleRequested()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer);
            var registry = Registry(new FakeSession());

            installer.Install(registry);

            Assert.IsTrue(registry.TryResolve<IRunCheatStart>(out var start));
            Assert.IsFalse(start.BattleRequested);
            Assert.That(start.Team.Count, Is.EqualTo(3), "Default team of RunConfig.");
        }
#endif

        /// <summary>Installs the real asset; menus are skipped (IDEA), so Begin goes straight into battle with the default team.</summary>
        private static RunFlow InstalledRunInBattle(FakeSimClock clock)
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");
            var registry = Registry(clock: clock);
            installer.Install(registry);
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            flow.Begin();
            Assert.AreEqual(RunStep.Playing, flow.Step);
            return flow;
        }

        [Test]
        public void ConfigAssetSkipsMenusAndHoldsTheDefaultTeam()
        {
            // People decided 2026-11-27: build is at IDEA, the hand-set skipMenus flag is on (turned off at PROTOTYPE).
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");

            Assert.AreEqual(1f, config.LoseDelaySeconds, "flow-rules: 1 s before the lose popup.");
            Assert.AreEqual(3, config.TeamSize);
            Assert.IsTrue(config.SkipMenus, "IDEA: the run goes straight into battle (no Home, TeamSelect, GameFlow).");
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.DefaultTeam.HeroCodes,
                "feature-team-select/team-memory/memory-rules: default team (IDEA battle, dev cheats).");
        }

        [Test]
        public void InstalledIdeaRunReplaysStraightIntoANewBattleWithTheDefaultTeam()
        {
            // People decided 2026-11-27: build is at IDEA (no Home, TeamSelect, GameFlow), so menus are skipped.
            var clock = new FakeSimClock();
            var flow = InstalledRunInBattle(clock);

            flow.Lose();
            clock.Step(40);
            Assert.AreEqual(RunStep.Lost, flow.Step);
            flow.Replay();
            Assert.AreEqual(RunStep.Playing, flow.Step, "IDEA: Play again goes straight into a new battle.");
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, flow.Team.HeroCodes);
            flow.Shutdown();
        }

        private static readonly RunTeam ChosenTeam = new RunTeam(new[] { "knight", "axeman", "archer" });

        /// <summary>
        /// PROTOTYPE copy of the real assets: RunConfig.asset and RunInstaller.asset instantiated, skipMenus turned off
        /// through SerializedObject, every other value of the asset kept. Destroy both with <see cref="DestroyPrototype"/>.
        /// </summary>
        private static RunInstaller PrototypeInstaller(out RunConfig config)
        {
            var realInstaller = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var realConfig = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(realInstaller, $"Missing {InstallerPath}");
            Assert.IsNotNull(realConfig, $"Missing {ConfigPath}");

            config = UnityEngine.Object.Instantiate(realConfig);
            var configObject = new SerializedObject(config);
            configObject.FindProperty("skipMenus").boolValue = false;
            configObject.ApplyModifiedPropertiesWithoutUndo();

            var installer = UnityEngine.Object.Instantiate(realInstaller);
            var installerObject = new SerializedObject(installer);
            installerObject.FindProperty("config").objectReferenceValue = config;
            installerObject.ApplyModifiedPropertiesWithoutUndo();
            return installer;
        }

        private static void DestroyPrototype(RunInstaller installer, RunConfig config)
        {
            UnityEngine.Object.DestroyImmediate(installer);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ConfigAssetHoldsThePrototypeRunValues()
        {
            // "Thông số theo giai đoạn", PROTOTYPE column of flow-rules and start-values: numbers read from the real asset.
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");
            Assert.AreEqual(1f, config.LoseDelaySeconds, "flow-rules: 1 s before the lose popup.");
            Assert.AreEqual(3, config.TeamSize, "start-values: team of 3.");
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.DefaultTeam.HeroCodes);

            // The PROTOTYPE copy keeps those numbers, only menus are no longer skipped.
            var installer = PrototypeInstaller(out var copy);
            try
            {
                Assert.IsFalse(copy.SkipMenus);
                Assert.AreEqual(config.LoseDelaySeconds, copy.LoseDelaySeconds);
                Assert.AreEqual(config.TeamSize, copy.TeamSize);
                CollectionAssert.AreEqual(config.DefaultTeam.HeroCodes, copy.DefaultTeam.HeroCodes);
                Assert.AreSame(config.AppSignalsPrefab, copy.AppSignalsPrefab);
            }
            finally { DestroyPrototype(installer, copy); }
        }

        [Test]
        public void InstalledPrototypeRunBuildsTheBattleTheScreenFlowAsksForAndHandsBackTheLeaveChoice()
        {
            // flow-rules "Thông số theo giai đoạn", PROTOTYPE: feature-game-flow picks the screens; the run builds,
            // clears and hands back the leave choice.
            var installer = PrototypeInstaller(out var config);
            try
            {
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
            finally { DestroyPrototype(installer, config); }
        }

        [Test]
        public void PrototypeRunHasBackToStart()
        {
            var installer = PrototypeInstaller(out var config);
            try
            {
                var clock = new FakeSimClock();
                var registry = Registry(clock: clock);
                installer.Install(registry);
                var flow = (RunFlow)registry.Resolve<IRunFlow>();
                flow.Begin();
                flow.Play();
                flow.EnterBattle(ChosenTeam);

                flow.Lose();
                clock.Step(40);
                Assert.AreEqual(RunStep.Lost, flow.Step);
                flow.GoHome();
                Assert.AreEqual(RunStep.Home, flow.Step, "Back to start on the lose popup goes to the start screen.");
                flow.Shutdown();
            }
            finally { DestroyPrototype(installer, config); }
        }

        [Test]
        public void InstalledRunUsesTheConfigAndOpensOnTheStartScreen()
        {
            var installer = PrototypeInstaller(out var config);
            try
            {
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
            finally { DestroyPrototype(installer, config); }
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
        public void InstalledRunUsesTheConfigAndOpensStraightInBattle()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = Registry();
            installer.Install(registry);

            var flow = registry.Resolve<IRunFlow>() as RunFlow;
            Assert.IsNotNull(flow, "IRunFlow is not the run's RunFlow.");
            Assert.AreEqual(1f, flow.LoseDelaySeconds);
            Assert.AreEqual(3, flow.TeamSize);
            Assert.IsTrue(flow.SkipMenus);

            flow.Begin();
            Assert.AreEqual(RunStep.Playing, flow.Step, "IDEA: the Game scene opens straight in battle.");
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, flow.Team.HeroCodes,
                "The battle uses the default team of RunConfig.");
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
