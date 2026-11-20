using UnityEngine;
using UnityEngine.UI;

namespace Game.Wallet
{
    /// <summary>Shows the current coin count. Holds no game state: it only mirrors WalletLogic.</summary>
    internal sealed class WalletCoinsLabel : MonoBehaviour
    {
        [SerializeField] private Text label;

        private WalletLogic logic;

        private void OnEnable()
        {
            logic = WalletRuntime.Logic;
            if (logic == null) return;

            logic.CoinsChanged += OnCoinsChanged;
            Show(logic.Coins);
        }

        private void OnDisable()
        {
            if (logic != null) logic.CoinsChanged -= OnCoinsChanged;
            logic = null;
        }

        private void OnCoinsChanged(CoinsChanged change)
        {
            Show(change.Current);
        }

        private void Show(int coins)
        {
            if (label != null) label.text = coins.ToString();
        }
    }
}
