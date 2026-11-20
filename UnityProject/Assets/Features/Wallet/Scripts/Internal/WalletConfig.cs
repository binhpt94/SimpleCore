using UnityEngine;

namespace Game.Wallet
{
    /// <summary>Tuning values for the wallet. Read-only at runtime.</summary>
    [CreateAssetMenu(menuName = "Game/Wallet/Config")]
    internal sealed class WalletConfig : ScriptableObject
    {
        [SerializeField] private int startCoins = 100;

        internal int StartCoins => startCoins;
    }
}
