using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.SessionScene;

namespace Game.Wallet
{
    /// <summary>
    /// Keeps the coin display in step with the wallet. The display exists only while the session
    /// runs: it is created when the session starts running and closed when the session ends.
    /// </summary>
    internal sealed class WalletCoinsPresenter : ISessionParticipant
    {
        private readonly WalletLogic logic;
        private readonly ICoinsDisplaySource displays;

        private ICoinsDisplay display;

        internal WalletCoinsPresenter(WalletLogic logic, ICoinsDisplaySource displays)
        {
            this.logic = logic;
            this.displays = displays;
        }

        public UniTask LoadAsync(CancellationToken cancellation)
        {
            return UniTask.CompletedTask;
        }

        public void Run()
        {
            display = displays.Create();
            display.ShowCoins(logic.Coins);
            logic.CoinsChanged += OnCoinsChanged;
        }

        public UniTask DisposeAsync()
        {
            if (display != null)
            {
                logic.CoinsChanged -= OnCoinsChanged;
                display.Close();
                display = null;
            }

            return UniTask.CompletedTask;
        }

        private void OnCoinsChanged(CoinsChanged change)
        {
            display.ShowCoins(change.Current);
        }
    }
}
