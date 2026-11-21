using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Services.Popup;
using Game.Services.SessionScene;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Services.SessionScene.SessionState;

namespace Game.Shop.Tests
{
    public class ShopLogicTests
    {
        private FakeWallet wallet;
        private FakePopups popups;
        private ShopLogic shop;

        [SetUp]
        public void SetUp()
        {
            wallet = new FakeWallet { Coins = 100 };
            popups = new FakePopups();
            shop = new ShopLogic(wallet, popups,
                new[] { new ShopItem("gem_small", 30), new ShopItem("gem_large", 250) });
        }

        [Test]
        public void TryPurchase_AffordableItem_SpendsItsPriceAndShowsTheShopsPopup()
        {
            Assert.That(shop.TryPurchase("gem_small"), Is.EqualTo(PurchaseResult.Ok));

            Assert.That(wallet.Coins, Is.EqualTo(70));
            Assert.That(popups.Shown, Is.EqualTo(new[] { ShopPopups.Purchased }));
        }

        [Test]
        public void TryPurchase_TooExpensive_SpendsNothingAndShowsNoPopup()
        {
            Assert.That(shop.TryPurchase("gem_large"), Is.EqualTo(PurchaseResult.NotEnoughCoins));

            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(popups.Shown, Is.Empty);
        }

        [TestCase("nope")]
        [TestCase("")]
        [TestCase(null)]
        public void TryPurchase_UnknownItem_NeverAsksTheWallet(string itemId)
        {
            Assert.That(shop.TryPurchase(itemId), Is.EqualTo(PurchaseResult.UnknownItem));

            Assert.That(wallet.SpendRequests, Is.Empty);
            Assert.That(popups.Shown, Is.Empty);
        }

        [Test]
        public void TryPurchase_Twice_SecondFailsWhenCoinsRunOut()
        {
            wallet.Coins = 50;

            Assert.That(shop.TryPurchase("gem_small"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(shop.TryPurchase("gem_small"), Is.EqualTo(PurchaseResult.NotEnoughCoins));

            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(popups.Shown, Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallerAsset_JoinsTheSessionWithAShopBuiltFromTheConfigAsset()
        {
            var session = new FakeSession();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IWalletService>(wallet);
            registry.Register<IPopupService>(popups);

            LoadInstallerAsset().Install(registry);

            Assert.That(session.Joined, Has.Count.EqualTo(1), "the shop should join the session exactly once");
            Assert.That(session.Joined[0], Is.InstanceOf<ShopBuyPresenter>());
            ((ShopBuyPresenter)session.Joined[0]).Buy("gem_small");
            Assert.That(wallet.Coins, Is.EqualTo(70), "price of gem_small should come from ShopConfig.asset");
            Assert.That(popups.Shown, Is.EqualTo(new[] { new PopupId("Shop/Purchased") }));
        }

        [Test]
        public void InstallerAsset_WithoutSession_FailsWithAMessageNamingTheMissingContract()
        {
            var registry = new ServiceRegistry();
            registry.Register<IWalletService>(wallet);
            registry.Register<IPopupService>(popups);

            var error = Assert.Throws<System.InvalidOperationException>(() => LoadInstallerAsset().Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(ISessionScene)));
        }

        [Test]
        public void InstallerAsset_WithoutWallet_FailsWithAMessageNamingTheMissingContract()
        {
            var session = new FakeSession();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IPopupService>(popups);

            var error = Assert.Throws<System.InvalidOperationException>(() => LoadInstallerAsset().Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(IWalletService)));
            Assert.That(session.Joined, Is.Empty, "a shop that could not be built must not join the session");
        }

        [Test]
        public void Install_WithoutConfig_FailsWithAMessageNamingTheConfig()
        {
            var installer = ScriptableObject.CreateInstance<ShopInstaller>();

            var error = Assert.Throws<System.InvalidOperationException>(
                () => installer.Install(new ServiceRegistry()));

            Assert.That(error.Message, Does.Contain(nameof(ShopConfig)));
            Object.DestroyImmediate(installer);
        }

        private static ShopInstaller LoadInstallerAsset()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ShopInstaller>(
                "Assets/Features/Shop/FeatureAssets/Public/ShopInstaller.asset");
            Assert.That(installer, Is.Not.Null, "ShopInstaller.asset missing or its script reference is broken");
            return installer;
        }

        private sealed class FakeSession : ISessionScene
        {
            internal readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event System.Action<SessionState> StateChanged
            {
                add { }
                remove { }
            }

            public void Join(ISessionParticipant participant)
            {
                Joined.Add(participant);
            }

            public UniTask EndAsync()
            {
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeWallet : IWalletService
        {
            internal int Coins;
            internal readonly List<int> SpendRequests = new List<int>();

            public SpendResult TrySpend(int amount)
            {
                SpendRequests.Add(amount);
                if (amount <= 0) return SpendResult.InvalidAmount;
                if (amount > Coins) return SpendResult.NotEnoughCoins;
                Coins -= amount;
                return SpendResult.Ok;
            }
        }

        private sealed class FakePopups : IPopupService
        {
            internal readonly List<PopupId> Shown = new List<PopupId>();

            public PopupHandle Show(PopupId id)
            {
                Shown.Add(id);
                return new PopupHandle(Shown.Count);
            }

            public void Close(PopupHandle handle)
            {
            }
        }
    }
}
