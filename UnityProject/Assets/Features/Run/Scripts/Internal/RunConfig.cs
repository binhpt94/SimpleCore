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

        public float LoseDelaySeconds => loseDelaySeconds;

        public int TeamSize => teamSize;

        public bool SkipMenus => skipMenus;

        public RunTeam DefaultTeam => new RunTeam(defaultTeam ?? new string[0]);
    }
}
