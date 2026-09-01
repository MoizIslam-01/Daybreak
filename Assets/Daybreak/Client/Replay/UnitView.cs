using System.Collections;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// One unit's on-screen presence during a replay. Built entirely in code — a colored body
    /// square, a tag pip, a HP bar, and a name label. It owns no game logic; the ReplayPlayer
    /// tells it when to lunge, take a hit, or die.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        private SpriteRenderer _body;
        private Transform _hpFill;
        private SpriteRenderer _hpFillRenderer;
        private TextMesh _nameLabel;

        private Vector3 _home;
        private Vector3 _towardEnemy;
        private int _maxHp;
        private int _curHp;

        private const float HpBarWidth = 1.0f;

        public int CurHp => _curHp;
        public bool Dead => _curHp <= 0;

        public static UnitView Create(Transform parent, UnitDef def, Vector3 home, bool facingRight, int maxHp)
        {
            var go = new GameObject("Unit_" + def.Id);
            go.transform.SetParent(parent, false);
            go.transform.position = home;

            var view = go.AddComponent<UnitView>();
            view.Build(def, home, facingRight, maxHp);
            return view;
        }

        private void Build(UnitDef def, Vector3 home, bool facingRight, int maxHp)
        {
            _home = home;
            _towardEnemy = facingRight ? Vector3.right : Vector3.left;
            _maxHp = Mathf.Max(1, maxHp);
            _curHp = _maxHp;

            // Body.
            _body = NewSprite("Body", transform, ReplayArt.Square(), Vector3.zero, new Vector3(1.1f, 1.1f, 1f), 0);
            _body.color = ReplayArt.ArchetypeColor(def.Arch);

            // Tag pip, top-left corner.
            var pip = NewSprite("Tag", transform, ReplayArt.Square(),
                new Vector3(-0.38f, 0.38f, 0f), new Vector3(0.32f, 0.32f, 1f), 1);
            pip.color = ReplayArt.TagColor(def.Tag);

            // HP bar: dark background + colored fill (left-pivot so it shrinks toward the left).
            NewSprite("HpBg", transform, ReplayArt.LeftSquare(),
                new Vector3(-HpBarWidth / 2f, 0.72f, 0f), new Vector3(HpBarWidth, 0.14f, 1f), 1)
                .color = new Color(0.15f, 0.05f, 0.05f, 0.9f);

            _hpFillRenderer = NewSprite("HpFill", transform, ReplayArt.LeftSquare(),
                new Vector3(-HpBarWidth / 2f, 0.72f, 0f), new Vector3(HpBarWidth, 0.14f, 1f), 2);
            _hpFill = _hpFillRenderer.transform;
            RefreshHpBar();

            _nameLabel = MakeLabel(def.Name, new Vector3(0f, -0.62f, 0f), 0.07f);
        }

        private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite,
            Vector3 localPos, Vector3 localScale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private TextMesh MakeLabel(string text, Vector3 localPos, float charSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = charSize;
            tm.fontSize = 64;
            tm.color = Color.white;
            var font = ReplayText.Font;
            if (font != null) tm.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = 3;
            return tm;
        }

        private void RefreshHpBar()
        {
            float frac = _maxHp <= 0 ? 0f : Mathf.Clamp01((float)_curHp / _maxHp);
            var s = _hpFill.localScale;
            s.x = HpBarWidth * frac;
            _hpFill.localScale = s;
            _hpFillRenderer.color = frac > 0.5f ? new Color(0.35f, 0.8f, 0.35f)
                                  : frac > 0.25f ? new Color(0.9f, 0.8f, 0.25f)
                                  : new Color(0.9f, 0.3f, 0.25f);
        }

        // ---- animations the ReplayPlayer drives ----

        public IEnumerator Lunge(float duration)
        {
            Vector3 out1 = _home + _towardEnemy * 0.7f;
            yield return MoveOverTime(_home, out1, duration * 0.5f);
            yield return MoveOverTime(out1, _home, duration * 0.5f);
        }

        public IEnumerator TakeHit(int damage, Color flashColor, float duration)
        {
            _curHp = Mathf.Max(0, _curHp - damage);
            RefreshHpBar();
            ReplayText.SpawnFloating(transform.position + Vector3.up * 0.3f, damage.ToString(), flashColor);

            var baseColor = _body.color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _body.color = Color.Lerp(Color.white, baseColor, t / duration);
                yield return null;
            }
            _body.color = baseColor;
        }

        public void Die()
        {
            var c = _body.color;
            _body.color = new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 0.25f);
            if (_hpFillRenderer != null) _hpFillRenderer.enabled = false;
            transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            if (_nameLabel != null) _nameLabel.color = new Color(1f, 1f, 1f, 0.3f);
        }

        private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f) { transform.position = to; yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            transform.position = to;
        }
    }
}
