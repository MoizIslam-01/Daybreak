using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>
    /// Procedural uGUI helpers — build a phone-ready Canvas and common widgets in code, so the whole
    /// UI lives in scripts (one component per scene) instead of hand-wired prefabs. TMP for text.
    /// </summary>
    public static class UIBuilder
    {
        /// <summary>A portrait-phone canvas that scales to any device (1080x1920 reference).</summary>
        public static Canvas CreateCanvas(string name)
        {
            EnsureEventSystem();

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            // New Input System is active in this project, so use its UI module (not StandaloneInputModule).
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ---- primitives ----

        public static RectTransform Rect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            return rt != null ? rt : go.AddComponent<RectTransform>();
        }

        public static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>A full-rect image panel under parent.</summary>
        public static GameObject Panel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            Stretch(Rect(go));
            return go;
        }

        /// <summary>A colored block of a fixed height, for bars anchored top/bottom.</summary>
        public static GameObject Bar(Transform parent, Color color, float height, bool top)
        {
            var go = new GameObject(top ? "TopBar" : "BottomBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rt = Rect(go);
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, int size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string label, Action onClick,
            Color? bg = null, int size = UITheme.BodySize)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg ?? UITheme.Accent;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var tmp = Label(go.transform, label, size, UITheme.Text, TextAlignmentOptions.Center);
            Stretch(Rect(tmp.gameObject), 8f);

            return btn;
        }

        // ---- layout ----

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing = 12f, int pad = 16,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing = 12f, int pad = 0,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Sizing(GameObject go, float minHeight = -1, float preferredHeight = -1,
            float minWidth = -1, float preferredWidth = -1, float flexibleWidth = -1)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (minHeight >= 0) le.minHeight = minHeight;
            if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
            if (minWidth >= 0) le.minWidth = minWidth;
            if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            return le;
        }

        /// <summary>A vertical scroll view; returns the content transform to add rows to.</summary>
        public static RectTransform ScrollView(Transform parent, out ScrollRect scrollRect)
        {
            // RectMask2D clips to the rect (no stencil graphic needed). A transparent Image with
            // raycastTarget on lets touch-drag scrolling register.
            var viewport = new GameObject("Scroll", typeof(RectTransform), typeof(Image),
                typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0f);
            vpImg.raycastTarget = true;
            Stretch(Rect(viewport));

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = Rect(content);
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = Vector2.zero;

            VLayout(content, 8f, 8);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.content = crt;
            scrollRect.viewport = Rect(viewport);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            return crt;
        }

        /// <summary>A TMP text input with placeholder, built procedurally.</summary>
        public static TMP_InputField InputField(Transform parent, string placeholder, int size = UITheme.BodySize)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = UITheme.SurfaceAlt;
            var input = go.GetComponent<TMP_InputField>();

            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            Stretch(Rect(area), 12f);

            var ph = Label(area.transform, placeholder, size, UITheme.TextDim);
            Stretch(Rect(ph.gameObject));
            var txt = Label(area.transform, "", size, UITheme.Text);
            Stretch(Rect(txt.gameObject));

            input.textViewport = Rect(area);
            input.textComponent = txt;
            input.placeholder = ph;
            input.fontAsset = txt.font;
            input.onFocusSelectAll = false;
            return input;
        }

        /// <summary>A labeled row container (Image bg) that lays out children horizontally.</summary>
        public static GameObject Row(Transform parent, float height = 76f, int pad = 12)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = UITheme.Surface;
            var hl = HLayout(row, 10f, pad);
            hl.childAlignment = TextAnchor.MiddleLeft;
            Sizing(row, minHeight: height, preferredHeight: height);
            return row;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>
        /// Force a layout pass. Needed after populating a ContentSizeFitter-driven list at runtime —
        /// otherwise the content keeps a zero size and a scroll mask clips everything away.
        /// </summary>
        public static void Rebuild(RectTransform rt)
        {
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
