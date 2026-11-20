using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    [CreateAssetMenu(menuName = "Game/Installers/Popup")]
    public sealed class PopupInstaller : InstallerAsset
    {
        public override void Install(IServiceRegistry registry)
        {
            registry.Register<IPopupService>(new PopupService(new UnityPopupPresenter()));
        }
    }
}
