using System.Collections;
using System.Collections.Generic;
using Daybreak.Sim;
using UnityEngine;
// Both namespaces define EventType (UnityEngine's is for IMGUI); we mean the sim's.
using EventType = Daybreak.Sim.EventType;

namespace Daybreak.Client
{
    /// <summary>
    /// Consumes a BattleEvent[] and animates it: units on two 2x3 grids, lunges, HP bars, deaths.
    /// It owns NO game logic — it only replays what the sim already decided. Give it a result and
    /// it plays; the same call is what M3 (local practice) and M4 (server results) will use.
    /// </summary>
    public sealed class ReplayPlayer : MonoBehaviour
    {
        [Header("Timing (seconds, before speed multiplier)")]
        [SerializeField] private float _lungeTime = 0.16f;
        [SerializeField] private float _hitTime = 0.14f;
        [SerializeField] private float _betweenActions = 0.06f;
        [SerializeField] private float _roundPause = 0.5f;

        private readonly UnitView[] _views = new UnitView[10];
        private Transform _root;

        private Squad _a, _b;
        private WeeklyModifier _mod;
        private BattleResult _result;
        private UnitCatalog _defs;

        private Coroutine _playback;
        private float _speed = 1f;
        private string _status = "";
        private int _round;
        private bool _finished;
        private bool _active;   // false until Play(); Clear() hides the renderer and its controls

        /// <summary>Render a resolved battle. Rebuilds the scene and starts playback.</summary>
        public void Play(Squad a, Squad b, WeeklyModifier mod, BattleResult result, UnitCatalog defs)
        {
            _a = a; _b = b; _mod = mod; _result = result; _defs = defs;
            _active = true;
            Restart();
        }

        /// <summary>Tear down the replay and hide its controls (used when returning to the builder).</summary>
        public void Clear()
        {
            _active = false;
            if (_playback != null) { StopCoroutine(_playback); _playback = null; }
            if (_root != null) { Destroy(_root.gameObject); _root = null; }
            for (int i = 0; i < _views.Length; i++) _views[i] = null;
        }

        public void Restart()
        {
            if (_playback != null) StopCoroutine(_playback);
            BuildScene();
            _playback = StartCoroutine(RunPlayback());
        }

        private void BuildScene()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("ReplayRoot").transform;

            BuildSide(_a, 0);
            BuildSide(_b, 1);

            _round = 0;
            _finished = false;
            _status = "";
        }

        private void BuildSide(Squad squad, int side)
        {
            for (int i = 0; i < squad.Units.Length; i++)
            {
                var p = squad.Units[i];
                var def = _defs.Get(p.UnitId);
                int index = side * 5 + i;

                // Effective max HP isn't exposed by the log, so show base HP for the bar baseline.
                // Damage numbers still read correctly; the bar is a relative indicator.
                int maxHp = def.HP;
                _views[index] = UnitView.Create(_root, def, GridPos(side, p.Row, p.Col), side == 0, maxHp);
            }
        }

        /// <summary>Two facing grids. Side A on the left, its front row inward; B mirrored.</summary>
        private static Vector3 GridPos(int side, int row, int col)
        {
            float x = side == 0
                ? (row == 0 ? -3.7f : -7.7f)   // A: front row nearer centre
                : (row == 0 ? 3.7f : 7.7f);    // B: front row nearer centre
            float y = (1 - col) * 2.3f;         // col 0 top, col 2 bottom
            return new Vector3(x, y, 0f);
        }

        private IEnumerator RunPlayback()
        {
            var log = _result.Log;
            for (int i = 0; i < log.Length; i++)
            {
                var e = log[i];
                switch (e.Type)
                {
                    case EventType.BattleStart:
                        _status = "Battle start";
                        yield return Wait(_roundPause);
                        break;

                    case EventType.SynergyApplied:
                        _status = "Side " + (e.Source == 0 ? "A" : "B") + " synergy: " + (Tag)e.Aux;
                        break;

                    case EventType.ModifierApplied:
                        _status = "Modifier: " + _mod.Id;
                        break;

                    case EventType.RoundStart:
                        _round = e.Value;
                        _status = "Round " + _round;
                        yield return Wait(_roundPause);
                        break;

                    case EventType.Attack:
                    {
                        var attacker = ViewAt(e.Source);
                        if (attacker != null && !attacker.Dead)
                            yield return attacker.Lunge(_lungeTime / _speed);
                        break;
                    }

                    case EventType.Damage:
                    {
                        var target = ViewAt(e.Target);
                        if (target != null)
                            yield return target.TakeHit(e.Value, ReplayArt.MultiplierColor(e.Aux), _hitTime / _speed);
                        yield return Wait(_betweenActions);
                        break;
                    }

                    case EventType.Death:
                        ViewAt(e.Target)?.Die();
                        break;

                    case EventType.BattleEnd:
                        _finished = true;
                        _status = "Winner: Side " + (e.Value == 0 ? "A" : "B");
                        break;
                }
            }
        }

        private UnitView ViewAt(int index) =>
            index >= 0 && index < _views.Length ? _views[index] : null;

        private WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds / _speed);

        // ---- lightweight on-screen controls (IMGUI: no Canvas, no input-system dependency) ----

        private void OnGUI()
        {
            if (!_active) return;
            const int pad = 10;
            GUI.Label(new Rect(pad, pad, 600, 24),
                "Daybreak replay — " + _status + (_finished ? "  (done)" : ""));

            if (GUI.Button(new Rect(pad, pad + 28, 90, 26), "Replay"))
                Restart();

            float[] speeds = { 1f, 2f, 4f };
            for (int i = 0; i < speeds.Length; i++)
            {
                bool on = Mathf.Approximately(_speed, speeds[i]);
                if (GUI.Button(new Rect(pad + 100 + i * 46, pad + 28, 42, 26), (on ? "•" : "") + speeds[i] + "x"))
                    _speed = speeds[i];
            }
        }
    }
}
