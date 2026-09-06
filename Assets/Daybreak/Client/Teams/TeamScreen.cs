using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Create or join a team. Membership lives server-side (game data); your team id is mirrored
    /// into your profile. One component, IMGUI; add to an empty scene and press Play.
    /// </summary>
    public sealed class TeamScreen : MonoBehaviour
    {
        private static readonly string[] Palette =
        {
            "#E0574C", "#E08A3C", "#E7C13A", "#5FBF63",
            "#3FA9B0", "#4A90D9", "#8C6FD1", "#D96FB0"
        };

        private string _newName = "";
        private string _newColor = "#4A90D9";
        private string _myTeamId = "";
        private TeamDto[] _teams = new TeamDto[0];
        private string _status = "Loading...";
        private Vector2 _scroll;

        private async void Start()
        {
            try
            {
                await AuthService.SignInAnonymouslyAsync();
                var profile = await DataService.LoadProfileAsync();
                _myTeamId = profile?.teamId ?? "";
                await RefreshTeams();
            }
            catch (Exception e) { _status = "Load failed: " + e.Message; }
        }

        private async System.Threading.Tasks.Task RefreshTeams()
        {
            _status = "Loading teams...";
            try
            {
                var list = await CloudCodeService.ListTeamsAsync();
                _teams = list?.teams ?? new TeamDto[0];
                var profile = await DataService.LoadProfileAsync();
                _myTeamId = profile?.teamId ?? "";
                _status = _teams.Length + " team(s).";
            }
            catch (Exception e) { _status = "List failed: " + e.Message; }
        }

        private void OnGUI()
        {
            var area = new Rect(12, 12, Mathf.Min(520, Screen.width - 24), Screen.height - 24);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("DAYBREAK — Teams");
            GUILayout.Label("Your team: " + (string.IsNullOrEmpty(_myTeamId) ? "(none)" : _myTeamId));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(90))) _ = RefreshTeams();
            GUI.enabled = !string.IsNullOrEmpty(_myTeamId);
            if (GUILayout.Button("Leave team", GUILayout.Width(110))) Leave();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("CREATE A TEAM");
            _newName = GUILayout.TextField(_newName ?? "", TeamRules.MaxNameLength);
            GUILayout.BeginHorizontal();
            foreach (var hex in Palette)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Parse(hex);
                string mark = hex.Equals(_newColor, StringComparison.OrdinalIgnoreCase) ? "•" : " ";
                if (GUILayout.Button(mark, GUILayout.Width(36), GUILayout.Height(26))) _newColor = hex;
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = _newName.Trim().Length > 0;
            if (GUILayout.Button("Create team", GUILayout.Height(30))) Create();
            GUI.enabled = true;

            GUILayout.Space(10);
            GUILayout.Label("JOIN A TEAM");
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var t in _teams)
            {
                GUILayout.BeginHorizontal();
                var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                style.normal.textColor = Parse(t.colorHex);
                int members = t.memberIds?.Length ?? 0;
                GUILayout.Label(t.name + "  (" + members + ")", style, GUILayout.Width(320));
                GUI.enabled = t.id != _myTeamId;
                if (GUILayout.Button(t.id == _myTeamId ? "Joined" : "Join", GUILayout.Width(90))) Join(t.id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private async void Create()
        {
            _status = "Creating...";
            try
            {
                var r = await CloudCodeService.CreateTeamAsync(_newName, _newColor);
                _status = r.ok ? "Created team " + r.teamId : ("Create failed: " + r.error);
                if (r.ok) { _newName = ""; await RefreshTeams(); }
            }
            catch (Exception e) { _status = "Create error: " + e.Message; }
        }

        private async void Join(string teamId)
        {
            _status = "Joining...";
            try
            {
                var r = await CloudCodeService.JoinTeamAsync(teamId);
                _status = r.ok ? "Joined " + r.teamId : ("Join failed: " + r.error);
                if (r.ok) await RefreshTeams();
            }
            catch (Exception e) { _status = "Join error: " + e.Message; }
        }

        private async void Leave()
        {
            _status = "Leaving...";
            try
            {
                await CloudCodeService.LeaveTeamAsync();
                await RefreshTeams();
                _status = "Left team.";
            }
            catch (Exception e) { _status = "Leave error: " + e.Message; }
        }

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
