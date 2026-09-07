using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Home: this week's modifier, your record, last champion, and watchable battles.</summary>
    public sealed class HomePanel : AppPanel
    {
        public override string NavLabel => "Home";

        private UnitCatalog _defs;
        private TextMeshProUGUI _header;
        private RectTransform _list;
        private DayResultDto _result;

        protected override void Build(Transform content)
        {
            try { _defs = ConfigService.Units; } catch { }

            // Header block (top), fixed height.
            var head = new GameObject("Header", typeof(RectTransform));
            head.transform.SetParent(content, false);
            var hrt = UIBuilder.Rect(head);
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1);
            hrt.sizeDelta = new Vector2(0, 210);
            UIBuilder.VLayout(head, 8f, 20);
            _header = UIBuilder.Label(head.transform, "", UITheme.BodySize, UITheme.Text);

            // Battle list (fills below the header).
            var listHost = new GameObject("ListHost", typeof(RectTransform));
            listHost.transform.SetParent(content, false);
            var lrt = UIBuilder.Rect(listHost);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(0, 0); lrt.offsetMax = new Vector2(0, -210);
            _list = UIBuilder.ScrollView(listHost.transform, out _);
        }

        public override async void OnShow()
        {
            _header.text = "Loading...";
            UIBuilder.Clear(_list);
            try
            {
                _result = await DataService.LoadDayResultAsync();
                var champs = await CloudCodeService.GetChampionsAsync();
                await Shell.RefreshSparks();
                Populate(champs);
            }
            catch (Exception e) { _header.text = "Couldn't load: " + e.Message; }
        }

        private void Populate(ChampionListDto champs)
        {
            var mod = WeeklyModifierRotation.ForWeek(
                GameCalendar.WeekNumber(GameCalendar.LockTargetDay(DateTime.UtcNow)));

            int battleCount = _result?.battles?.Length ?? 0;
            string line = "This week: " + ModifierLabel(mod);
            if (_result != null) line += "\nYou went " + _result.wins + " - " + _result.losses
                + "  (day " + _result.day + ", " + battleCount + " battles)";
            var champ = MostRecent(champs);
            if (champ != null) line += "\nLast Dawn Crown: " + champ.name + " (" + champ.wins + " wins)";
            _header.text = line;

            UIBuilder.Clear(_list);
            if (battleCount == 0)
            {
                var empty = UIBuilder.Label(_list, "No battles yet. Lock a squad and check back after the resolve.",
                    UITheme.SmallSize, UITheme.TextDim);
                UIBuilder.Sizing(empty.gameObject, minHeight: 60, preferredHeight: 60);
                UIBuilder.Rebuild(_list);
                return;
            }

            foreach (var b in _result.battles)
                AddBattleRow(b);

            UIBuilder.Rebuild(_list);
        }

        private void AddBattleRow(BattleRecordDto b)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_list, false);
            row.GetComponent<Image>().color = UITheme.Surface;
            var hl = UIBuilder.HLayout(row, 10f, 12);
            hl.childAlignment = TextAnchor.MiddleLeft;
            UIBuilder.Sizing(row, minHeight: 76, preferredHeight: 76);

            string opp = string.IsNullOrEmpty(b.opponentName) ? "opponent" : b.opponentName;
            var label = UIBuilder.Label(row.transform, (b.won ? "WON" : "LOST") + "  vs " + opp,
                UITheme.SmallSize, b.won ? UITheme.Positive : UITheme.Negative);
            UIBuilder.Sizing(label.gameObject, flexibleWidth: 1);

            var watch = UIBuilder.Button(row.transform, "Watch", () => Watch(b), UITheme.Accent, UITheme.SmallSize);
            UIBuilder.Sizing(watch.gameObject, preferredWidth: 170, minWidth: 170, preferredHeight: 58, minHeight: 58);
        }

        private void Watch(BattleRecordDto b)
        {
            if (_result.mySquad == null || b.opponentSquad == null || _defs == null)
            {
                _header.text = "That battle predates replay data — run a fresh resolve to watch new ones.";
                return;
            }
            try
            {
                var mine = SquadCodec.FromDto(_result.mySquad);
                var opp = SquadCodec.FromDto(b.opponentSquad);
                bool meA = string.CompareOrdinal(Shell.PlayerId, b.opponentId) < 0;
                var a = meA ? mine : opp;
                var sideB = meA ? opp : mine;
                var mod = WeeklyModifierRotation.ById(b.modifierId);
                var result = BattleSimulator.Simulate(a, sideB, mod, b.seed, _defs);
                Shell.EnterReplay(a, sideB, mod, result, _defs);
            }
            catch (Exception e) { _header.text = "Replay failed: " + e.Message; }
        }

        private static string ModifierLabel(WeeklyModifier mod)
        {
            switch (mod.Id)
            {
                case "entrenched": return "Entrenched";
                case "glass_cannons": return "Glass Cannons";
                case "vanguards_hour": return "Vanguard's Hour";
                case "arcane_surge": return "Arcane Surge";
                default: return "No modifier";
            }
        }

        private static ChampionDto MostRecent(ChampionListDto list)
        {
            if (list?.champions == null || list.champions.Length == 0) return null;
            var best = list.champions[0];
            foreach (var c in list.champions) if (c.week > best.week) best = c;
            return best;
        }
    }
}
