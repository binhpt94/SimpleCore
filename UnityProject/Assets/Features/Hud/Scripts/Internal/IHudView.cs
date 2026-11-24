using System;
using System.Collections.Generic;

namespace Game.Hud
{
    /// <summary>The battle HUD on screen. Shows what it is given and reports taps; decides nothing.</summary>
    internal interface IHudView
    {
        void SetVisible(bool visible);

        void ShowHealth(string text, float fill);

        void ShowCoins(string text);

        void ShowWave(string text);

        void ShowBanner(string text);

        void HideBanner();

        /// <summary>One button per name, left to right in this order.</summary>
        void CreateSkillButtons(IReadOnlyList<string> names);

        /// <param name="index">Button index from <see cref="CreateSkillButtons"/>.</param>
        /// <param name="ready">Bright and pressable when true, dimmed otherwise.</param>
        /// <param name="seconds">Seconds label; empty hides it.</param>
        /// <param name="cover">Share of the button covered, 0..1.</param>
        /// <param name="scale">Button size, 1 = normal.</param>
        void ShowSkill(int index, bool ready, string seconds, float cover, float scale);

        /// <summary>The player tapped a skill button, with its index.</summary>
        event Action<int> SkillPressed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev only: a button that applies a cheat state.</summary>
        void ShowCheatButton(string label);

        event Action CheatPressed;
#endif

        void Destroy();
    }

    /// <summary>Puts a new HUD view on screen.</summary>
    internal interface IHudViewSpawner
    {
        IHudView Spawn();
    }
}
