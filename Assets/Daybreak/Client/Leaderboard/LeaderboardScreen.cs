using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// The weekly standings — individual and team tabs — computed server-side from everyone's
    /// weekly Cloud Save totals and shown with names and colors. One component, IMGUI.
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private enum Tab { Individual, Teams }

        private StandingsDto _standings;
        private Tab _tab = Tab.Individual;
        private string _status = "Loading...";
        private Vector2 _scroll;

        private async void Start()
        {
            try
            {
                await AuthService.SignInAnonymouslyAsync();
                await Refresh();
            }
            catch (Exception e) { _status = "Sign-in failed: " + e.Message; }
        }

        private async System.Threading.Tasks.Task Refresh()
        {
            _status = "Loading standings...";
            try
            {
                _standings = await CloudCodeService.GetStandingsAsync();
                _status = "Week " + _standings.week;
            }
            catch (Exception e) { _status = "Load failed: " + e.Message; }
        }

        private void OnGUI()
        {
            var area = new Rect(12, 12, Mathf.Min(540, Screen.width - 24), Screen.height - 24);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("DAYBREAK — Weekly standings");
            GUILayout.BeginHorizontal();
            if (TabButton("Individual", Tab.Individual)) _tab = Tab.Individual;
            if (TabButton("Teams", Tab.Teams)) _tab = Tab.Teams;
            if (GUILayout.Button("Refresh", GUILayout.Width(90))) _ = Refresh();
            GUILayout.EndHorizontal();
            GUILayout.Label(_status);

            GUILayout.Space(6);
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_standings != null)
            {
                if (_tab == Tab.Individual) DrawIndividual();
                else DrawTeams();
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private bool TabButton(string label, Tab tab)
        {
            var prev = GUI.backgroundColor;
            if (_tab == tab) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            bool clicked = GUILayout.Button(label, GUILayout.Width(120));
            GUI.backgroundColor = prev;
            return clicked;
        }

        private void DrawIndividual()
        {
            var rows = _standings.players;
            if (rows == null || rows.Length == 0) { GUILayout.Label("No players yet."); return; }
            foreach (var r in rows)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("#" + r.rank, GUILayout.Width(44));
                var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                style.normal.textColor = Parse(r.colorHex);
                string title = TitleName(r.title);
                string label = string.IsNullOrEmpty(title) ? r.name : (title + " " + r.name);
                GUILayout.Label(label, style, GUILayout.Width(320));
                GUILayout.Label(r.wins + " W", GUILayout.Width(60));
                GUILayout.EndHorizontal();
            }
        }

        private void DrawTeams()
        {
            var rows = _standings.teams;
            if (rows == null || rows.Length == 0) { GUILayout.Label("No teams yet."); return; }
            foreach (var r in rows)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("#" + r.rank, GUILayout.Width(44));
                var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                style.normal.textColor = Parse(r.colorHex);
                GUILayout.Label(r.name, style, GUILayout.Width(240));
                GUILayout.Label(r.totalWins + " W", GUILayout.Width(60));
                GUILayout.Label("(" + r.memberCount + ")", GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }
        }

        private static string TitleName(string id)
        {
            var c = CosmeticCatalog.Get(id);
            return c != null ? c.name : "";
        }

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
