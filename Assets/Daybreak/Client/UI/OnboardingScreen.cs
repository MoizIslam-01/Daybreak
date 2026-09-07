using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>
    /// First-run welcome + profile setup. Shown when a player has no profile yet; on "Start playing"
    /// it saves the profile and hands control to the main app. Its own full-screen canvas.
    /// </summary>
    public sealed class OnboardingScreen
    {
        private static readonly string[] Palette =
        { "#E0574C", "#E08A3C", "#E7C13A", "#5FBF63", "#3FA9B0", "#4A90D9", "#8C6FD1", "#D96FB0" };
        private static readonly string[] Badges = { "", "*", "+", "#", "!", "@" };

        private Canvas _canvas;
        private TMP_InputField _name;
        private TextMeshProUGUI _preview, _status;
        private string _color = ProfileRules.DefaultColorHex;
        private string _badge = "";
        private Action _onDone;

        public void Show(Action onDone)
        {
            _onDone = onDone;

            _canvas = UIBuilder.CreateCanvas("Onboarding");
            _canvas.sortingOrder = 50;
            UIBuilder.Panel(_canvas.transform, UITheme.Bg, "Background");

            var list = UIBuilder.ScrollView(_canvas.transform, out _);

            Head(list, "Welcome to DAYBREAK", UITheme.TitleSize, UITheme.Text, 80);
            Head(list, "Set up your player — this is what your friends will see.", UITheme.SmallSize, UITheme.TextDim, 50);

            Head(list, "YOUR NAME", UITheme.SmallSize, UITheme.TextDim, 40);
            _name = UIBuilder.InputField(list, "Enter a name");
            UIBuilder.Sizing(_name.gameObject, minHeight: 70, preferredHeight: 70);
            _name.onValueChanged.AddListener(_ => UpdatePreview());

            Head(list, "ACCENT COLOR", UITheme.SmallSize, UITheme.TextDim, 40);
            var colorRow = Sub(list);
            foreach (var hex in Palette)
            {
                string h = hex;
                var b = UIBuilder.Button(colorRow.transform, " ", () => { _color = h; UpdatePreview(); }, Parse(hex));
                UIBuilder.Sizing(b.gameObject, minWidth: 96, preferredWidth: 96);
            }

            Head(list, "BADGE", UITheme.SmallSize, UITheme.TextDim, 40);
            var badgeRow = Sub(list);
            foreach (var bd in Badges)
            {
                string bb = bd;
                var b = UIBuilder.Button(badgeRow.transform, string.IsNullOrEmpty(bd) ? "none" : bd,
                    () => { _badge = bb; UpdatePreview(); }, UITheme.SurfaceAlt);
                UIBuilder.Sizing(b.gameObject, minWidth: 110, preferredWidth: 110);
            }

            Head(list, "PREVIEW", UITheme.SmallSize, UITheme.TextDim, 40);
            _preview = UIBuilder.Label(list, "", UITheme.HeaderSize, UITheme.Text);
            UIBuilder.Sizing(_preview.gameObject, minHeight: 64, preferredHeight: 64);

            var start = UIBuilder.Button(list, "Start playing", StartPlaying, UITheme.Accent, UITheme.BodySize);
            UIBuilder.Sizing(start.gameObject, minHeight: 88, preferredHeight: 88);

            _status = UIBuilder.Label(list, "", UITheme.SmallSize, UITheme.Negative);
            UIBuilder.Sizing(_status.gameObject, minHeight: 44, preferredHeight: 44);

            UpdatePreview();
            UIBuilder.Rebuild(list);
        }

        private void UpdatePreview()
        {
            var clean = ProfileRules.Sanitize(new ProfileDto { displayName = _name.text, colorHex = _color, emoji = _badge });
            _preview.color = Parse(clean.colorHex);
            _preview.text = (string.IsNullOrEmpty(clean.emoji) ? "" : clean.emoji + " ") + clean.displayName;
        }

        private async void StartPlaying()
        {
            if (string.IsNullOrWhiteSpace(_name.text)) { _status.text = "Please enter a name."; return; }
            _status.text = "Saving...";
            try
            {
                var r = await CloudCodeService.SaveProfileAsync(
                    new ProfileDto { displayName = _name.text, colorHex = _color, emoji = _badge });
                if (!r.ok) { _status.text = r.error; return; } // e.g. name taken — stay on onboarding
                UnityEngine.Object.Destroy(_canvas.gameObject);
                _onDone?.Invoke();
            }
            catch (Exception e) { _status.text = "Couldn't save: " + e.Message; }
        }

        private void Head(Transform parent, string text, int size, Color color, float height)
        {
            var l = UIBuilder.Label(parent, text, size, color, TextAlignmentOptions.Center);
            UIBuilder.Sizing(l.gameObject, minHeight: height, preferredHeight: height);
        }

        private GameObject Sub(Transform parent)
        {
            var row = new GameObject("SubRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var h = UIBuilder.HLayout(row, 8f, 0);
            h.childForceExpandHeight = true;
            UIBuilder.Sizing(row, minHeight: 84, preferredHeight: 84);
            return row;
        }

        private static Color Parse(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
