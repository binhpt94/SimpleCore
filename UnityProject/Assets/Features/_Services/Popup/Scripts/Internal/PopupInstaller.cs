using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Needs the scene's asset catalog of kind "Popup" (registered by Boot) and the session (SessionScene must be
    /// installed earlier). Publishes <see cref="IPopupService"/> and <see cref="IPopupViews"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Installers/Popup")]
    public sealed class PopupInstaller : InstallerAsset
    {
        [SerializeField] private PopupConfig config;
        [Tooltip("Dev builds only (feature-popup/cheat). Release builds ignore it.")]
        [SerializeField] private PopupCheatState cheat = PopupCheatState.None;

        public override void Install(IServiceRegistry registry)
        {
            if (config == null) throw new InvalidOperationException($"Popup installer '{name}': config is missing.");
            config.Check();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PopupCheat.Check(cheat, config);
#endif
            var catalog = registry.Resolve<IAssetCatalogs>().Get(CatalogPopupPrefabs.Kind);
            var session = registry.Resolve<ISessionScene>();

            IPopupPrefabs prefabs = new CatalogPopupPrefabs(catalog);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            prefabs = PopupCheat.Prefabs(cheat, config, prefabs);
#endif
            var presenter = new UnityPopupPresenter(config.LayerPrefab, config.BackdropPrefab);
            var service = new PopupService(presenter, prefabs, new UnityPopupLog(), config.MaxOpen);

            registry.Register<IPopupService>(service);
            registry.Register<IPopupViews>(service);
            registry.Register<IPopupStack>(service);
            session.Join(new PopupParticipant(service, prefabs));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PopupCheat.Join(cheat, session, service);   // after the service's part: its Run opens the stack first
#endif
        }
    }
}
