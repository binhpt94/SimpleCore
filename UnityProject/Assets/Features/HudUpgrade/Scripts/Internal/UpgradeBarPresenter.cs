using System;
using System.Collections.Generic;
using Game.Hero;
using Game.Run;
using Game.Upgrade;

namespace Game.HudUpgrade
{
    /// <summary>
    /// Feeds the upgrade bar from the shop: one button per item in shop order, heroes then castle,
    /// level, shortened price and lit/dim from <see cref="UpgradeItem.CanBuy"/>. A tap on a lit button
    /// is one purchase order (<see cref="IUpgradeOrders"/>); the button bounces when that order was bought.
    /// The bar is shown from the battle's start until the lose popup.
    /// </summary>
    internal sealed class UpgradeBarPresenter : IDisposable
    {
        private const int HeroGroup = 0;
        private const int CastleGroup = 1;

        private readonly IUpgradeShop _shop;
        private readonly IUpgradeOrders _orders;
        private readonly IHeroRoster _roster;
        private readonly IRunFlow _run;
        private readonly IUpgradeBarView _view;
        private readonly IHeroDownState _down;
        private readonly List<ButtonPicture> _built = new List<ButtonPicture>();
        private readonly List<ButtonPicture> _scratch = new List<ButtonPicture>();
        private bool _started;

        public UpgradeBarPresenter(IUpgradeShop shop, IUpgradeOrders orders, IHeroRoster roster, IRunFlow run,
            IHeroDownState down, IUpgradeBarView view)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _down = down ?? throw new ArgumentNullException(nameof(down));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _shop.ItemsChanged += Refresh;
            _run.StepChanged += OnStepChanged;
            _orders.OrderApplied += OnOrderApplied;
            _down.DownChanged += OnDownChanged;
            Refresh();
        }

        public void Dispose()
        {
            if (!_started) return;
            _started = false;
            _shop.ItemsChanged -= Refresh;
            _run.StepChanged -= OnStepChanged;
            _orders.OrderApplied -= OnOrderApplied;
            _down.DownChanged -= OnDownChanged;
        }

        /// <summary>
        /// The player touched button <paramref name="index"/>: a lit button queues one purchase, applied at the
        /// next step of the shared clock, and the bounce waits for that step's outcome. A dim button sends nothing
        /// and shakes once.
        /// </summary>
        public void Tap(int index)
        {
            if (index < 0 || index >= _shop.Count) return;
            if (!_shop.Get(index).CanBuy)
            {
                _view.Shake(index);
                return;
            }

            _orders.Order(index);
        }

        private void OnDownChanged(int slot)
        {
            var count = Math.Min(_shop.Count, _built.Count);
            for (var i = 0; i < count; i++)
            {
                var item = _shop.Get(i);
                if (item.Kind == UpgradeKind.Hero && item.HeroSlot == slot) _view.SetDown(i, _down.IsDown(slot));
            }
        }

        private void OnOrderApplied(int index, PurchaseResult result)
        {
            if (result != PurchaseResult.Bought) return;
            if (index < 0 || index >= _built.Count) return;
            _view.Bounce(index);
            _view.FlashLevel(index);
        }

        private void Refresh()
        {
            var count = _shop.Count;
            _scratch.Clear();
            for (var i = 0; i < count; i++) _scratch.Add(PictureOf(_shop.Get(i)));

            if (!SameAsBuilt())
            {
                _built.Clear();
                _built.AddRange(_scratch);
                _view.Build(_built.ToArray(), Tap);
            }

            for (var i = 0; i < count; i++)
            {
                var item = _shop.Get(i);
                _view.SetButton(i, new ButtonLook("Cấp " + item.Level, PriceText.Format(item.NextPrice), item.CanBuy));
                if (item.Kind == UpgradeKind.Hero) _view.SetDown(i, _down.IsDown(item.HeroSlot));
            }

            UpdateVisibility(_run.Step);
        }

        private bool SameAsBuilt()
        {
            if (_scratch.Count != _built.Count) return false;
            for (var i = 0; i < _scratch.Count; i++)
                if (!_scratch[i].Equals(_built[i])) return false;
            return true;
        }

        private ButtonPicture PictureOf(UpgradeItem item)
        {
            switch (item.Kind)
            {
                case UpgradeKind.Hero:
                    var name = _roster.TryGet(item.HeroCode, out var hero) ? hero.Name : item.HeroCode;
                    return new ButtonPicture(ButtonPictureKind.Hero, item.HeroCode, name, HeroGroup);
                case UpgradeKind.CastleHealth:
                    return new ButtonPicture(ButtonPictureKind.CastleHealth, null, "Máu", CastleGroup);
                case UpgradeKind.CastleAttack:
                    return new ButtonPicture(ButtonPictureKind.CastleAttack, null, "Sức đánh", CastleGroup);
                default:
                    return new ButtonPicture(ButtonPictureKind.CastleSkill, null, "Kĩ năng", CastleGroup);
            }
        }

        private void OnStepChanged(RunStep step) => UpdateVisibility(step);

        /// <summary>
        /// Shown once the run is in battle and the buttons exist, whichever of the two comes last
        /// (the step and the item list arrive through separate events, in either order).
        /// </summary>
        private void UpdateVisibility(RunStep step) => _view.SetVisible(IsBattleOnScreen(step) && _built.Count > 0);

        /// <summary>From the battle's first frame until the lose popup, the ending included.</summary>
        private static bool IsBattleOnScreen(RunStep step) =>
            step == RunStep.Playing || step == RunStep.Ending;
    }
}
