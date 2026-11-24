using Game.Core;
using Game.Run;

namespace Game.Wallet
{
    /// <summary>Puts the balance back to the start coins whenever the run says a new run begins.</summary>
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
            WhenDisposed(() => run.RunStarted -= OnRunStarted);
        }

        private void OnRunStarted(RunTeam team)
        {
            logic.ResetToStart();
        }
    }
}
