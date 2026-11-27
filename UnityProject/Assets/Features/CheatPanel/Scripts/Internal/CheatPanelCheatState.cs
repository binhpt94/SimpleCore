namespace Game.CheatPanel
{
    /// <summary>States of node feature-cheat-panel/cheat, chosen on the installer asset. Built in dev builds only.</summary>
    internal enum CheatPanelCheatState
    {
        None = 0,

        /// <summary>panel-open: the panel is open as soon as the scene runs, with every row the build has.</summary>
        PanelOpen = 1,
    }
}
