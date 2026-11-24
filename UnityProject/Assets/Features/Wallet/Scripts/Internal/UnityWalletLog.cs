using UnityEngine;

namespace Game.Wallet
{
    /// <summary>Writes refused wallet requests to the Unity console. Does not stop the game.</summary>
    internal sealed class UnityWalletLog : IWalletLog
    {
        public void Refused(string message)
        {
            Debug.LogError(message);
        }
    }
}
