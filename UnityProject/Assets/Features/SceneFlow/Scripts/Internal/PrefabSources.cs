using System;
using UnityEngine;

namespace Game.SceneFlow
{
    internal sealed class PrefabLoadingScreenSource : ILoadingScreenSource
    {
        private readonly GameObject prefab;
        private readonly bool keepAcrossScenes;

        /// <param name="keepAcrossScenes">True for the switch cover: it must outlive the scene it was opened in.</param>
        internal PrefabLoadingScreenSource(GameObject prefab, bool keepAcrossScenes)
        {
            this.prefab = prefab;
            this.keepAcrossScenes = keepAcrossScenes;
        }

        public ILoadingScreen Create()
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            if (keepAcrossScenes) UnityEngine.Object.DontDestroyOnLoad(instance);
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
