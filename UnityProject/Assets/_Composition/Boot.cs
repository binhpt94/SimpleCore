using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Starts the game: runs each feature's installer in list order. The list on Boot.prefab is the
    /// boot order, one entry per feature. No game logic here.
    /// </summary>
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] private InstallerAsset[] installers;

        internal IReadOnlyList<InstallerAsset> Installers => installers;

        private void Awake()
        {
            Run();
        }

        internal IServiceRegistry Run()
        {
            var registry = new ServiceRegistry();
            foreach (var installer in installers)
            {
                installer.Install(registry);
            }
            return registry;
        }
    }
}
