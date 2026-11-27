using System;
using Game.Core;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
#endif

namespace Game.PopupRating
{
    /// <summary>
    /// States of node feature-popup-rating/cheat, picked on the installer asset. The type stays in every build so
    /// the asset serializes the same; release builds ignore it (the code below is dev only).
    /// </summary>
    internal enum RatingCheatState
    {
        None,

        /// <summary>invite-open: PopupRating/Main open, no button touched; timing rules and the launch limit skipped.</summary>
        InviteOpen,
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Names and keys of this feature's cheat states, for the cheat list (Core's ICheatStateSource).</summary>
    internal static class RatingCheats
    {
        /// <summary>Title of the design node feature-popup-rating.</summary>
        internal const string FeatureTitle = "Popup mời đánh giá";

        internal const string InviteOpenKey = "PopupRating/InviteOpen";

        /// <summary>In the order of the ideas of node feature-popup-rating/cheat, titled as those ideas.</summary>
        internal static readonly IReadOnlyList<CheatState> States = new[]
        {
            new CheatState(InviteOpenKey, "Popup mời đánh giá đang mở, chưa chạm nút nào"),
        };

        /// <summary>The state a picked key stands for: <see cref="RatingCheatState.None"/> when it is not this feature's (or normal play).</summary>
        internal static RatingCheatState StateOf(string pickedKey) =>
            pickedKey == InviteOpenKey ? RatingCheatState.InviteOpen : RatingCheatState.None;

        internal static string KeyOf(RatingCheatState state) => state switch
        {
            RatingCheatState.InviteOpen => InviteOpenKey,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "not a cheat state"),
        };
    }

    /// <summary>Opens the invite as soon as the session runs, without playing until a new best.</summary>
    internal sealed class RatingCheatParticipant : SessionParticipantBase
    {
        private readonly RatingParticipant _invite;

        internal RatingCheatParticipant(RatingParticipant invite)
        {
            _invite = invite ?? throw new ArgumentNullException(nameof(invite));
        }

        protected override void OnRun() => _invite.Open();
    }
#endif
}
