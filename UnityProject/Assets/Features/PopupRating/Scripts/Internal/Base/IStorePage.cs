namespace Game.PopupRating
{
    /// <summary>
    /// Opens this game's page on Google Play (node feature-popup-rating/rating-invite/invite-spec: rate-flow,
    /// store-web-fallback, open-fail-silent). Kept behind an interface so tests use a fake; the real one
    /// (Android intent, Application.OpenURL) is written by the feature session.
    /// Which place to try first, and logging when both fail, is the caller's rule (InviteParams.store_open_order).
    /// </summary>
    internal interface IStorePage
    {
        /// <summary>Package name of the build that is running; the page is found by it, never hard-coded.</summary>
        string PackageName { get; }

        /// <summary>Tries to open the game's page in the Google Play app. False when the app cannot open it.</summary>
        bool TryOpenApp(string packageName);

        /// <summary>Tries to open the game's Google Play web page in a browser. False when no browser opens it.</summary>
        bool TryOpenBrowser(string packageName);
    }
}
