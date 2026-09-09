using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Weekly standings — Individual and Teams tabs — with the last Dawn Crown up top.</summary>
    public sealed class BoardPanel : AppPanel
    {
        public override string NavLabel => "Board";

        private enum Tab { Individual, Teams }
        private Tab _tab = Tab.Individual;

        private TextMeshProUGUI _header;
        private Button _indBtn, _teamBtn;
        private RectTransform _list;
        private StandingsDto _standings;
        private ChampionDto _champ;

        protected override void Build(Transform content)
        {
            var top = new GameObject("Top", typeof(RectTransform));
            top.transform.SetParent(content, false);
            var trt = UIBuilder.Rect(top);
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 230);
            UIBuilder.VLayout(top, 10f, 20);

            _header = UIBuilder.Label(top.transform, "", UITheme.SmallSize, UITheme.Gold);
            UIBuilder.Sizing(_header.gameObject, minHeight: 60, preferredHeight: 60);

            var tabs = new GameObject("Tabs", typeof(RectTransform));
            tabs.transform.SetParent(top.transform, false);
            var th = UIBuilder.HLayout(tabs, 10f, 0);
            th.childForceExpandWidth = true; th.childForceExpandHeight = true;
            UIBuilder.Sizing(tabs, minHeight: 70, preferredHeight: 70);
            _indBtn = UIBuilder.Button(tabs.transform, "Individual", () => Switch(Tab.Individual));
            _teamBtn = UIBuilder.Button(tabs.transform, "Teams", () => Switch(Tab.Teams));

            var listHost = new GameObject("ListHost", typeof(RectTransform));
            listHost.transform.SetParent(content, false);
            var lrt = UIBuilder.Rect(listHost);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(0, -230);
            _list = UIBuilder.ScrollView(listHost.transform, out _);
        }

        public override async void OnShow()
        {
            _header.text = "Loading standings...";
            try
            {
                _standings = await CloudCodeService.GetStandingsAsync();
                _champ = MostRecent(await CloudCodeService.GetChampionsAsync());
                Refresh();
            }
            catch (Exception e) { _header.text = "Load failed: " + e.Message; }
        }

        private void Switch(Tab tab) { _tab = tab; Refresh(); }

        private void Refresh()
        {
            _indBtn.targetGraphic.color = _tab == Tab.Individual ? UITheme.Accent : UITheme.SurfaceAlt;
            _teamBtn.targetGraphic.color = _tab == Tab.Teams ? UITheme.Accent : UITheme.SurfaceAlt;

            string h = "Week " + (_standings != null ? _standings.week : 0);
            // The server falls back to the last week with results while the new week is still
            // empty (midnight UTC until that night's resolve) — say so rather than looking stale.
            if (_standings != null && !_standings.isCurrentWeek) h += " (final)";
            if (_champ != null) h += "   Last crown: " + _champ.name;
            _header.text = h;

            UIBuilder.Clear(_list);
            if (_tab == Tab.Individual) DrawIndividual(); else DrawTeams();
            UIBuilder.Rebuild(_list);
        }

        private void DrawIndividual()
        {
            var rows = _standings?.players;
            if (rows == null || rows.Length == 0) { Empty("No players yet."); return; }
            foreach (var r in rows)
            {
                var row = UIBuilder.Row(_list);
                var rankLbl = UIBuilder.Label(row.transform, "#" + r.rank, UITheme.SmallSize, UITheme.TextDim);
                UIBuilder.Sizing(rankLbl.gameObject, preferredWidth: 60, minWidth: 60);

                string title = CosmeticCatalog.DisplayName(r.title);
                string name = string.IsNullOrEmpty(title) ? r.name : title + " " + r.name;
                var nameLbl = UIBuilder.Label(row.transform, name, UITheme.SmallSize, Parse(r.colorHex));
                UIBuilder.Sizing(nameLbl.gameObject, flexibleWidth: 1);

                var winLbl = UIBuilder.Label(row.transform, r.wins + " W", UITheme.SmallSize, UITheme.Text);
                UIBuilder.Sizing(winLbl.gameObject, preferredWidth: 90, minWidth: 90);
            }
        }

        private void DrawTeams()
        {
            var rows = _standings?.teams;
            if (rows == null || rows.Length == 0) { Empty("No teams yet."); return; }
            foreach (var r in rows)
            {
                var row = UIBuilder.Row(_list);
                var rankLbl = UIBuilder.Label(row.transform, "#" + r.rank, UITheme.SmallSize, UITheme.TextDim);
                UIBuilder.Sizing(rankLbl.gameObject, preferredWidth: 60, minWidth: 60);
                var nameLbl = UIBuilder.Label(row.transform, r.name, UITheme.SmallSize, Parse(r.colorHex));
                UIBuilder.Sizing(nameLbl.gameObject, flexibleWidth: 1);
                var winLbl = UIBuilder.Label(row.transform, r.totalWins + " W (" + r.memberCount + ")", UITheme.SmallSize, UITheme.Text);
                UIBuilder.Sizing(winLbl.gameObject, preferredWidth: 150, minWidth: 150);
            }
        }

        private void Empty(string msg)
        {
            var e = UIBuilder.Label(_list, msg, UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(e.gameObject, minHeight: 60, preferredHeight: 60);
        }

        private static ChampionDto MostRecent(ChampionListDto list)
        {
            if (list?.champions == null || list.champions.Length == 0) return null;
            var best = list.champions[0];
            foreach (var c in list.champions) if (c.week > best.week) best = c;
            return best;
        }

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
