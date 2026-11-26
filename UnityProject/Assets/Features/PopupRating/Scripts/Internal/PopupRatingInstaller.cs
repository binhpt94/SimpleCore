using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using UnityEngine;

namespace Game.PopupRating
{
    /// <summary>
    /// Wires the rating invite into the Game scene session. Install after Popup and Run.
    /// The popup prefab is listed under id PopupRating/Main in the scene's popup catalog (_Composition).
    /// This asset also holds the "invites shown" count of the app launch: in memory only, never saved.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/PopupRating/Installer", fileName = "PopupRatingInstaller")]
    public sealed class PopupRatingInstaller : InstallerAsset
    {
        [Tooltip("InviteParams and TimingParams (FeatureAssets/Private/PopupRatingConfig).")]
        [SerializeField] private RatingConfig _config;

        [Tooltip("Dev builds only: build this cheat state as soon as the session runs (node feature-popup-rating/cheat).")]
        [SerializeField] private RatingCheatState _cheat = RatingCheatState.None;

        [NonSerialized] private RatingLaunchMemory _launch = new RatingLaunchMemory();

        internal RatingConfig Config { get => _config; set => _config = value; }
        internal RatingCheatState Cheat { get => _cheat; set => _cheat = value; }
        internal RatingLaunchMemory Launch => _launch;

        // A new launch (or entering Play in the Editor) starts with no invite shown.
        private void OnEnable() => _launch = new RatingLaunchMemory();

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null)
                throw new InvalidOperationException("PopupRating: the installer has no RatingConfig (FeatureAssets/Private/PopupRatingConfig).");
            var session = Need<ISessionScene>(registry, "SessionScene");
            var flow = Need<IRunFlow>(registry, "Run");
            var score = Need<IRunScore>(registry, "Run");
            var popups = Need<IPopupService>(registry, "Popup");
            var views = Need<IPopupViews>(registry, "Popup");

            var invite = new RatingParticipant(flow, score, popups, views, new UnityStorePage(),
                new UnityRatingClock(), new UnityRatingLog(), _config, _launch);
            session.Join(invite);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_cheat == RatingCheatState.InviteOpen) session.Join(new RatingCheatParticipant(invite));
#endif
        }

        private static T Need<T>(IServiceRegistry registry, string owner) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"PopupRating: {typeof(T).Name} is missing. Install {owner} before PopupRating.");
        }
    }
}
