using System;
using UnityEngine;

namespace Game.Wallet
{
    internal sealed class PrefabCoinsDisplaySource : ICoinsDisplaySource
    {
        private readonly GameObject prefab;

        internal PrefabCoinsDisplaySource(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public ICoinsDisplay Create()
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            var display = instance.GetComponentInChildren<ICoinsDisplay>(true);
            if (display == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no component implementing ICoinsDisplay (expected WalletCoinsLabel).");
            }

            return display;
        }
    }
}
