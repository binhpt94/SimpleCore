using System;
using System.Collections.Generic;
using Game.SceneFlow;

namespace Game.CheatPanel
{
    /// <summary>
    /// Rules of the cheat button and panel (nodes access-spec and states-rules): the button opens and closes the
    /// panel; choosing a row closes it and applies it (a state row picks the state and reloads its scene, also when it
    /// is the running one; the normal row drops the pick and restarts the game from its first scene); while a scene
    /// switch runs every row is locked. A scene change ends the session, which destroys the overlay, so the next scene
    /// starts with the panel closed. Plain C#.
    /// </summary>
    internal sealed class CheatPanelPresenter : ICheatPanelInput, IDisposable
    {
        private readonly CheatPanelConfig _config;
        private readonly ICheatStates _states;
        private readonly ISceneSwitch _switch;
        private readonly IReadOnlyList<CheatRow> _rows;
        private ICheatPanelDisplay _display;

        public CheatPanelPresenter(CheatPanelConfig config, ICheatStates states, ISceneSwitch sceneSwitch, bool openAtStart = false)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _states = states ?? throw new ArgumentNullException(nameof(states));
            _switch = sceneSwitch ?? throw new ArgumentNullException(nameof(sceneSwitch));
            _rows = CheatRows.Build(config.NormalRowLabel, states.Entries);
            IsOpen = openAtStart;
            _switch.SwitchingChanged += Refresh;
        }

        public bool IsOpen { get; private set; }

        public IReadOnlyList<CheatRow> Rows => _rows;

        public bool RowsLocked => _switch.IsSwitching;

        /// <summary>The running state's row; the normal row when no state runs (or the running one has no row).</summary>
        public int ActiveRow
        {
            get
            {
                var running = _states.RunningKey;
                if (running == null) return 0;
                for (var i = 1; i < _rows.Count; i++)
                    if (_rows[i].StateKey == running) return i;
                return 0;
            }
        }

        public CheatPanelScreen Screen => new CheatPanelScreen(_config.ButtonLabel, IsOpen, _config.PanelTitle, _rows,
            ActiveRow, RowsLocked, _rows.Count == 1 ? _config.EmptyLabel : null);

        public void Attach(ICheatPanelDisplay display)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _display.Bind(this);
            Refresh();
        }

        public void PressButton()
        {
            IsOpen = !IsOpen;
            Refresh();
        }

        public void PressRow(int index)
        {
            if (!IsOpen || RowsLocked || index < 0 || index >= _rows.Count) return;
            var row = _rows[index];
            IsOpen = false;
            Refresh();
            _states.Pick(row.StateKey);
            if (row.IsNormal) _switch.Restart();
            else _switch.Reload(row.Scene);
        }

        public void Dispose()
        {
            _switch.SwitchingChanged -= Refresh;
            _display = null;
        }

        private void Refresh()
        {
            _display?.Show(Screen);
        }
    }
}
