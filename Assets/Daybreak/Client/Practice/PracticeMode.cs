using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// The M3 offline loop, in one component: build a squad (pick 5, place on the 2×3 grid, choose
    /// a tactic), then Practice against a preset opponent — which runs the deterministic sim
    /// locally and plays the result through the M2 ReplayPlayer. Back returns to the builder.
    ///
    /// UI is IMGUI on purpose: no Canvas, no prefabs, no input-system wiring — add this one
    /// component to an empty scene and press Play.
    /// </summary>
    [RequireComponent(typeof(ReplayPlayer))]
    public sealed class PracticeMode : MonoBehaviour
    {
        private enum Phase { Building, Watching }

        private readonly SquadDraft _draft = new SquadDraft();
        private UnitCatalog _defs;
        private ReplayPlayer _player;

        private Phase _phase = Phase.Building;
        private int _selectedSlot = -1;
        private int _opponentIndex = 0;
        private string _message = "Pick five units and place them on the grid.";
        private Vector2 _rosterScroll;

        private void Start()
        {
            ConfigureCamera();
            _player = GetComponent<ReplayPlayer>();
            try { _defs = ConfigService.Units; }
            catch (Exception e) { Debug.LogError("[Daybreak] Could not load units: " + e.Message); }
        }

        private void OnGUI()
        {
            if (_defs == null)
            {
                GUI.Label(new Rect(20, 20, 600, 40), "Could not load the unit roster. See the Console.");
                return;
            }

            if (_phase == Phase.Building) DrawBuilder();
            else DrawWatchingOverlay();
        }

        // ------------------------------------------------------------------ builder

        private void DrawBuilder()
        {
            GUI.Label(new Rect(12, 8, 800, 24), "DAYBREAK — Squad Builder (local practice)");

            DrawRoster(new Rect(12, 36, 320, Screen.height - 48));
            DrawGridAndTactic(new Rect(344, 36, 360, Screen.height - 48));
            DrawInfoAndActions(new Rect(716, 36, Mathf.Max(260, Screen.width - 728), Screen.height - 48));
        }

        private void DrawRoster(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("ROSTER  (click to add / remove)");
            _rosterScroll = GUILayout.BeginScrollView(_rosterScroll);

            foreach (var id in _defs.Ids)
            {
                var d = _defs.Get(id);
                bool placed = _draft.Contains(id);

                var prev = GUI.backgroundColor;
                GUI.backgroundColor = placed ? new Color(0.4f, 0.7f, 0.4f) : prev;
                string label = (placed ? "* " : "") + d.Name + "\n"
                    + d.Arch + " / " + d.Tag + "   HP" + d.HP + " ATK" + d.Atk + " DEF" + d.Def + " SPD" + d.Spd;
                if (GUILayout.Button(label, GUILayout.Height(40)))
                    OnRosterClick(id, placed);
                GUI.backgroundColor = prev;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnRosterClick(string id, bool placed)
        {
            if (placed) { _draft.ClearUnit(id); _message = "Removed " + _defs.Get(id).Name + "."; return; }

            int slot = _selectedSlot >= 0 && _draft.GetSlot(_selectedSlot) == null
                ? _selectedSlot
                : FirstEmptySlot();

            if (slot < 0) { _message = "Grid full — remove a unit first."; return; }
            if (_draft.Place(slot, id, out var err)) { _message = "Placed " + _defs.Get(id).Name + "."; _selectedSlot = -1; }
            else _message = err;
        }

        private int FirstEmptySlot()
        {
            for (int i = 0; i < SquadDraft.SlotCount; i++)
                if (_draft.GetSlot(i) == null) return i;
            return -1;
        }

        private void DrawGridAndTactic(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("SQUAD  (click a cell, then a unit; click a filled cell to clear)");

            DrawGridRow("FRONT — takes the hits", 0);
            DrawGridRow("BACK — protected, deals damage", 1);

            GUILayout.Space(8);
            GUILayout.Label("TACTIC");
            foreach (Tactic t in Enum.GetValues(typeof(Tactic)))
            {
                var prev = GUI.backgroundColor;
                if (_draft.Tactic == t) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                if (GUILayout.Button(TacticLabel(t))) _draft.Tactic = t;
                GUI.backgroundColor = prev;
            }

            GUILayout.EndArea();
        }

        private void DrawGridRow(string caption, int row)
        {
            GUILayout.Space(6);
            GUILayout.Label(caption);
            GUILayout.BeginHorizontal();
            for (int col = 0; col < SquadDraft.Cols; col++)
            {
                int slot = SquadDraft.SlotIndex(row, col);
                string id = _draft.GetSlot(slot);
                string label = id == null ? "[ empty ]" : _defs.Get(id).Name;

                var prev = GUI.backgroundColor;
                if (slot == _selectedSlot) GUI.backgroundColor = new Color(0.9f, 0.8f, 0.3f);
                else if (id != null) GUI.backgroundColor = new Color(0.35f, 0.55f, 0.75f);

                if (GUILayout.Button(label, GUILayout.Height(46)))
                    OnSlotClick(slot, id);
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();
        }

        private void OnSlotClick(int slot, string id)
        {
            if (id != null) { _draft.Clear(slot); _selectedSlot = slot; _message = "Cleared a slot."; }
            else { _selectedSlot = slot; _message = "Selected a slot — now click a unit."; }
        }

        private void DrawInfoAndActions(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("SYNERGIES");
            DrawSynergyPreview();

            GUILayout.Space(8);
            GUILayout.Label("Units placed: " + _draft.FilledCount + " / " + SquadDraft.MaxUnits);

            GUILayout.Space(8);
            GUILayout.Label("OPPONENT");
            GUILayout.BeginHorizontal();
            GUILayout.Label(DummyOpponents.NameOf(_opponentIndex));
            if (GUILayout.Button("Change", GUILayout.Width(80)))
                _opponentIndex = (_opponentIndex + 1) % DummyOpponents.Count;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUI.enabled = _draft.IsComplete;
            if (GUILayout.Button("PRACTICE BATTLE", GUILayout.Height(40))) StartPractice();
            GUI.enabled = true;

            if (GUILayout.Button("Clear all")) { _draft.ClearAll(); _selectedSlot = -1; _message = "Cleared."; }

            GUILayout.Space(10);
            GUILayout.Label(_message);
            GUILayout.EndArea();
        }

        private void DrawSynergyPreview()
        {
            var counts = new int[4];
            for (int slot = 0; slot < SquadDraft.SlotCount; slot++)
            {
                var id = _draft.GetSlot(slot);
                if (id != null) counts[(int)_defs.Get(id).Tag]++;
            }

            bool any = false;
            foreach (Tag tag in Enum.GetValues(typeof(Tag)))
            {
                int c = counts[(int)tag];
                if (c < 2) continue;
                any = true;
                GUILayout.Label("  " + tag + " x" + c + "  ->  " + SynergyText(tag, c));
            }
            if (!any) GUILayout.Label("  (none yet — 2+ of a tag activates one)");
        }

        private static string SynergyText(Tag tag, int copies)
        {
            bool three = copies >= 3;
            switch (tag)
            {
                case Tag.Guardian: return three ? "+30% team DEF" : "+15% team DEF";
                case Tag.Pack: return three ? "+25% team ATK" : "+12% team ATK";
                case Tag.Arcane: return three ? "+9 true damage" : "+4 true damage";
                case Tag.Swift: return three ? "+30% team SPD" : "+15% team SPD";
                default: return "";
            }
        }

        private static string TacticLabel(Tactic t)
        {
            switch (t)
            {
                case Tactic.Balanced: return "Balanced — same column, else nearest";
                case Tactic.FocusFire: return "Focus Fire — pile on the weakest";
                case Tactic.ProtectBackline: return "Protect Backline — clear their front";
                case Tactic.Berserk: return "Berserk — +25% ATK / -25% DEF, hit hardest";
                default: return t.ToString();
            }
        }

        // ------------------------------------------------------------------ practice / watch

        private void StartPractice()
        {
            if (!_draft.TryBuild("you", out var mine, out var err)) { _message = err; return; }

            var opponent = DummyOpponents.At(_opponentIndex);
            int seed = unchecked(Environment.TickCount ^ (_opponentIndex * 92821));
            var result = BattleSimulator.Simulate(mine, opponent, WeeklyModifier.None, seed, _defs);

            _phase = Phase.Watching;
            _player.Play(mine, opponent, WeeklyModifier.None, result, _defs);
        }

        private void DrawWatchingOverlay()
        {
            if (GUI.Button(new Rect(Screen.width - 170, 10, 160, 30), "< Back to builder"))
            {
                _player.Clear();
                _phase = Phase.Building;
            }
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
