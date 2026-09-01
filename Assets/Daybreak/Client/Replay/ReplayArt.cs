using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Placeholder art helpers — everything the replay renders is generated in code (colored
    /// squares, no imported sprites). Keeps M2 asset-free per the guide: "prefer placeholder art
    /// until the core loop is fun."
    /// </summary>
    public static class ReplayArt
    {
        private static Sprite _centerSquare;
        private static Sprite _leftSquare;

        /// <summary>A 1-unit white square, centre pivot. Tint via SpriteRenderer.color.</summary>
        public static Sprite Square()
        {
            if (_centerSquare == null) _centerSquare = Build(new Vector2(0.5f, 0.5f));
            return _centerSquare;
        }

        /// <summary>A white square with a left pivot, so scaling x grows it rightward (HP bars).</summary>
        public static Sprite LeftSquare()
        {
            if (_leftSquare == null) _leftSquare = Build(new Vector2(0f, 0.5f));
            return _leftSquare;
        }

        private static Sprite Build(Vector2 pivot)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), pivot, 4f);
        }

        public static Color ArchetypeColor(Archetype a)
        {
            switch (a)
            {
                case Archetype.Vanguard: return new Color(0.30f, 0.52f, 0.85f);   // steel blue
                case Archetype.Skirmisher: return new Color(0.86f, 0.34f, 0.32f); // crimson
                case Archetype.Marksman: return new Color(0.38f, 0.74f, 0.42f);   // green
                default: return Color.gray;
            }
        }

        public static Color TagColor(Tag t)
        {
            switch (t)
            {
                case Tag.Guardian: return new Color(0.70f, 0.78f, 0.90f); // pale blue
                case Tag.Pack: return new Color(0.95f, 0.60f, 0.25f);     // orange
                case Tag.Arcane: return new Color(0.70f, 0.45f, 0.90f);   // purple
                case Tag.Swift: return new Color(0.95f, 0.88f, 0.30f);    // yellow
                default: return Color.white;
            }
        }

        /// <summary>Hit colour by archetype multiplier (permille) — favourable pops, weak is dim.</summary>
        public static Color MultiplierColor(int permille)
        {
            if (permille >= BattleSimulator.FavorablePermille) return new Color(1f, 0.75f, 0.2f); // orange
            if (permille <= BattleSimulator.UnfavorablePermille) return new Color(0.6f, 0.6f, 0.6f); // gray
            return Color.white;
        }
    }
}
