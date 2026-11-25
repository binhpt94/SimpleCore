using Game.Core;
using Game.Run;

namespace Game.Wallet
{
    /// <summary>
    /// Puts the balance back to the start coins whenever the run says a new run begins,
    /// and stops taking income once the run says the battle stopped.
    /// </summary>
    internal sealed class WalletRunReset : SessionParticipantBase
    {
        private readonly WalletLogic logic;
        private readonly IRunFlow run;

        internal WalletRunReset(WalletLogic logic, IRunFlow run)
        {
            this.logic = logic;
            this.run = run;
        }

        protected override void OnRun()
        {
            run.RunStarted += OnRunStarted;
            run.BattleStopped += OnBattleStopped;
            WhenDisposed(() =>
            {
                run.RunStarted -= OnRunStarted;
                run.BattleStopped -= OnBattleStopped;
            });
        }

        private void OnRunStarted(RunTeam team)
        {
            logic.ResetToStart();
        }

        private void OnBattleStopped()
        {
            logic.EndRun();
        }
    }
}
