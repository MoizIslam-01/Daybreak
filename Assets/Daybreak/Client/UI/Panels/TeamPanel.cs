using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Create or join a team; one team per player.</summary>
    public sealed class TeamPanel : AppPanel
    {
        public override string NavLabel => "Team";

        private static readonly string[] Palette =
        { "#E0574C", "#E08A3C", "#E7C13A", "#5FBF63", "#3FA9B0", "#4A90D9", "#8C6FD1", "#D96FB0" };

        private TextMeshProUGUI _header;
        private TMP_InputField _nameInput;
        private Button _colorBtn, _createBtn;
        private RectTransform _list;
        private int _colorIndex = 5;
        private string _myTeamId = "";
        private TeamDto[] _teams = new TeamDto[0];

        protected override void Build(Transform content)
        {
            var top = new GameObject("Top", typeof(RectTransform));
            top.transform.SetParent(content, false);
            var trt = UIBuilder.Rect(top);
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 340);
            UIBuilder.VLayout(top, 10f, 20);

            _header = UIBuilder.Label(top.transform, "", UITheme.SmallSize, UITheme.Text);
            UIBuilder.Sizing(_header.gameObject, minHeight: 56, preferredHeight: 56);

            _nameInput = UIBuilder.InputField(top.transform, "New team name");
            UIBuilder.Sizing(_nameInput.gameObject, minHeight: 66, preferredHeight: 66);

            var createRow = new GameObject("CreateRow", typeof(RectTransform));
            createRow.transform.SetParent(top.transform, false);
            var ch = UIBuilder.HLayout(createRow, 10f, 0);
            ch.childForceExpandHeight = true;
            UIBuilder.Sizing(createRow, minHeight: 66, preferredHeight: 66);
            _colorBtn = UIBuilder.Button(createRow.transform, "Color", CycleColor, Parse(Palette[_colorIndex]), UITheme.SmallSize);
            UIBuilder.Sizing(_colorBtn.gameObject, preferredWidth: 160, minWidth: 160);
            _createBtn = UIBuilder.Button(createRow.transform, "Create team", Create, UITheme.Accent, UITheme.SmallSize);
            UIBuilder.Sizing(_createBtn.gameObject, flexibleWidth: 1);

            var leaveBtn = UIBuilder.Button(top.transform, "Leave team", Leave, UITheme.SurfaceAlt, UITheme.SmallSize);
            UIBuilder.Sizing(leaveBtn.gameObject, minHeight: 56, preferredHeight: 56);

            var listHost = new GameObject("ListHost", typeof(RectTransform));
            listHost.transform.SetParent(content, false);
            var lrt = UIBuilder.Rect(listHost);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(0, -340);
            _list = UIBuilder.ScrollView(listHost.transform, out _);
        }

        public override async void OnShow()
        {
            _header.text = "Loading...";
            await RefreshAsync();
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            try
            {
                var list = await CloudCodeService.ListTeamsAsync();
                _teams = list?.teams ?? new TeamDto[0];
                var profile = await DataService.LoadProfileAsync();
                _myTeamId = profile?.teamId ?? "";
                Refresh();
            }
            catch (Exception e) { _header.text = "Load failed: " + e.Message; }
        }

        private void Refresh()
        {
            _header.text = "Your team: " + (string.IsNullOrEmpty(_myTeamId) ? "(none)" : _myTeamId);

            UIBuilder.Clear(_list);
            if (_teams.Length == 0) { var e = UIBuilder.Label(_list, "No teams yet — create one.", UITheme.SmallSize, UITheme.TextDim); UIBuilder.Sizing(e.gameObject, minHeight: 60, preferredHeight: 60); }
            foreach (var t in _teams)
            {
                var row = UIBuilder.Row(_list);
                int members = t.memberIds?.Length ?? 0;
                var name = UIBuilder.Label(row.transform, t.name + "  (" + members + ")", UITheme.SmallSize, Parse(t.colorHex));
                UIBuilder.Sizing(name.gameObject, flexibleWidth: 1);
                bool mine = t.id == _myTeamId;
                var join = UIBuilder.Button(row.transform, mine ? "Joined" : "Join",
                    () => Join(t.id), mine ? UITheme.SurfaceAlt : UITheme.Accent, UITheme.SmallSize);
                join.interactable = !mine;
                UIBuilder.Sizing(join.gameObject, preferredWidth: 150, minWidth: 150, preferredHeight: 58, minHeight: 58);
            }
            UIBuilder.Rebuild(_list);
        }

        private void CycleColor()
        {
            _colorIndex = (_colorIndex + 1) % Palette.Length;
            _colorBtn.targetGraphic.color = Parse(Palette[_colorIndex]);
        }

        private async void Create()
        {
            var name = _nameInput != null ? _nameInput.text : "";
            if (string.IsNullOrWhiteSpace(name)) { _header.text = "Enter a team name first."; return; }
            try { var r = await CloudCodeService.CreateTeamAsync(name, Palette[_colorIndex]); if (!r.ok) _header.text = "Create failed: " + r.error; else _nameInput.text = ""; await RefreshAsync(); }
            catch (Exception e) { _header.text = "Create error: " + e.Message; }
        }

        private async void Join(string id)
        {
            try { var r = await CloudCodeService.JoinTeamAsync(id); if (!r.ok) _header.text = "Join failed: " + r.error; await RefreshAsync(); }
            catch (Exception e) { _header.text = "Join error: " + e.Message; }
        }

        private async void Leave()
        {
            try { await CloudCodeService.LeaveTeamAsync(); await RefreshAsync(); }
            catch (Exception e) { _header.text = "Leave error: " + e.Message; }
        }

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
