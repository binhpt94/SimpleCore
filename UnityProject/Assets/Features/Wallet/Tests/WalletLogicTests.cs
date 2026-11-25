using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
// UnityEditor has its own SessionState class; the one meant here is the session contract's enum.
using SessionState = Game.Core.SessionState;

namespace Game.Wallet.Tests
{
    public class WalletLogicTests
    {
        // ---- start of a run ----

        [Test]
        public void StartsWithConfiguredCoins()
        {
            Assert.That(NewWallet(50).Coins, Is.EqualTo(50));
        }

        [Test]
        public void NegativeStartCoins_AreTreatedAsZero()
        {
            Assert.That(NewWallet(-5).Coins, Is.EqualTo(0));
        }

        [Test]
        public void NewRun_PutsCoinsBackToStartCoins_AndAnnouncesThem()
        {
            var wallet = NewWallet(50);
            wallet.Add(70);
            var balance = RecordBalance(wallet);

            wallet.ResetToStart();

            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.EqualTo(new[] { 50 }));
        }

        [Test]
        public void NewRun_WhenCoinsAlreadyEqualStartCoins_StillAnnouncesOnce()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            wallet.ResetToStart();

            Assert.That(balance, Is.EqualTo(new[] { 50 }));
        }

        [Test]
        public void WhenTheRunStarts_CoinsGoBackToStartCoins()
        {
            var wallet = NewWallet(50);
            var run = new FakeRunFlow();
            new WalletRunReset(wallet, run).Run();
            wallet.TrySpend(30);

            run.StartRun();

            Assert.That(wallet.Coins, Is.EqualTo(50));
        }

        [Test]
        public void AfterTheSessionEnds_ANewRunNoLongerResetsCoins()
        {
            var wallet = NewWallet(50);
            var run = new FakeRunFlow();
            var reset = new WalletRunReset(wallet, run);
            reset.Run();
            reset.DisposeAsync().Forget();
            wallet.TrySpend(30);

            run.StartRun();

            Assert.That(wallet.Coins, Is.EqualTo(20));
        }

        // ---- spending ----

        [Test]
        public void TrySpend_WithEnoughCoins_TakesThem()
        {
            var wallet = NewWallet(50);

            Assert.That(wallet.TrySpend(30), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(20));
        }

        [Test]
        public void TrySpend_ExactBalance_LeavesZero_AndAnnouncesZero()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            Assert.That(wallet.TrySpend(50), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(0));
            Assert.That(balance, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void TrySpend_MoreThanBalance_ChangesNothing()
        {
            var wallet = NewWallet(50);
            var events = Record(wallet);
            var balance = RecordBalance(wallet);

            Assert.That(wallet.TrySpend(51), Is.EqualTo(SpendResult.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(events, Is.Empty);
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void TwoQuickBuys_WithCoinsForOnlyOne_SecondIsNotEnough()
        {
            var wallet = NewWallet(50);

            Assert.That(wallet.TrySpend(30), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.TrySpend(30), Is.EqualTo(SpendResult.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(20));
        }

        [Test]
        public void TrySpend_PriceZero_IsOk_AndAnnouncesNothing()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            Assert.That(wallet.TrySpend(0), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void TrySpend_PriceZero_WithNoCoins_IsOk()
        {
            Assert.That(NewWallet(0).TrySpend(0), Is.EqualTo(SpendResult.Ok));
        }

        [Test]
        public void TrySpend_NegativePrice_IsRefusedAndLogged_AndChangesNothing()
        {
            var log = new FakeLog();
            var wallet = new WalletLogic(50, log);
            var balance = RecordBalance(wallet);

            Assert.That(wallet.TrySpend(-10), Is.EqualTo(SpendResult.InvalidAmount));
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.Empty);
            Assert.That(log.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void TrySpend_RaisesCoinsChangedWithBothValues()
        {
            var wallet = NewWallet(50);
            var events = Record(wallet);

            wallet.TrySpend(30);

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Previous, Is.EqualTo(50));
            Assert.That(events[0].Current, Is.EqualTo(20));
        }

        // ---- income ----

        [Test]
        public void Add_IncreasesCoins_AndAnnouncesTheNewBalance()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            wallet.Add(5);

            Assert.That(wallet.Coins, Is.EqualTo(55));
            Assert.That(balance, Is.EqualTo(new[] { 55 }));
        }

        [Test]
        public void Add_Zero_IsIgnored_AndAnnouncesNothing()
        {
            var log = new FakeLog();
            var wallet = new WalletLogic(50, log);
            var balance = RecordBalance(wallet);

            wallet.Add(0);

            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.Empty);
            Assert.That(log.Messages, Is.Empty);
        }

        [Test]
        public void Add_Negative_IsRefusedAndLogged_AndChangesNothing()
        {
            var log = new FakeLog();
            var wallet = new WalletLogic(50, log);
            var balance = RecordBalance(wallet);

            wallet.Add(-5);

            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.Empty);
            Assert.That(log.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_PastTheCap_StopsAtMaxCoins_AndAnnouncesIt()
        {
            var wallet = NewWallet(WalletLogic.MaxCoins - 10);
            var balance = RecordBalance(wallet);

            wallet.Add(int.MaxValue);

            Assert.That(wallet.Coins, Is.EqualTo(999_999_999));
            Assert.That(balance, Is.EqualTo(new[] { 999_999_999 }));
        }

        [Test]
        public void Add_AtTheCap_ChangesNothing_AndAnnouncesNothing()
        {
            var wallet = NewWallet(WalletLogic.MaxCoins);
            var balance = RecordBalance(wallet);

            wallet.Add(1);

            Assert.That(wallet.Coins, Is.EqualTo(WalletLogic.MaxCoins));
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void AddThenSpendInOneFrame_EachChangeIsAnnounced_LastIsTheBalance()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            wallet.Add(5);
            wallet.TrySpend(30);

            Assert.That(balance, Is.EqualTo(new[] { 55, 25 }));
        }

        [Test]
        public void EachIncomeCall_AddsExactlyTheGivenAmount_Once()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            wallet.Add(5);
            wallet.Add(5);
            wallet.Add(5);

            Assert.That(wallet.Coins, Is.EqualTo(65));
            Assert.That(balance, Is.EqualTo(new[] { 55, 60, 65 }));
        }

        [Test]
        public void SeveralIncomesInOneFrame_AreAddedInCallOrder_EachAnnounced()
        {
            var wallet = NewWallet(50);
            var balance = RecordBalance(wallet);

            // last enemy of a wave falls: its coins arrive first, then the wave-clear bonus
            wallet.Add(5);
            wallet.Add(25);

            Assert.That(wallet.Coins, Is.EqualTo(80));
            Assert.That(balance, Is.EqualTo(new[] { 55, 80 }));
        }

        [Test]
        public void IncomeAfterTheRunEnded_IsIgnored_AndAnnouncesNothing()
        {
            var wallet = NewWallet(50);
            var run = new FakeRunFlow();
            new WalletRunReset(wallet, run).Run();
            var balance = RecordBalance(wallet);

            run.StopBattle();
            wallet.Add(5);

            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void EnemyFallingInTheFrameTheCastleFalls_GivesNoCoinsOnceTheRunStopped()
        {
            var wallet = NewWallet(50);
            var run = new FakeRunFlow();
            new WalletRunReset(wallet, run).Run();

            wallet.Add(5);   // reached the wallet before the castle fell
            run.StopBattle();
            wallet.Add(6);   // reached the wallet after the run ended

            Assert.That(wallet.Coins, Is.EqualTo(55));
        }

        [Test]
        public void IncomeFromTheOldRun_DoesNotCarryIntoTheNewRun_AndNewIncomeCountsAgain()
        {
            var wallet = NewWallet(50);
            var run = new FakeRunFlow();
            new WalletRunReset(wallet, run).Run();
            wallet.TrySpend(30);

            run.StopBattle();
            wallet.Add(15);
            run.StartRun();

            Assert.That(wallet.Coins, Is.EqualTo(50));

            wallet.Add(5);

            Assert.That(wallet.Coins, Is.EqualTo(55));
        }

        [Test]
        public void Income_AddsWhateverAmountTheCallerGives_WithNoTableOfItsOwn()
        {
            var wallet = NewWallet(50);

            wallet.Add(100);
            wallet.Add(4);
            wallet.Add(37);

            Assert.That(wallet.Coins, Is.EqualTo(191));
        }

        // ---- installer ----

        [Test]
        public void InstallerAsset_RegistersWalletContractsWithCoinsFromConfigAsset_AndJoinsTheSession()
        {
            var installer = LoadInstaller();
            var session = new FakeSession();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRunFlow());

            installer.Install(registry);

            var balance = registry.Resolve<IWalletBalance>();
            Assert.That(balance.Coins, Is.EqualTo(50), "start coins should come from WalletConfig.asset");
            Assert.That(registry.Resolve<IWalletService>(), Is.SameAs(balance));
            Assert.That(registry.Resolve<IWalletIncome>(), Is.SameAs(balance));
            Assert.That(session.Joined, Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallerAsset_WithoutASessionInstalledFirst_FailsAtInstall()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRunFlow>(new FakeRunFlow());

            Assert.That(() => LoadInstaller().Install(registry), Throws.Exception);
        }

        [Test]
        public void InstallerAsset_WithoutTheRunInstalledFirst_FailsAtInstall()
        {
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());

            Assert.That(() => LoadInstaller().Install(registry), Throws.Exception);
        }

        [Test]
        public void Installer_WithoutConfig_FailsAtInstall_InsteadOfStartingWithZeroCoins()
        {
            var installer = ScriptableObject.CreateInstance<WalletInstaller>();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRunFlow());

            try
            {
                var failure = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
                Assert.That(failure.Message, Does.Contain("config"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        // ---- helpers and fakes ----

        private static WalletLogic NewWallet(int startCoins)
        {
            return new WalletLogic(startCoins, new FakeLog());
        }

        private static WalletInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<WalletInstaller>(
                "Assets/Features/Wallet/FeatureAssets/Public/WalletInstaller.asset");
            Assert.That(installer, Is.Not.Null, "WalletInstaller.asset missing or its script reference is broken");
            return installer;
        }

        private static List<CoinsChanged> Record(WalletLogic wallet)
        {
            var events = new List<CoinsChanged>();
            wallet.CoinsChanged += events.Add;
            return events;
        }

        private static List<int> RecordBalance(IWalletBalance wallet)
        {
            var values = new List<int>();
            wallet.CoinsChanged += values.Add;
            return values;
        }

        private sealed class FakeLog : IWalletLog
        {
            public readonly List<string> Messages = new List<string>();

            public void Refused(string message)
            {
                Messages.Add(message);
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

#pragma warning disable CS0067 // the wallet never listens to session state
            public event Action<SessionState> StateChanged;
#pragma warning restore CS0067

            public void Join(ISessionParticipant participant)
            {
                Joined.Add(participant);
            }

            public UniTask EndAsync()
            {
                return UniTask.CompletedTask;
            }
        }

#pragma warning disable CS0067 // only RunStarted matters to the wallet
        private sealed class FakeRunFlow : IRunFlow
        {
            public RunStep Step => RunStep.Home;
            public event Action<RunStep> StepChanged;
            public RunTeam Team => default;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
            public bool IsPaused => false;

            public void StartRun()
            {
                RunStarted?.Invoke(new RunTeam(new[] { "hero" }));
            }

            public void StopBattle()
            {
                BattleStopped?.Invoke();
            }

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
#pragma warning restore CS0067
    }
}
