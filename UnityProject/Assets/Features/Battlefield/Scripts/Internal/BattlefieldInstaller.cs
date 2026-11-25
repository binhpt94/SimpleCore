using System;
using Game.Core;
using Game.Run;
using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>Wires the battle backdrop: needs the run (when the battle is set up and left) and the scene session.</summary>
    [CreateAssetMenu(menuName = "Game/Battlefield/Installer", fileName = "BattlefieldInstaller")]
    public sealed class BattlefieldInstaller : InstallerAsset
    {
        [SerializeField] private BattlefieldConfig config;
        [SerializeField] private GameObject viewPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException("BattlefieldInstaller: BattlefieldConfig is missing");
            if (viewPrefab == null) throw new InvalidOperationException("BattlefieldInstaller: view prefab is missing");
            if (!registry.TryResolve<IRunFlow>(out var run))
                throw new InvalidOperationException("BattlefieldInstaller: IRunFlow is missing, install Run before Battlefield");
            if (!registry.TryResolve<ISessionScene>(out var session))
                throw new InvalidOperationException("BattlefieldInstaller: ISessionScene is missing, install SessionScene first");

            var layout = new BackdropLayout(config);
            var problems = layout.Problems();
            if (problems.Count > 0)
                throw new InvalidOperationException("BattlefieldInstaller: BattlefieldConfig breaks the design: " +
                                                    string.Join("; ", problems));

            session.Join(new BattlefieldParticipant(run, layout, new PrefabBackdropViewFactory(viewPrefab)));
        }
    }
}
