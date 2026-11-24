using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Run
{
    /// <summary>
    /// The run inside the Game scene session: reads the saved best before play, begins the run on the frame after
    /// Run (participants installed after the run get their Run call later in the same frame, so they already listen
    /// to RunStarted by then), and drops pending waits at the end.
    /// </summary>
    internal sealed class RunSession : SessionParticipantBase
    {
        private readonly RunFlow _flow;
        private readonly RunScore _score;
        private readonly IRunDelay _delay;

        public RunSession(RunFlow flow, RunScore score, IRunDelay delay)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellation)
        {
            _score.LoadBest();
            return UniTask.CompletedTask;
        }

        protected override void OnRun()
        {
            var begin = _delay.NextFrame(_flow.Begin);
            WhenDisposed(begin.Dispose);
            WhenDisposed(_flow.Shutdown);
        }
    }
}
