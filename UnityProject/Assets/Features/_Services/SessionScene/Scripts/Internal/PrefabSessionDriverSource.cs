using System;
using UnityEngine;

namespace Game.Services.SessionScene
{
    internal sealed class PrefabSessionDriverSource : ISessionDriverSource
    {
        private readonly GameObject prefab;

        internal PrefabSessionDriverSource(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public ISessionDriver Create()
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            var driver = instance.GetComponent<ISessionDriver>();
            if (driver == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no component implementing ISessionDriver (expected SessionSceneDriver).");
            }

            return driver;
        }
    }
}
