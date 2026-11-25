using Game.Ads;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using UnityEngine;

namespace Game.PopupRevive
{
    /// <summary>
    /// Wires the revive popup: takes the run, ads and popup service from the registry and joins the session.
    /// Install after SessionScene, Run, Ads and Popup. Publishes no contract.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/PopupRevive/Installer", fileName = "PopupReviveInstaller")]
    public sealed class PopupReviveInstaller : InstallerAsset
    {
        public override void Install(IServiceRegistry registry)
        {
            var session = Require<ISessionScene>(registry);
            var flow = Require<IRunFlow>(registry);
            var revive = Require<IRunRevive>(registry);
            var ads = Require<IRewardedAds>(registry);
            var popups = Require<IPopupService>(registry);
            var views = Require<IPopupViews>(registry);

            session.Join(new PopupReviveParticipant(flow, revive, ads, popups, views));
        }

        private static T Require<T>(IServiceRegistry registry) where T : class
        {
            if (registry.TryResolve(out T service)) return service;
            throw new System.InvalidOperationException(
                $"PopupReviveInstaller: {typeof(T).Name} is not installed; install its feature before PopupRevive.");
        }
    }
}
