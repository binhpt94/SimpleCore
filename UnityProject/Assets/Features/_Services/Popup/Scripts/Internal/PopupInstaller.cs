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

        public override void Install(IServiceRegistry registry)
        {
            if (config == null) throw new InvalidOperationException($"Popup installer '{name}': config is missing.");
            config.Check();
            var catalog = registry.Resolve<IAssetCatalogs>().Get(CatalogPopupPrefabs.Kind);
            var session = registry.Resolve<ISessionScene>();

            var prefabs = new CatalogPopupPrefabs(catalog);
            var presenter = new UnityPopupPresenter(config.LayerPrefab, config.BackdropPrefab);
            var service = new PopupService(presenter, prefabs, new UnityPopupLog(), config.MaxOpen);

            registry.Register<IPopupService>(service);
            registry.Register<IPopupViews>(service);
            session.Join(new PopupParticipant(service, prefabs));
        }
    }
}
