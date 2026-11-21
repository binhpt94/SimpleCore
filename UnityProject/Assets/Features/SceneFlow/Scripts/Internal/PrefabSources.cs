using System;
using UnityEngine;

namespace Game.SceneFlow
{
    internal sealed class PrefabLoadingScreenSource : ILoadingScreenSource
    {
        private readonly GameObject prefab;

        internal PrefabLoadingScreenSource(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public ILoadingScreen Create()
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            var screen = instance.GetComponentInChildren<ILoadingScreen>(true);
            if (screen == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no component implementing ILoadingScreen (expected LoadingScreenView).");
            }

            return screen;
        }
    }

    internal sealed class PrefabSceneGateCarrierSource : ISceneGateCarrierSource
    {
        private readonly GameObject prefab;

        internal PrefabSceneGateCarrierSource(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public ISceneGateCarrier Create()
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            var carrier = instance.GetComponent<ISceneGateCarrier>();
            if (carrier == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no component implementing ISceneGateCarrier (expected SceneGateCarrier).");
            }

            return carrier;
        }
    }
}
