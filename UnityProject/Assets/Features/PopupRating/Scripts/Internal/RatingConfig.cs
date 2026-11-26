using UnityEngine;

namespace Game.PopupRating
{
    /// <summary>
    /// Tables InviteParams (node rating-invite/invite-spec) and TimingParams (node rating-timing/timing-spec).
    /// Every row is the same at IDEA, PROTOTYPE and FULL. The asset lives in FeatureAssets/Private.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/PopupRating/Config", fileName = "PopupRatingConfig")]
    internal sealed class RatingConfig : ScriptableObject
    {
        [Header("InviteParams")]
        [Tooltip("invite_text: neutral, no question about liking the game, no stars.")]
        [SerializeField] private string _inviteText = "Đánh giá game trên Google Play";
        [Tooltip("rate_label: the main button.")]
        [SerializeField] private string _rateLabel = "Đánh giá";
        [Tooltip("later_label: the second button.")]
        [SerializeField] private string _laterLabel = "Để sau";
        [Tooltip("store_open_order: the Google Play app, then the browser.")]
        [SerializeField] private StoreOpenOrder _storeOpenOrder = StoreOpenOrder.AppThenBrowser;

        [Header("TimingParams")]
        [Tooltip("invite_delay: real seconds from the battle stop to asking for the invite. Must be above the 1 s lose popup delay of feature-run.")]
        [SerializeField] private float _inviteDelay = 1.5f;
        [Tooltip("max_invites_per_launch: invites shown per app launch.")]
        [SerializeField] private int _maxInvitesPerLaunch = 1;

        internal string InviteText { get => _inviteText; set => _inviteText = value; }
        internal string RateLabel { get => _rateLabel; set => _rateLabel = value; }
        internal string LaterLabel { get => _laterLabel; set => _laterLabel = value; }
        internal StoreOpenOrder StoreOpen { get => _storeOpenOrder; set => _storeOpenOrder = value; }
        internal float InviteDelay { get => _inviteDelay; set => _inviteDelay = value; }
        internal int MaxInvitesPerLaunch { get => _maxInvitesPerLaunch; set => _maxInvitesPerLaunch = value; }
    }
}
