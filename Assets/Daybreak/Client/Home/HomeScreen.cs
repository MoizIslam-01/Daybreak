using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// The daily payoff screen: your record from the last resolve, and a Watch button per battle
    /// that regenerates that exact fight on-device from the stored (squads, seed) and plays it
    /// through the M2 renderer. Nothing is streamed — the replay is rebuilt from a few bytes, which
    /// is the whole point of the deterministic sim.
    ///
    /// One component; IMGUI; add it to an empty scene with a ReplayPlayer (auto-added).
    /// </summary>
    [RequireComponent(typeof(ReplayPlayer))]
    public sealed class HomeScreen : MonoBehaviour
    {
        private enum Phase { Home, Watching }

        private UnitCatalog _defs;
        private ReplayPlayer _player;
        private Phase _phase = Phase.Home;

        private string _myId;
        private DayResultDto _result;
        private string _status = "Loading...";
        private Vector2 _scroll;

        private async void Start()
        {
            ConfigureCamera();
            _player = GetComponent<ReplayPlayer>();
            try { _defs = ConfigService.Units; }
            catch (Exception e) { Debug.LogError("[Daybreak] Units load failed: " + e.Message); }

            try
            {
                _myId = await AuthService.SignInAnonymouslyAsync();
                await Refresh();
            }
            catch (Exception e) { _status = "Sign-in failed: " + e.Message; }
        }

        private async System.Threading.Tasks.Task Refresh()
        {
            _status = "Loading your results...";
            try
            {
                _result = await DataService.LoadDayResultAsync();
                _status = _result == null
                    ? "No results yet. Lock a squad in Practice, run a resolve, then come back."
                    : "";
            }
            catch (Exception e) { _status = "Load failed: " + e.Message; }
        }

        private void OnGUI()
        {
            if (_phase == Phase.Watching)
            {
                if (GUI.Button(new Rect(Screen.width - 170, 10, 160, 30), "< Back to results"))
                {
                    _player.Clear();
                    _phase = Phase.Home;
                }
                return;
            }

            DrawHome();
        }

        private void DrawHome()
        {
            var area = new Rect(12, 12, Mathf.Min(560, Screen.width - 24), Screen.height - 24);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("DAYBREAK — Your day");
            if (GUILayout.Button("Refresh", GUILayout.Width(90))) _ = Refresh();

            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);

            if (_result != null)
            {
                GUILayout.Space(6);
                string me = string.IsNullOrEmpty(_result.myName) ? "You" : _result.myName;
                var meStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                meStyle.normal.textColor = Parse(_result.myColor);
                GUILayout.Label(me + " went " + _result.wins + " - " + _result.losses
                    + "   (day " + _result.day + ")", meStyle);
                GUILayout.Space(6);
                GUILayout.Label("BATTLES  (tap Watch to replay)");

                _scroll = GUILayout.BeginScrollView(_scroll);
                var battles = _result.battles;
                if (battles != null)
                {
                    for (int i = 0; i < battles.Length; i++)
                    {
                        var b = battles[i];
                        GUILayout.BeginHorizontal();
                        string tag = b.won ? "WON " : "LOST";
                        string oppName = string.IsNullOrEmpty(b.opponentName) ? Short(b.opponentId) : b.opponentName;
                        var oppStyle = new GUIStyle(GUI.skin.label);
                        oppStyle.normal.textColor = Parse(b.opponentColor);
                        GUILayout.Label(tag + "  vs ", GUILayout.Width(70));
                        GUILayout.Label(oppName, oppStyle, GUILayout.Width(280));
                        GUI.enabled = b.opponentSquad != null && _result.mySquad != null;
                        if (GUILayout.Button("Watch", GUILayout.Width(90))) Watch(b);
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        private void Watch(BattleRecordDto b)
        {
            try
            {
                var mine = SquadCodec.FromDto(_result.mySquad);
                var opp = SquadCodec.FromDto(b.opponentSquad);

                // Side A is always the lexicographically-smaller id — same rule the resolver used.
                bool meA = string.CompareOrdinal(_myId, b.opponentId) < 0;
                var a = meA ? mine : opp;
                var sideB = meA ? opp : mine;

                var mod = WeeklyModifier.None; // b.modifierId == "none" for now
                var result = BattleSimulator.Simulate(a, sideB, mod, b.seed, _defs);

                _phase = Phase.Watching;
                _player.Play(a, sideB, mod, result, _defs);
            }
            catch (Exception e)
            {
                _status = "Could not rebuild replay: " + e.Message;
            }
        }

        private static string Short(string id) =>
            string.IsNullOrEmpty(id) ? "?" : (id.Length <= 8 ? id : id.Substring(0, 8) + "…");

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

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
