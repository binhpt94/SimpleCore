using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Run
{
    /// <summary>Makes the device signals object from its prefab (a view in Scripts/View). Only while playing.</summary>
    internal static class UnityAppSignals
    {
        public static IAppSignals Create(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var instance = Object.Instantiate(prefab);
            instance.name = prefab.name;
            var signals = instance.GetComponent<IAppSignals>();
            if (signals != null) return signals;
            Object.Destroy(instance);
            throw new InvalidOperationException($"Prefab '{prefab.name}' has no component that hears the device (RunAppSignalsView).");
        }
    }
}
