using System;
using Game.Core;
using Game.Run;
using Game.Wallet;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Enemy
{
    /// <summary>
    /// Wires the enemy feature: publishes <see cref="IEnemyField"/> and joins the scene session.
    /// Needs SessionScene, Run and Wallet installed earlier. Foes (heroes, castle) add themselves later
    /// through <see cref="IEnemyField.AddFoes"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemy/Enemy Installer", fileName = "EnemyInstaller")]
    public sealed class EnemyInstaller : InstallerAsset
    {
        [SerializeField] private EnemyConfig _config;

        [Tooltip("Prefab whose root has the component that draws the enemies (IEnemyStage).")]
        [SerializeField] private GameObject _stagePrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null)
                throw new InvalidOperationException($"EnemyInstaller '{name}': EnemyConfig is not assigned.");

            var missingKinds = _config.MissingKinds();
            if (missingKinds.Length > 0)
                throw new InvalidOperationException(
                    $"EnemyInstaller '{name}': EnemyConfig '{_config.name}' has no row for: {missingKinds}.");

            if (_stagePrefab == null)
                throw new InvalidOperationException($"EnemyInstaller '{name}': stage prefab is not assigned.");
            if (_stagePrefab.GetComponent<IEnemyStage>() == null)
                throw new InvalidOperationException(
                    $"EnemyInstaller '{name}': stage prefab '{_stagePrefab.name}' has no IEnemyStage component.");

            var session = registry.Resolve<ISessionScene>();
            var runFlow = registry.Resolve<IRunFlow>();
            var income = registry.Resolve<IWalletIncome>();

            var field = new EnemyField(_config, income);
            registry.Register<IEnemyField>(field);

            var prefab = _stagePrefab;
            session.Join(new EnemyParticipant(field, runFlow,
                () => Object.Instantiate(prefab).GetComponent<IEnemyStage>()));
        }
    }
}
