using UnityEngine;
using UnityEngine.UI;

namespace Game.Wallet
{
    /// <summary>Shows the coin count it is given. Knows nothing about the wallet itself.</summary>
    internal sealed class WalletCoinsLabel : MonoBehaviour, ICoinsDisplay
    {
        [SerializeField] private Text label;

        void ICoinsDisplay.ShowCoins(int coins)
        {
            if (label == null)
            {
                Debug.LogError("WalletCoinsLabel has no label assigned; the coin count cannot be shown.", this);
                return;
            }

            label.text = coins.ToString();
        }

        void ICoinsDisplay.Close()
        {
            Destroy(gameObject);
        }
    }
}
