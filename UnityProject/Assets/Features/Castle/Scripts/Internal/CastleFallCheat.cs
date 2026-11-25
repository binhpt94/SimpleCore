#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Game.Castle
{
    /// <summary>
    /// Dev-only: brings the castle down through the same hit path enemies use, so the run is told it is lost exactly
    /// once and the castle collapses as in a real fall (feature-popup-revive/cheat#waiting-ad).
    /// </summary>
    internal sealed class CastleFallCheat : ICastleFallCheat
    {
        private readonly CastleBody _body;
        private readonly Func<bool> _inBattle;

        public CastleFallCheat(CastleBody body, Func<bool> inBattle)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _inBattle = inBattle ?? throw new ArgumentNullException(nameof(inBattle));
        }

        public void Fall()
        {
            if (_body.IsDestroyed || !_inBattle()) return;
            // All the health that is left, as one enemy hit: the usual fall (0 health, run told once, Destroyed).
            _body.Hit(0, Math.Max(_body.Health, float.Epsilon));
        }
    }
}
#endif
