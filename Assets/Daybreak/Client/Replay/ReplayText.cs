using System.Collections;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Text helpers for the replay: the built-in font (fetched defensively, since Unity has
    /// renamed it across versions) and floating damage numbers that rise and fade.
    /// </summary>
    public static class ReplayText
    {
        private static Font _font;
        private static bool _fontResolved;

        public static Font Font
        {
            get
            {
                if (_fontResolved) return _font;
                _fontResolved = true;
                // Unity 6 ships the legacy dynamic font under this name; older editors used Arial.ttf.
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static void SpawnFloating(Vector3 worldPos, string text, Color color)
        {
            var host = FloatingTextRunner.Instance;
            if (host != null) host.Spawn(worldPos, text, color);
        }

        /// <summary>A tiny always-present MonoBehaviour so static callers can run coroutines.</summary>
        private sealed class FloatingTextRunner : MonoBehaviour
        {
            private static FloatingTextRunner _instance;

            public static FloatingTextRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("~FloatingText");
                        Object.DontDestroyOnLoad(go);
                        _instance = go.AddComponent<FloatingTextRunner>();
                    }
                    return _instance;
                }
            }

            public void Spawn(Vector3 worldPos, string text, Color color)
            {
                var font = Font;
                if (font == null) return;

                var go = new GameObject("dmg");
                go.transform.position = worldPos;
                var tm = go.AddComponent<TextMesh>();
                tm.text = text;
                tm.characterSize = 0.14f;
                tm.fontSize = 64;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = color;
                tm.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) { mr.sharedMaterial = font.material; mr.sortingOrder = 20; }

                StartCoroutine(Rise(go.transform, tm));
            }

            private IEnumerator Rise(Transform t, TextMesh tm)
            {
                float life = 0.6f;
                float elapsed = 0f;
                Vector3 start = t.position;
                Color c0 = tm.color;
                while (elapsed < life && t != null)
                {
                    elapsed += Time.deltaTime;
                    float k = elapsed / life;
                    t.position = start + Vector3.up * (0.6f * k);
                    tm.color = new Color(c0.r, c0.g, c0.b, 1f - k);
                    yield return null;
                }
                if (t != null) Object.Destroy(t.gameObject);
            }
        }
    }
}
