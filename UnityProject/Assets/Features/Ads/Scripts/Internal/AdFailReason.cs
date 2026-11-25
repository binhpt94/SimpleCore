namespace Game.Ads
{
    /// <summary>Why a request ended as NotWatched. Goes to the log only; the caller just sees NotWatched.</summary>
    internal enum AdFailReason
    {
        /// <summary>The provider had no ad to show.</summary>
        NoFill,

        /// <summary>The network dropped.</summary>
        NoNetwork,

        /// <summary>The player closed the ad before it counted.</summary>
        ClosedEarly,

        /// <summary>The ad did not open in time, or the reward was not confirmed in time after it closed.</summary>
        TimedOut,

        /// <summary>Another request was already running.</summary>
        Busy,
    }
}
