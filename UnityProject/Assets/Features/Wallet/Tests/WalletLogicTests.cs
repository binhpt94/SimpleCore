using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Services.SessionScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
// UnityEditor has its own SessionState class; the one meant here is the session contract's enum.
using SessionState = Game.Services.SessionScene.SessionState;

namespace Game.Wallet.Tests
{
    public class WalletLogicTests
    {
        [Test]
        public void StartsWithConfiguredCoins()
        {
            Assert.That(new WalletLogic(100).Coins, Is.EqualTo(100));
        }

        [Test]
        public void NegativeStartCoins_AreTreatedAsZero()
        {
            Assert.That(new WalletLogic(-5).Coins, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_WithEnoughCoins_TakesThem()
        {
            var wallet = new WalletLogic(100);

            Assert.That(wallet.TrySpend(30), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(70));
        }

        [Test]
        public void TrySpend_ExactBalance_LeavesZero()
        {
            var wallet = new WalletLogic(100);

            Assert.That(wallet.TrySpend(100), Is.EqualTo(SpendResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_MoreThanBalance_ChangesNothing()
        {
            var wallet = new WalletLogic(100);
            var events = Record(wallet);

            Assert.That(wallet.TrySpend(101), Is.EqualTo(SpendResult.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(events, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void TrySpend_NonPositiveAmount_IsInvalidAndChangesNothing(int amount)
        {
            var wallet = new WalletLogic(100);
            var events = Record(wallet);

            Assert.That(wallet.TrySpend(amount), Is.EqualTo(SpendResult.InvalidAmount));
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void TrySpend_RaisesCoinsChangedWithBothValues()
        {
            var wallet = new WalletLogic(100);
            var events = Record(wallet);

            wallet.TrySpend(30);

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Previous, Is.EqualTo(100));
            Assert.That(events[0].Current, Is.EqualTo(70));
        }

        [Test]
        public void Grant_AddsCoinsAndRaisesCoinsChanged()
        {
            var wallet = new WalletLogic(100);
            var events = Record(wallet);

            Assert.That(wallet.Grant(50), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(150));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Current, Is.EqualTo(150));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Grant_NonPositiveAmount_IsRefused(int amount)
        {
            var wallet = new WalletLogic(100);

            Assert.That(wallet.Grant(amount), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(100));
        }

        [Test]
        public void NoCoinDisplayExists_BeforeTheSessionRuns()
        {
            var displays = new FakeDisplays();
            var presenter = new WalletCoinsPresenter(new WalletLogic(100), displays);

            Assert.That(displays.Display, Is.Null);
            Assert.That(presenter, Is.Not.Null);
        }

        [Test]
        public void WhenTheSessionRuns_TheDisplayShowsTheCurrentCoins()
        {
            var displays = new FakeDisplays();
            var presenter = new WalletCoinsPresenter(new WalletLogic(100), displays);

            presenter.Run();

            Assert.That(displays.Display.Shown, Is.EqualTo(new[] { 100 }));
        }

        [Test]
        public void WhenCoinsChange_TheDisplayFollows()
        {
            var wallet = new WalletLogic(100);
            var displays = new FakeDisplays();
            new WalletCoinsPresenter(wallet, displays).Run();

            wallet.TrySpend(30);
            wallet.Grant(5);

            Assert.That(displays.Display.Shown, Is.EqualTo(new[] { 100, 70, 75 }));
        }

        [Test]
        public void WhenTheSessionEnds_TheDisplayCloses_AndLaterChangesAreNotShown()
        {
            var wallet = new WalletLogic(100);
            var displays = new FakeDisplays();
            var presenter = new WalletCoinsPresenter(wallet, displays);
            presenter.Run();
            var display = displays.Display;

            presenter.DisposeAsync().Forget();
            wallet.TrySpend(30);

            Assert.That(display.Closed, Is.True);
            Assert.That(display.Shown, Is.EqualTo(new[] { 100 }));
        }

        [Test]
        public void EndingASessionThatNeverRan_ClosesNothing()
        {
            var presenter = new WalletCoinsPresenter(new WalletLogic(100), new FakeDisplays());

            Assert.DoesNotThrow(() => presenter.DisposeAsync().Forget());
        }

        [Test]
        public void InstallerAsset_RegistersWalletWithCoinsFromConfigAsset_AndJoinsTheSession()
        {
            var installer = LoadInstaller();
            var session = new FakeSession();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);

            // Also proves the coin label prefab is assigned: Install throws without it.
            installer.Install(registry);

            var wallet = (WalletLogic)registry.Resolve<IWalletService>();
            Assert.That(wallet.Coins, Is.EqualTo(100), "start coins should come from WalletConfig.asset");
            Assert.That(session.Joined, Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallerAsset_WithoutASessionInstalledFirst_FailsAtInstall()
        {
            Assert.That(() => LoadInstaller().Install(new ServiceRegistry()), Throws.Exception);
        }

        [Test]
        public void Installer_WithoutConfig_FailsAtInstall_InsteadOfStartingWithZeroCoins()
        {
            var installer = ScriptableObject.CreateInstance<WalletInstaller>();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());

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

        private static WalletInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<WalletInstaller>(
                "Assets/Features/Wallet/FeatureAssets/Public/WalletInstaller.asset");
            Assert.That(installer, Is.Not.Null, "WalletInstaller.asset missing or its script reference is broken");
            return installer;
        }

        private sealed class FakeDisplays : ICoinsDisplaySource
        {
            public FakeDisplay Display { get; private set; }

            public ICoinsDisplay Create()
            {
                Display = new FakeDisplay();
                return Display;
            }
        }

        private sealed class FakeDisplay : ICoinsDisplay
        {
            public readonly List<int> Shown = new List<int>();

            public bool Closed { get; private set; }

            public void ShowCoins(int coins)
            {
                Shown.Add(coins);
            }

            public void Close()
            {
                Closed = true;
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

        private static List<CoinsChanged> Record(WalletLogic wallet)
        {
            var events = new List<CoinsChanged>();
            wallet.CoinsChanged += events.Add;
            return events;
        }
    }
}
