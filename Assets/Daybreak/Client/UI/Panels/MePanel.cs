using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Profile: set display name, accent color, and badge; saved to Cloud Save.</summary>
    public sealed class MePanel : AppPanel
    {
        public override string NavLabel => "Me";

        private static readonly string[] Palette =
        { "#E0574C", "#E08A3C", "#E7C13A", "#5FBF63", "#3FA9B0", "#4A90D9", "#8C6FD1", "#D96FB0" };
        private static readonly string[] Badges = { "", "*", "+", "#", "!", "@" };

        private TMP_InputField _nameInput;
        private TextMeshProUGUI _preview, _status;
        private string _color = ProfileRules.DefaultColorHex;
        private string _badge = "";

        protected override void Build(Transform content)
        {
            var list = UIBuilder.ScrollView(content, out _);

            Head(list, "DISPLAY NAME");
            _nameInput = UIBuilder.InputField(list, "Your name");
            UIBuilder.Sizing(_nameInput.gameObject, minHeight: 66, preferredHeight: 66);
            _nameInput.onValueChanged.AddListener(_ => UpdatePreview());

            Head(list, "ACCENT COLOR");
            var colorRow = SubRow(list);
            foreach (var hex in Palette)
            {
                string h = hex;
                var b = UIBuilder.Button(colorRow.transform, " ", () => { _color = h; UpdatePreview(); }, Parse(hex), UITheme.SmallSize);
                UIBuilder.Sizing(b.gameObject, minWidth: 96, preferredWidth: 96);
            }

            Head(list, "BADGE");
            var badgeRow = SubRow(list);
            foreach (var bd in Badges)
            {
                string bb = bd;
                var b = UIBuilder.Button(badgeRow.transform, string.IsNullOrEmpty(bd) ? "none" : bd,
                    () => { _badge = bb; UpdatePreview(); }, UITheme.SurfaceAlt, UITheme.SmallSize);
                UIBuilder.Sizing(b.gameObject, minWidth: 110, preferredWidth: 110);
            }

            Head(list, "PREVIEW");
            _preview = UIBuilder.Label(list, "", UITheme.HeaderSize, UITheme.Text);
            UIBuilder.Sizing(_preview.gameObject, minHeight: 60, preferredHeight: 60);

            var save = UIBuilder.Button(list, "Save profile", Save, UITheme.Accent, UITheme.BodySize);
            UIBuilder.Sizing(save.gameObject, minHeight: 72, preferredHeight: 72);

            _status = UIBuilder.Label(list, "", UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(_status.gameObject, minHeight: 50, preferredHeight: 50);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var reset = UIBuilder.Button(list, "Reset onboarding (dev)", ResetOnboarding, UITheme.SurfaceAlt, UITheme.SmallSize);
            UIBuilder.Sizing(reset.gameObject, minHeight: 60, preferredHeight: 60);

            var wipe = UIBuilder.Button(list, "WIPE ALL DATA (dev)", Wipe, UITheme.Negative, UITheme.SmallSize);
            UIBuilder.Sizing(wipe.gameObject, minHeight: 60, preferredHeight: 60);
#endif
        }

        public override async void OnShow()
        {
            try
            {
                var p = await DataService.LoadProfileAsync();
                if (p != null)
                {
                    _nameInput.text = p.displayName ?? "";
                    _color = string.IsNullOrEmpty(p.colorHex) ? ProfileRules.DefaultColorHex : p.colorHex;
                    _badge = p.emoji ?? "";
                }
                _status.text = p != null ? "Loaded your profile." : "Set your profile below.";
                UpdatePreview();
            }
            catch (Exception e) { _status.text = "Load failed: " + e.Message; }
        }

        private void UpdatePreview()
        {
            var clean = ProfileRules.Sanitize(new ProfileDto { displayName = _nameInput.text, colorHex = _color, emoji = _badge });
            _preview.color = Parse(clean.colorHex);
            _preview.text = (string.IsNullOrEmpty(clean.emoji) ? "" : clean.emoji + " ") + clean.displayName;
        }

        private async void Save()
        {
            if (string.IsNullOrWhiteSpace(_nameInput.text)) { _status.text = "Enter a name first."; return; }
            _status.text = "Saving...";
            try
            {
                var r = await CloudCodeService.SaveProfileAsync(
                    new ProfileDto { displayName = _nameInput.text, colorHex = _color, emoji = _badge });
                if (!r.ok) { _status.text = r.error; return; }
                var saved = ProfileRules.Sanitize(new ProfileDto { displayName = _nameInput.text, colorHex = _color, emoji = _badge });
                _nameInput.text = saved.displayName; _color = saved.colorHex;
                _status.text = "Saved as \"" + saved.displayName + "\".";
                UpdatePreview();
            }
            catch (Exception e) { _status.text = "Save failed: " + e.Message; }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private async void ResetOnboarding()
        {
            _status.text = "Clearing...";
            try
            {
                await DataService.ClearProfileAsync();
                _status.text = "Cleared. Stop and press Play again to see the welcome screen.";
            }
            catch (Exception e) { _status.text = "Reset failed: " + e.Message; }
        }

        private async void Wipe()
        {
            _status.text = "Wiping all data...";
            try
            {
                var msg = await CloudCodeService.WipeAllAsync();
                await DataService.ClearProfileAsync();
                _status.text = msg;
            }
            catch (Exception e) { _status.text = "Wipe failed: " + e.Message; }
        }
#endif

        private void Head(Transform parent, string text)
        {
            var l = UIBuilder.Label(parent, text, UITheme.SmallSize, UITheme.TextDim);
            UIBuilder.Sizing(l.gameObject, minHeight: 40, preferredHeight: 40);
        }

        private GameObject SubRow(Transform parent)
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
