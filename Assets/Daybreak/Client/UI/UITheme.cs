using UnityEngine;

namespace Daybreak.Client.UI
{
    /// <summary>Shared colors and sizes for the app UI. One place to retheme.</summary>
    public static class UITheme
    {
        public static readonly Color Bg = new Color(0.09f, 0.10f, 0.13f);
        public static readonly Color Surface = new Color(0.15f, 0.17f, 0.21f);
        public static readonly Color SurfaceAlt = new Color(0.19f, 0.22f, 0.27f);
        public static readonly Color NavBar = new Color(0.12f, 0.13f, 0.17f);
        public static readonly Color Accent = new Color(0.29f, 0.56f, 0.85f);
        public static readonly Color AccentDim = new Color(0.22f, 0.30f, 0.40f);
        public static readonly Color Text = new Color(0.92f, 0.94f, 0.97f);
        public static readonly Color TextDim = new Color(0.60f, 0.65f, 0.72f);
        public static readonly Color Positive = new Color(0.35f, 0.80f, 0.45f);
        public static readonly Color Negative = new Color(0.88f, 0.36f, 0.34f);
        public static readonly Color Gold = new Color(0.96f, 0.85f, 0.32f);
        public static readonly Color ButtonDisabled = new Color(0.25f, 0.27f, 0.30f);

        public const int TitleSize = 40;
        public const int HeaderSize = 30;
        public const int BodySize = 26;
        public const int SmallSize = 22;

        public const float NavHeight = 150f;
        public const float TopBarHeight = 120f;
    }
}
