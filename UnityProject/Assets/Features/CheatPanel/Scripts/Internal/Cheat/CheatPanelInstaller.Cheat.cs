#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.Core;

namespace Game.CheatPanel
{
    /// <summary>Dev builds only: the panel lists the states of Core's cheat catalog and picks through the shared selection.</summary>
    public sealed partial class CheatPanelInstaller
    {
        internal const string PanelOpenKey = "CheatPanel/PanelOpen";

        partial void ReadCheats(IServiceRegistry registry, ref ICheatStates states, ref bool openAtStart)
        {
            // Boot registers both in every scene of a dev build; a registry without them (a test of the access
            // rules alone) has no cheat state to list.
            if (!registry.TryResolve<ICheatSelection>(out var selection)) return;
            if (!registry.TryResolve<ICheatStateCatalog>(out var catalog)) return;
            states = new CatalogCheatStates(catalog, selection);
            openAtStart = !selection.HasPick && _cheat == CheatPanelCheatState.PanelOpen;
            if (openAtStart) selection.ReportRunning(PanelOpenKey);
        }
    }
}
#endif
