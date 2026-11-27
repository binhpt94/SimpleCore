using UnityEngine;

namespace Game.CheatPanel
{
    /// <summary>
    /// Table AccessParams (node feature-cheat-panel/cheat-access/access-spec), IDEA column.
    /// The asset lives in FeatureAssets/Private and ships with the build: the build carries the value set before building.
    /// Only <see cref="CheatEnabled"/> decides whether the cheat button exists, in the Editor and in every kind of build.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/CheatPanel/Config", fileName = "CheatPanelConfig")]
    internal sealed class CheatPanelConfig : ScriptableObject
    {
        [Header("AccessParams")]
        [Tooltip("cheat_enabled: on = cheat button and panel in every scene; off = nothing is created, no touch is blocked. Default on: every build is a test build for now.")]
        [SerializeField] private bool _cheatEnabled = true;
        [Tooltip("button_label: text on the cheat button.")]
        [SerializeField] private string _buttonLabel = "Cheat";
        [Tooltip("panel_title: title of the cheat panel.")]
        [SerializeField] private string _panelTitle = "Cheat";
        [Tooltip("normal_row_label: text of the first row, which drops the cheat state.")]
        [SerializeField] private string _normalRowLabel = "Chơi bình thường";
        [Tooltip("empty_label: shown under the normal row when the build has no cheat state.")]
        [SerializeField] private string _emptyLabel = "Bản này không có state cheat";

        internal bool CheatEnabled { get => _cheatEnabled; set => _cheatEnabled = value; }
        internal string ButtonLabel { get => _buttonLabel; set => _buttonLabel = value; }
        internal string PanelTitle { get => _panelTitle; set => _panelTitle = value; }
        internal string NormalRowLabel { get => _normalRowLabel; set => _normalRowLabel = value; }
        internal string EmptyLabel { get => _emptyLabel; set => _emptyLabel = value; }
    }
}
