using System;
using Game.Run;

namespace Game.ButtonTest
{
    /// <summary>
    /// Nối số đếm với view và với màn trận. Nút và nhãn hiện khi màn trận hiện, ẩn khi rời màn trận (N bỏ đi).
    /// Một lượt chơi mới (vào trận, thua rồi chơi lại) đưa N về 0; hồi sinh thành vẫn là màn trận đó nên N giữ nguyên.
    /// Chỉ đọc bước của lượt chơi để biết màn trận có hiện không, không gửi gì cho feature khác.
    /// </summary>
    internal sealed class TestButtonPresenter
    {
        private readonly IRunFlow _run;
        private readonly ITestButtonView _view;
        private readonly TapCounter _counter = new TapCounter();
        private bool _started;

        internal TestButtonPresenter(IRunFlow run, ITestButtonView view)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        internal long Count => _counter.Count;

        internal void Start()
        {
            if (_started) return;
            _started = true;
            _counter.Changed += ShowCount;
            _view.Released += OnReleased;
            _run.StepChanged += OnStepChanged;
            _run.RunStarted += OnRunStarted;
            ShowCount(_counter.Count);
            _view.SetVisible(IsBattleScreen(_run.Step));
        }

        internal void Stop()
        {
            if (!_started) return;
            _started = false;
            _counter.Changed -= ShowCount;
            _view.Released -= OnReleased;
            _run.StepChanged -= OnStepChanged;
            _run.RunStarted -= OnRunStarted;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Cheat (chỉ bản dev): hiện nút và nhãn với N ép sẵn, không cần trận chạy. Bấm tiếp vẫn tăng N như thường;
        /// bước kế tiếp của lượt chơi lại quyết hiện hay ẩn như bình thường.
        /// </summary>
        internal void ShowCheatState(long count)
        {
            _counter.ForceCount(count);
            _view.SetVisible(true);
        }
#endif

        /// <summary>Các bước mà màn trận đang hiện (kể cả lúc popup tạm dừng, hồi sinh, thua che lên).</summary>
        internal static bool IsBattleScreen(RunStep step) =>
            step != RunStep.Home && step != RunStep.TeamSelect;

        private void OnReleased(bool releasedOverButton) => _counter.Release(releasedOverButton);

        private void OnRunStarted(RunTeam team) => _counter.Reset();

        private void OnStepChanged(RunStep step)
        {
            bool onBattle = IsBattleScreen(step);
            if (!onBattle && _counter.Count != 0)
            {
                _counter.Reset();
            }

            _view.SetVisible(onBattle);
        }

        private void ShowCount(long count) => _view.SetCounterText(CounterText.Format(count));
    }
}
