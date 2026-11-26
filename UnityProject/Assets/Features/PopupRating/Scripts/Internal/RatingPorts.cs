using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.PopupRating
{
    /// <summary>What the invite popup prefab shows and reports. Implemented in View, on the popup root.</summary>
    internal interface IRatingView
    {
        /// <summary>The player touched "Đánh giá".</summary>
        event Action RatePressed;

        /// <summary>The player touched "Để sau".</summary>
        event Action LaterPressed;

        /// <summary>The device back key went down while the popup is shown (top or not: the caller checks).</summary>
        event Action BackPressed;

        void ShowText(string inviteText, string rateLabel, string laterLabel);

        /// <summary>After the first touch: both buttons stop taking touches.</summary>
        void LockButtons();
    }

    /// <summary>Where the feature reports problems the player must not see.</summary>
    internal interface IRatingLog
    {
        void Warning(string message);
        void Error(string message);
    }

    internal sealed class UnityRatingLog : IRatingLog
    {
        public void Warning(string message) => Debug.LogWarning($"[PopupRating] {message}");
        public void Error(string message) => Debug.LogError($"[PopupRating] {message}");
    }

    /// <summary>
    /// Real time for the invite delay, and whether the app is in front. Not the battle's SimClock: the battle
    /// stands still while the lose popup is up. Tests use a fake.
    /// </summary>
    internal interface IRatingClock
    {
        /// <summary>
        /// Calls <paramref name="tick"/> every frame with the real seconds since the last frame, and
        /// <paramref name="appActive"/> when the app goes to the background (false) or comes back (true),
        /// until the returned handle is disposed.
        /// </summary>
        IDisposable Start(Action<float> tick, Action<bool> appActive);
    }

    /// <summary>Runs on Unity's player loop (no scene object) and listens to the app losing or regaining focus.</summary>
    internal sealed class UnityRatingClock : IRatingClock
    {
        public IDisposable Start(Action<float> tick, Action<bool> appActive)
        {
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            if (appActive == null) throw new ArgumentNullException(nameof(appActive));
            var stop = new Stop(appActive);
            Application.focusChanged += appActive;
            Loop(tick, stop.Token).Forget();
            return stop;
        }

        private static async UniTaskVoid Loop(Action<float> tick, CancellationToken token)
        {
            while (true)
            {
                var cancelled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (cancelled) return;
                tick(Time.unscaledDeltaTime);
            }
        }

        private sealed class Stop : IDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private Action<bool> _appActive;

            public Stop(Action<bool> appActive) => _appActive = appActive;

            public CancellationToken Token => _cts.Token;

            public void Dispose()
            {
                if (_appActive == null) return;
                Application.focusChanged -= _appActive;
                _appActive = null;
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }

    /// <summary>
    /// The game's Google Play page: the Play app by "market://" intent on Android, the web page through
    /// Application.OpenURL. In the Editor and on other platforms the app is never tried.
    /// </summary>
    internal sealed class UnityStorePage : IStorePage
    {
        public string PackageName => Application.identifier;

        public bool TryOpenApp(string packageName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "market://details?id=" + packageName);
                using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri);
                intent.Call<AndroidJavaObject>("setPackage", "com.android.vending");
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("startActivity", intent);
                return true;
            }
            catch (Exception)
            {
                return false;   // no Play app, or it refused the page
            }
#else
            return false;
#endif
        }

        public bool TryOpenBrowser(string packageName)
        {
            try
            {
                Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
