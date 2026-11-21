using System;
using System.Collections.Generic;
using System.Threading;
using Game.Services.Popup;
using Game.Wallet;
using NUnit.Framework;

namespace Game.Shop.Tests
{
    public class ShopBuyPresenterTests
    {
        private FakeWallet wallet;
        private FakePopups popups;
        private FakeViewSource views;
        private ShopBuyPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            wallet = new FakeWallet { Coins = 100 };
            popups = new FakePopups();
            views = new FakeViewSource();
            var logic = new ShopLogic(wallet, popups, new[] { new ShopItem("gem_small", 30) });
            presenter = new ShopBuyPresenter(logic, views);
        }

        [Test]
        public void BuyView_IsNotOpenedBeforeTheSessionRuns()
        {
            presenter.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(views.Opened, Is.EqualTo(0));
        }

        [Test]
        public void BuyView_OpensOnceWhenTheSessionRuns()
        {
            presenter.Run();

            Assert.That(views.Opened, Is.EqualTo(1));
            Assert.That(views.Closed, Is.EqualTo(0));
        }

        [Test]
        public void PressingBuy_SpendsThePriceAndShowsThePurchasedPopup()
        {
            presenter.Run();

            views.View.PressBuy("gem_small");

            Assert.That(wallet.Coins, Is.EqualTo(70));
            Assert.That(popups.Shown, Is.EqualTo(new[] { ShopPopups.Purchased }));
        }

        [Test]
        public void PressingBuy_ForAnUnknownItem_SpendsNothing()
        {
            presenter.Run();

            views.View.PressBuy("nope");

            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(popups.Shown, Is.Empty);
        }

        [Test]
        public void EndingTheSession_ClosesTheBuyViewAndStopsListeningToIt()
        {
            presenter.Run();

            presenter.DisposeAsync().GetAwaiter().GetResult();
            views.View.PressBuy("gem_small");

            Assert.That(views.Closed, Is.EqualTo(1));
            Assert.That(views.View.HasListeners, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(100));
        }

        [Test]
        public void EndingTheSession_BeforeItRan_ClosesNothing()
        {
            presenter.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(views.Opened, Is.EqualTo(0));
            Assert.That(views.Closed, Is.EqualTo(0));
        }

        private sealed class FakeView : IShopBuyView
        {
            public event Action<string> BuyRequested;

            internal bool HasListeners => BuyRequested != null;

            internal void PressBuy(string itemId)
            {
                BuyRequested?.Invoke(itemId);
            }
        }

        private sealed class FakeViewSource : IShopBuyViewSource
        {
            internal readonly FakeView View = new FakeView();
            internal int Opened;
            internal int Closed;

            public IShopBuyView Open()
            {
                Opened++;
                return View;
            }

            public void Close()
            {
                Closed++;
            }
        }

        private sealed class FakeWallet : IWalletService
        {
            internal int Coins;

            public SpendResult TrySpend(int amount)
            {
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
