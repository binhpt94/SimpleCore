using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Needs the scene's asset catalog of kind "Popup" (registered by Boot).</summary>
    [CreateAssetMenu(menuName = "Game/Installers/Popup")]
    public sealed class PopupInstaller : InstallerAsset
    {
        public override void Install(IServiceRegistry registry)
        {
            var catalog = registry.Resolve<IAssetCatalogs>().Get(CatalogPopupPrefabs.Kind);
            registry.Register<IPopupService>(new PopupService(new UnityPopupPresenter(), new CatalogPopupPrefabs(catalog)));
        }
    }
}
