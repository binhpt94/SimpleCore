using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Run;

namespace Game.Hud
{
    /// <summary>
    /// One button per castle skill (feature-hud/hud-skill-button/skill-button-spec), each in its fixed slot
    /// (<see cref="HudSkillSlots"/>); slots of skills the battle lacks stay empty. Every frame it shows the state the
    /// castle reports; a tap on a ready button during play asks the castle once. The castle keeps every cooldown.
    /// </summary>
    internal sealed class HudSkillPresenter
    {
        private readonly IHudView _view;
        private readonly ICastleSkills _skills;
        private readonly IRunFlow _run;
        private readonly HudTimings _timings;
        private readonly List<CastleSkill> _order = new List<CastleSkill>();
        private readonly List<bool> _wasReady = new List<bool>();
        private readonly List<float> _bounceTime = new List<float>();
        private bool _started;

        public HudSkillPresenter(IHudView view, ICastleSkills skills, IRunFlow run, HudTimings timings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _timings = timings;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            var names = new List<string>();
            var slots = new List<int>();
            foreach (var skill in _skills.Skills)
            {
                var slot = HudSkillSlots.SlotOf(skill);
                if (slot < 0 || slots.Contains(slot)) continue;
                _order.Add(skill);
                _wasReady.Add(_skills.GetState(skill).Ready);
                _bounceTime.Add(-1f);
                names.Add(HudText.SkillName(skill));
                slots.Add(slot);
            }

            _view.CreateSkillButtons(names, slots);
            _view.SkillPressed += OnPressed;
            Refresh();
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _view.SkillPressed -= OnPressed;
        }

        /// <summary>Shows the castle's current state; and plays the ready bounce.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!_started) return;
            if (deltaSeconds > 0f)
            {
                for (var i = 0; i < _bounceTime.Count; i++)
                {
                    if (_bounceTime[i] < 0f) continue;
                    _bounceTime[i] += deltaSeconds;
                    if (_bounceTime[i] >= _timings.BounceSeconds) _bounceTime[i] = -1f;
                }
            }

            Refresh();
        }

        private void OnPressed(int index)
        {
            if (!_started || index < 0 || index >= _order.Count) return;
            if (_run.Step != RunStep.Playing) return;

            var skill = _order[index];
            if (!_skills.GetState(skill).Ready) return;
            _skills.TryUse(skill);
            Show(index);
        }

        private void Refresh()
        {
            for (var i = 0; i < _order.Count; i++) Show(i);
        }

        private void Show(int index)
        {
            var state = _skills.GetState(_order[index]);
            if (state.Ready && !_wasReady[index] && _timings.BounceSeconds > 0f && _timings.BounceScale > 1f)
                _bounceTime[index] = 0f;
            _wasReady[index] = state.Ready;

            _view.ShowSkill(index, state.Ready, HudText.SkillSeconds(state), HudText.SkillCover(state), BounceScale(_bounceTime[index]));
        }

        /// <summary>1 → peak → 1 over the bounce time, linear both ways.</summary>
        private float BounceScale(float time)
        {
            if (time < 0f || _timings.BounceSeconds <= 0f) return 1f;
            var progress = Math.Min(1f, time / _timings.BounceSeconds);
            var toPeak = 1f - Math.Abs(2f * progress - 1f);
            return 1f + (_timings.BounceScale - 1f) * toPeak;
        }
    }
}
