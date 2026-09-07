using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Build a 5-unit squad on the 2x3 grid, pick a tactic, and LOCK it for today.</summary>
    public sealed class SquadPanel : AppPanel
    {
        public override string NavLabel => "Squad";

        private UnitCatalog _defs;
        private readonly SquadDraft _draft = new SquadDraft();
        private int _selectedSlot = -1;

        private readonly Button[] _cells = new Button[SquadDraft.SlotCount];
        private readonly Button[] _tacticBtns = new Button[4];
        private TextMeshProUGUI _synergy, _status;
        private Button _lockBtn;
        private RectTransform _roster;

        protected override void Build(Transform content)
        {
            try { _defs = ConfigService.Units; } catch { }

            var top = new GameObject("Top", typeof(RectTransform));
            top.transform.SetParent(content, false);
            var trt = UIBuilder.Rect(top);
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 670);
            UIBuilder.VLayout(top, 8f, 16);

            Caption(top.transform, "SQUAD — front row on top (takes hits), back row below");

            // 2x3 grid.
            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(top.transform, false);
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(330, 118);
            gl.spacing = new Vector2(8, 8);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.MiddleCenter;
            UIBuilder.Sizing(grid, minHeight: 252, preferredHeight: 252);
            for (int i = 0; i < SquadDraft.SlotCount; i++)
            {
                int slot = i;
                _cells[i] = UIBuilder.Button(grid.transform, "[ empty ]", () => OnCell(slot), UITheme.SurfaceAlt, UITheme.SmallSize);
            }

            Caption(top.transform, "TACTIC");
            var tacRow = new GameObject("Tactics", typeof(RectTransform));
            tacRow.transform.SetParent(top.transform, false);
            var th = UIBuilder.HLayout(tacRow, 8f, 0); th.childForceExpandWidth = true; th.childForceExpandHeight = true;
            UIBuilder.Sizing(tacRow, minHeight: 70, preferredHeight: 70);
            var tactics = new[] { Tactic.Balanced, Tactic.FocusFire, Tactic.ProtectBackline, Tactic.Berserk };
            for (int i = 0; i < tactics.Length; i++)
            {
                var t = tactics[i];
                _tacticBtns[i] = UIBuilder.Button(tacRow.transform, ShortTactic(t), () => { _draft.Tactic = t; Refresh(); },
                    UITheme.SurfaceAlt, UITheme.SmallSize);
            }

            _synergy = UIBuilder.Label(top.transform, "", UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(_synergy.gameObject, minHeight: 50, preferredHeight: 50);

            _lockBtn = UIBuilder.Button(top.transform, "LOCK SQUAD", Lock, UITheme.Accent, UITheme.BodySize);
            UIBuilder.Sizing(_lockBtn.gameObject, minHeight: 72, preferredHeight: 72);

            _status = UIBuilder.Label(top.transform, "Pick 5 units.", UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(_status.gameObject, minHeight: 44, preferredHeight: 44);

            // Roster list.
            var listHost = new GameObject("ListHost", typeof(RectTransform));
            listHost.transform.SetParent(content, false);
            var lrt = UIBuilder.Rect(listHost);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(0, -670);
            _roster = UIBuilder.ScrollView(listHost.transform, out _);

            BuildRoster();
            Refresh();
        }

        private void BuildRoster()
        {
            if (_defs == null) return;
            foreach (var id in _defs.Ids)
            {
                var d = _defs.Get(id);
                string label = d.Name + "   " + d.Arch + " / " + d.Tag
                    + "   HP" + d.HP + " ATK" + d.Atk + " DEF" + d.Def + " SPD" + d.Spd;
                var btn = UIBuilder.Button(_roster, label, () => OnRoster(id), UITheme.Surface, UITheme.SmallSize);
                btn.name = "unit_" + id;
                UIBuilder.Sizing(btn.gameObject, minHeight: 72, preferredHeight: 72);
            }
            UIBuilder.Rebuild(_roster);
        }

        public override void OnShow() => Refresh();

        // ---- interaction ----

        private void OnCell(int slot)
        {
            if (_draft.GetSlot(slot) != null) { _draft.Clear(slot); _selectedSlot = slot; }
            else _selectedSlot = slot;
            Refresh();
        }

        private void OnRoster(string id)
        {
            if (_draft.Contains(id)) { _draft.ClearUnit(id); Refresh(); return; }
            int slot = (_selectedSlot >= 0 && _draft.GetSlot(_selectedSlot) == null) ? _selectedSlot : FirstEmpty();
            if (slot < 0) { _status.text = "Grid full — remove a unit first."; return; }
            if (_draft.Place(slot, id, out var err)) _selectedSlot = -1; else _status.text = err;
            Refresh();
        }

        private int FirstEmpty()
        {
            for (int i = 0; i < SquadDraft.SlotCount; i++) if (_draft.GetSlot(i) == null) return i;
            return -1;
        }

        private void Refresh()
        {
            for (int i = 0; i < SquadDraft.SlotCount; i++)
            {
                var id = _draft.GetSlot(i);
                SetButtonLabel(_cells[i], id == null ? "[ empty ]" : _defs.Get(id).Name);
                _cells[i].targetGraphic.color = i == _selectedSlot ? UITheme.Gold
                    : id != null ? UITheme.Accent : UITheme.SurfaceAlt;
            }

            var tactics = new[] { Tactic.Balanced, Tactic.FocusFire, Tactic.ProtectBackline, Tactic.Berserk };
            for (int i = 0; i < _tacticBtns.Length; i++)
                _tacticBtns[i].targetGraphic.color = _draft.Tactic == tactics[i] ? UITheme.Accent : UITheme.SurfaceAlt;

            _synergy.text = SynergyText();
            _lockBtn.interactable = _draft.IsComplete;

            // Roster: tint placed units.
            if (_defs != null)
                foreach (var id in _defs.Ids)
                {
                    var t = _roster.Find("unit_" + id);
                    if (t != null)
                    {
                        var img = t.GetComponent<Image>();
                        if (img != null) img.color = _draft.Contains(id) ? UITheme.AccentDim : UITheme.Surface;
                    }
                }
        }

        private string SynergyText()
        {
            var counts = new int[4];
            for (int i = 0; i < SquadDraft.SlotCount; i++)
            {
                var id = _draft.GetSlot(i);
                if (id != null) counts[(int)_defs.Get(id).Tag]++;
            }
            var sb = new System.Text.StringBuilder("Placed " + _draft.FilledCount + "/5");
            foreach (Tag tag in Enum.GetValues(typeof(Tag)))
                if (counts[(int)tag] >= 2) sb.Append("   " + tag + " x" + counts[(int)tag]);
            return sb.ToString();
        }

        private async void Lock()
        {
            if (!_draft.TryBuild(Shell.PlayerId, out var squad, out var err)) { _status.text = err; return; }
            _status.text = "Locking...";
            try
            {
                int day = GameCalendar.LockTargetDay(DateTime.UtcNow);
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var r = await CloudCodeService.LockSquadAsync(SquadCodec.ToDto(squad, day, now));
                _status.text = r.ok ? "Locked for day " + r.day + "! (+Sparks)" : "Lock failed (backend?).";
                await Shell.RefreshSparks();
            }
            catch (Exception e) { _status.text = "Lock error: " + e.Message; }
        }

        private static void SetButtonLabel(Button b, string text)
        {
            var tmp = b.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private static string ShortTactic(Tactic t)
        {
            switch (t)
            {
                case Tactic.Balanced: return "Balanced";
                case Tactic.FocusFire: return "Focus";
                case Tactic.ProtectBackline: return "Protect";
                case Tactic.Berserk: return "Berserk";
                default: return t.ToString();
            }
        }

        private void Caption(Transform parent, string text)
        {
            var l = UIBuilder.Label(parent, text, UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(l.gameObject, minHeight: 38, preferredHeight: 38);
        }
    }
}
