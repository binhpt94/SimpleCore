using System;
using System.Collections.Generic;

namespace Game.Run
{
    /// <summary>The heroes taken into a run, by hero code, in slot order (slot 1 first).</summary>
    public readonly struct RunTeam
    {
        private readonly string[] _heroCodes;

        /// <summary>Copies <paramref name="heroCodes"/>; later changes to the source do not reach the team.</summary>
        public RunTeam(IReadOnlyList<string> heroCodes)
        {
            if (heroCodes == null) throw new ArgumentNullException(nameof(heroCodes));
            _heroCodes = new string[heroCodes.Count];
            for (var i = 0; i < _heroCodes.Length; i++) _heroCodes[i] = heroCodes[i];
        }

        public IReadOnlyList<string> HeroCodes => _heroCodes ?? Array.Empty<string>();

        public int Count => _heroCodes?.Length ?? 0;
    }
}
