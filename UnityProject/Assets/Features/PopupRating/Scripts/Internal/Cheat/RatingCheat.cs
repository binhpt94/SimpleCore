using System;
using Game.Core;

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
