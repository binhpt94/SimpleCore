using System;
using Game.Core;
using Game.Run;

namespace Game.ButtonTest
{
    /// <summary>
    /// ButtonTest trong phiên của scene Game: tạo view nút thử khi chạy, nối presenter, huỷ khi phiên kết thúc.
    /// </summary>
    internal sealed class ButtonTestParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _run;
        private readonly ITestButtonViewFactory _views;
        private readonly ButtonTestCheat _cheat;

        internal ButtonTestParticipant(IRunFlow run, ITestButtonViewFactory views, ButtonTestCheat cheat = ButtonTestCheat.None)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _cheat = cheat;
        }

        internal TestButtonPresenter Presenter { get; private set; }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var view = _views.Create();
            var tracked = TrackView(view.Root);
            WhenDisposed(() =>
            {
                tracked.Dispose();
                _views.Destroy(view);
            });

            Presenter = new TestButtonPresenter(_run, view);
            Presenter.Start();
            WhenDisposed(Presenter.Stop);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_cheat == ButtonTestCheat.Pressed12)
            {
                Presenter.ShowCheatState(ButtonTestCheatValues.Pressed12Count);
            }
#endif
        }
    }
}
