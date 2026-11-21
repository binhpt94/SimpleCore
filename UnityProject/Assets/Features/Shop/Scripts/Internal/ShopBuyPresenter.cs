using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.SessionScene;

namespace Game.Shop
{
    /// <summary>Opens the buy view when the session runs and passes each "buy" on to the shop logic.</summary>
    internal sealed class ShopBuyPresenter : ISessionParticipant
    {
        private readonly ShopLogic logic;
        private readonly IShopBuyViewSource views;
        private IShopBuyView view;

        internal ShopBuyPresenter(ShopLogic logic, IShopBuyViewSource views)
        {
            this.logic = logic;
            this.views = views;
        }

        public UniTask LoadAsync(CancellationToken cancellation)
        {
            return UniTask.CompletedTask;
        }

        public void Run()
        {
            view = views.Open();
            view.BuyRequested += Buy;
        }

        public UniTask DisposeAsync()
        {
            // The session also disposes participants that never ran; then there is no view to close.
            if (view != null)
            {
                view.BuyRequested -= Buy;
                view = null;
                views.Close();
            }
            return UniTask.CompletedTask;
        }

        internal void Buy(string itemId)
        {
            logic.TryPurchase(itemId);
        }
    }
}
