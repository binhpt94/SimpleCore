using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEditor;

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
        public void InstallerAsset_RegistersWalletWithCoinsFromConfigAsset()
        {
            var installer = AssetDatabase.LoadAssetAtPath<WalletInstaller>(
                "Assets/Features/Wallet/FeatureAssets/Public/WalletInstaller.asset");
            Assert.That(installer, Is.Not.Null, "WalletInstaller.asset missing or its script reference is broken");
            var registry = new ServiceRegistry();

            installer.Install(registry);

            Assert.That(registry.Resolve<IWalletService>(), Is.SameAs(WalletRuntime.Logic));
            Assert.That(WalletRuntime.Logic.Coins, Is.EqualTo(100), "start coins should come from WalletConfig.asset");
        }

        private static List<CoinsChanged> Record(WalletLogic wallet)
        {
            var events = new List<CoinsChanged>();
            wallet.CoinsChanged += events.Add;
            return events;
        }
    }
}
