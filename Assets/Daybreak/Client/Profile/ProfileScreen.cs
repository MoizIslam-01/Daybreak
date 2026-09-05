using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Lets a player set their public identity — display name, accent color, and a badge glyph —
    /// stored in their own Cloud Save. This is what everyone else sees instead of a raw player id.
    /// One component, IMGUI; add it to an empty scene and press Play.
    /// </summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        private static readonly string[] Palette =
        {
            "#E0574C", "#E08A3C", "#E7C13A", "#5FBF63",
            "#3FA9B0", "#4A90D9", "#8C6FD1", "#D96FB0"
        };

        private static readonly string[] Badges = { "", "★", "♦", "♣", "♠", "♥", "●", "▲", "■" };

        private string _name = "";
        private string _colorHex = ProfileRules.DefaultColorHex;
        private string _badge = "";
        private string _status = "Loading...";

        private async void Start()
        {
            try
            {
                await AuthService.SignInAnonymouslyAsync();
                var p = await DataService.LoadProfileAsync();
                if (p != null)
                {
                    _name = p.displayName ?? "";
                    _colorHex = string.IsNullOrEmpty(p.colorHex) ? ProfileRules.DefaultColorHex : p.colorHex;
                    _badge = p.emoji ?? "";
                    _status = "Loaded your profile.";
                }
                else _status = "No profile yet — set one below.";
            }
            catch (Exception e) { _status = "Load failed: " + e.Message; }
        }

        private void OnGUI()
        {
            var area = new Rect(12, 12, Mathf.Min(460, Screen.width - 24), 420);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("DAYBREAK — Your profile");

            GUILayout.Space(6);
            GUILayout.Label("Display name (max " + ProfileRules.MaxNameLength + ")");
            _name = GUILayout.TextField(_name ?? "", ProfileRules.MaxNameLength);

            GUILayout.Space(8);
            GUILayout.Label("Accent color");
            GUILayout.BeginHorizontal();
            foreach (var hex in Palette)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Parse(hex);
                string mark = hex.Equals(_colorHex, StringComparison.OrdinalIgnoreCase) ? "•" : " ";
                if (GUILayout.Button(mark, GUILayout.Width(40), GUILayout.Height(30))) _colorHex = hex;
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Badge");
            GUILayout.BeginHorizontal();
            foreach (var b in Badges)
            {
                var prev = GUI.backgroundColor;
                if (b == _badge) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                if (GUILayout.Button(string.IsNullOrEmpty(b) ? "none" : b, GUILayout.Width(46), GUILayout.Height(30)))
                    _badge = b;
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            DrawPreview();

            GUILayout.Space(12);
            if (GUILayout.Button("Save profile", GUILayout.Height(36))) Save();

            GUILayout.Space(8);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void DrawPreview()
        {
            var preview = ProfileRules.Sanitize(new ProfileDto
            {
                displayName = _name, colorHex = _colorHex, emoji = _badge
            });

            var style = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            style.normal.textColor = Parse(preview.colorHex);
            GUILayout.Label("Preview:");
            GUILayout.Label((string.IsNullOrEmpty(preview.emoji) ? "" : preview.emoji + " ") + preview.displayName, style);
        }

        private async void Save()
        {
            _status = "Saving...";
            try
            {
                await DataService.SaveProfileAsync(new ProfileDto
                {
                    displayName = _name, colorHex = _colorHex, emoji = _badge
                });
                var saved = ProfileRules.Sanitize(new ProfileDto { displayName = _name, colorHex = _colorHex, emoji = _badge });
                _name = saved.displayName; _colorHex = saved.colorHex;
                _status = "Saved as \"" + saved.displayName + "\".";
            }
            catch (Exception e) { _status = "Save failed: " + e.Message; }
        }

        private static Color Parse(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.gray;
        }
    }
}
