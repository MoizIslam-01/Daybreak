using System;
using System.Collections.Generic;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>
    /// The app: one component that builds a phone-ready Canvas with a top bar, a bottom nav, and a
    /// stack of panels it switches between. Signs in once on boot. Add it to an empty GameObject in
    /// the Main scene and press Play — no other wiring.
    /// </summary>
    public sealed class AppShell : MonoBehaviour
    {
        private readonly List<AppPanel> _panels = new List<AppPanel>();
        private readonly List<Button> _navButtons = new List<Button>();
        private int _active = -1;

        private TextMeshProUGUI _sparksLabel;
        private RectTransform _content;
        private Transform _navBar;
        private Canvas _canvas;

        // Replay integration (world-space renderer overlaid while watching).
        private ReplayPlayer _replay;
        private GameObject _replayOverlay;
        private Camera _replayCamera;

        public string PlayerId { get; private set; }

        private bool _built;

        private async void Start()
        {
            try
            {
                PlayerId = await AuthService.SignInAnonymouslyAsync();
                NotificationService.ScheduleDailyResultReminder();

                // First run (no profile yet) → onboarding; otherwise straight into the app.
                var profile = await DataService.LoadProfileAsync();
                if (profile == null || string.IsNullOrEmpty(profile.displayName))
                    new OnboardingScreen().Show(EnterApp);
                else
                    EnterApp();
            }
            catch (Exception e)
            {
                EnterApp();
                SetStatus("Sign-in issue: " + e.Message);
            }
        }

        private async void EnterApp()
        {
            if (!_built)
            {
                BuildChrome();
                RegisterPanels();
                _built = true;
            }
            _canvas.gameObject.SetActive(true);
            await RefreshSparks();
            Show(0); // Home
        }

        // ---- chrome: canvas, top bar, content area, bottom nav ----

        private void BuildChrome()
        {
            var canvas = UIBuilder.CreateCanvas("AppCanvas");
            _canvas = canvas;
            UIBuilder.Panel(canvas.transform, UITheme.Bg, "Background");

            // Top bar: title + Sparks.
            var top = UIBuilder.Bar(canvas.transform, UITheme.NavBar, UITheme.TopBarHeight, top: true);
            var title = UIBuilder.Label(top.transform, "DAYBREAK", UITheme.HeaderSize, UITheme.Text);
            var trt = UIBuilder.Rect(title.gameObject);
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(0.6f, 1);
            trt.offsetMin = new Vector2(28, 0); trt.offsetMax = new Vector2(0, -20);
            title.alignment = TextAlignmentOptions.Left;

            _sparksLabel = UIBuilder.Label(top.transform, "", UITheme.BodySize, UITheme.Gold);
            var srt = UIBuilder.Rect(_sparksLabel.gameObject);
            srt.anchorMin = new Vector2(0.4f, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(0, 0); srt.offsetMax = new Vector2(-28, -20);
            _sparksLabel.alignment = TextAlignmentOptions.Right;

            // Content area fills between the bars.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(canvas.transform, false);
            _content = UIBuilder.Rect(contentGo);
            _content.anchorMin = Vector2.zero; _content.anchorMax = Vector2.one;
            _content.offsetMin = new Vector2(0, UITheme.NavHeight);
            _content.offsetMax = new Vector2(0, -UITheme.TopBarHeight);

            // Bottom nav (buttons added in RegisterPanels).
            var nav = UIBuilder.Bar(canvas.transform, UITheme.NavBar, UITheme.NavHeight, top: false);
            var navLayout = UIBuilder.HLayout(nav, 4f, 6, TextAnchor.MiddleCenter);
            navLayout.childForceExpandWidth = true;
            navLayout.childForceExpandHeight = true;
            _navBar = nav.transform;
        }

        private void RegisterPanels()
        {
            // Phase A: stubs so navigation + layout can be verified. Phase B swaps in real panels.
            AddPanel(new HomePanel());
            AddPanel(new SquadPanel());
            AddPanel(new BoardPanel());
            AddPanel(new TeamPanel());
            AddPanel(new ShopPanel());
            AddPanel(new MePanel());

            for (int i = 0; i < _panels.Count; i++)
            {
                int index = i;
                var btn = UIBuilder.Button(_navBar, _panels[i].NavLabel, () => Show(index),
                    UITheme.NavBar, UITheme.SmallSize);
                UIBuilder.Sizing(btn.gameObject, flexibleWidth: 1);
                _navButtons.Add(btn);
            }
        }

        private void AddPanel(AppPanel panel)
        {
            panel.Init(_content, this);
            _panels.Add(panel);
        }

        // ---- navigation ----

        public void Show(int index)
        {
            if (index < 0 || index >= _panels.Count) return;
            for (int i = 0; i < _panels.Count; i++)
            {
                bool on = i == index;
                _panels[i].SetVisible(on);
                if (i < _navButtons.Count)
                    _navButtons[i].targetGraphic.color = on ? UITheme.Accent : UITheme.SurfaceAlt;
            }
            _active = index;
        }

        // ---- shared top-bar state ----

        public async System.Threading.Tasks.Task RefreshSparks()
        {
            try
            {
                var wallet = await DataService.LoadWalletAsync();
                _sparksLabel.text = "Sparks " + (wallet != null ? wallet.sparks : 0);
            }
            catch { /* leave as-is */ }
        }

        private void SetStatus(string s)
        {
            if (_sparksLabel != null && !string.IsNullOrEmpty(s)) _sparksLabel.text = s;
        }

        // ---- replay: hide the UI, show the world-space battle, offer Back ----

        public void EnterReplay(Squad a, Squad b, WeeklyModifier mod, BattleResult result, UnitCatalog defs)
        {
            ConfigureReplayCamera();
            if (_replay == null) _replay = gameObject.AddComponent<ReplayPlayer>();

            _canvas.gameObject.SetActive(false);
            BuildReplayOverlay();
            _replayOverlay.SetActive(true);
            _replay.Play(a, b, mod, result, defs);
        }

        public void ExitReplay()
        {
            if (_replay != null) _replay.Clear();
            if (_replayOverlay != null) _replayOverlay.SetActive(false);
            if (_replayCamera != null) _replayCamera.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(true);
            if (_active >= 0) _panels[_active].SetVisible(true);
        }

        private void ConfigureReplayCamera()
        {
            if (_replayCamera == null)
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    var go = new GameObject("ReplayCamera") { tag = "MainCamera" };
                    cam = go.AddComponent<Camera>();
                }
                _replayCamera = cam;
            }
            _replayCamera.gameObject.SetActive(true);
            _replayCamera.orthographic = true;
            _replayCamera.transform.position = new Vector3(0f, 0f, -10f);
            _replayCamera.backgroundColor = UITheme.Bg;
            _replayCamera.clearFlags = CameraClearFlags.SolidColor;

            // The battle spans roughly x +/-9 and y +/-5. Orthographic size is half the VERTICAL
            // extent, so on a tall portrait screen we must grow it until the WIDTH fits too —
            // otherwise the two squads get clipped off the sides.
            const float halfWidth = 9.5f;
            const float halfHeight = 5f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            _replayCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, aspect));
        }

        private void BuildReplayOverlay()
        {
            if (_replayOverlay != null) return;

            var overlayCanvas = UIBuilder.CreateCanvas("ReplayOverlay");
            overlayCanvas.sortingOrder = 100; // above the replay world
            _replayOverlay = overlayCanvas.gameObject;

            var back = UIBuilder.Button(_replayOverlay.transform, "< Back", ExitReplay, UITheme.Surface, UITheme.SmallSize);
            var rt = UIBuilder.Rect(back.gameObject);
            // Top-right, clear of ReplayPlayer's top-left IMGUI controls.
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(200, 80);
            rt.anchoredPosition = new Vector2(-24, -24);
        }
    }
}
