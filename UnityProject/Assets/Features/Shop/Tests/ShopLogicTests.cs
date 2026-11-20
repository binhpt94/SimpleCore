using System.Collections.Generic;
using Game.Core;
using Game.Services.Popup;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Shop.Tests
{
    public class ShopLogicTests
    {
        private FakeWallet wallet;
        private FakePopups popups;
        private GameObject popupPrefab;
        private ShopLogic shop;

        [SetUp]
        public void SetUp()
        {
            wallet = new FakeWallet { Coins = 100 };
            popups = new FakePopups();
            popupPrefab = new GameObject("PurchasedPopup");
            shop = new ShopLogic(wallet, popups,
                new[] { new ShopItem("gem_small", 30), new ShopItem("gem_large", 250) }, popupPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(popupPrefab);
        }

        [Test]
        public void TryPurchase_AffordableItem_SpendsItsPriceAndShowsTheShopsPopup()
        {
            Assert.That(shop.TryPurchase("gem_small"), Is.EqualTo(PurchaseResult.Ok));

            Assert.That(wallet.Coins, Is.EqualTo(70));
            Assert.That(popups.Shown, Has.Count.EqualTo(1));
            Assert.That(popups.Shown[0].Prefab, Is.SameAs(popupPrefab));
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
        public void InstallerAsset_BuildsShopFromConfigAsset()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ShopInstaller>(
                "Assets/Features/Shop/FeatureAssets/Public/ShopInstaller.asset");
            Assert.That(installer, Is.Not.Null, "ShopInstaller.asset missing or its script reference is broken");
            var registry = new ServiceRegistry();
            registry.Register<IWalletService>(wallet);
            registry.Register<IPopupService>(popups);

            installer.Install(registry);

            Assert.That(ShopRuntime.Logic.TryPurchase("gem_small"), Is.EqualTo(PurchaseResult.Ok));
            Assert.That(wallet.Coins, Is.EqualTo(70), "price of gem_small should come from ShopConfig.asset");
            Assert.That(popups.Shown[0].Prefab, Is.Not.Null, "popup prefab should come from ShopConfig.asset");
            Assert.That(popups.Shown[0].Prefab.name, Is.EqualTo("PurchasedPopup"));
        }

        [Test]
        public void Install_WithoutWallet_FailsWithAMessageNamingTheMissingContract()
        {
            var installer = ScriptableObject.CreateInstance<ShopInstaller>();
            var registry = new ServiceRegistry();

            var error = Assert.Throws<System.InvalidOperationException>(() => installer.Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(IWalletService)));
            Object.DestroyImmediate(installer);
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
            internal readonly List<PopupRequest> Shown = new List<PopupRequest>();

            public PopupHandle Show(PopupRequest request)
            {
                Shown.Add(request);
                return new PopupHandle(Shown.Count);
            }

            public void Close(PopupHandle handle)
            {
            }
        }
    }
}
