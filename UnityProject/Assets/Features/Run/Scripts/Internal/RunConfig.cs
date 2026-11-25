using UnityEngine;

namespace Game.Run
{
    /// <summary>Tuning values of the run (feature-run/run-flow/flow-rules, feature-run/run-start/start-values).</summary>
    [CreateAssetMenu(menuName = "Game/Run/Run Config", fileName = "RunConfig")]
    internal sealed class RunConfig : ScriptableObject
    {
        [Tooltip("Seconds between the battle stopping (castle destroyed) and the lose popup.")]
        [SerializeField, Min(0f)] private float loseDelaySeconds = 1f;

        [Tooltip("Number of heroes a team must have to enter battle.")]
        [SerializeField, Min(1)] private int teamSize = 3;

        [Tooltip("IDEA stage: no start screen or team select. The game goes straight into battle with the default team, and Play again starts a new battle with it. Off: Home → Team select.")]
        [SerializeField] private bool skipMenus = true;

        [Tooltip("Hero codes of the team used when menus are skipped, slot 1 first.")]
        [SerializeField] private string[] defaultTeam = { "knight", "axeman", "archer" };

        [Tooltip("The battle pauses when the game leaves the screen (pause-rules). IDEA: off, the battle keeps running.")]
        [SerializeField] private bool pauseOnLeavingApp;

        [Tooltip("The device back key pauses a running battle (flow-rules, pause-rules). IDEA: off.")]
        [SerializeField] private bool backKeyPauses;

        [Tooltip("The castle's first fall in a run offers one revive by ad (revive popup after the lose delay). IDEA: off, the first fall loses.")]
        [SerializeField] private bool reviveOnce;

        public bool ReviveOnce => reviveOnce;

        [Tooltip("Prefab that hears the device (leaving the game, back key): FeatureAssets/Private/RunAppSignals.prefab.")]
        [SerializeField] private GameObject appSignalsPrefab;

        public GameObject AppSignalsPrefab => appSignalsPrefab;

        public float LoseDelaySeconds => loseDelaySeconds;

        public bool PauseOnLeavingApp => pauseOnLeavingApp;

        public bool BackKeyPauses => backKeyPauses;

        public int TeamSize => teamSize;

        public bool SkipMenus => skipMenus;

        public RunTeam DefaultTeam => new RunTeam(defaultTeam ?? new string[0]);
    }
}
