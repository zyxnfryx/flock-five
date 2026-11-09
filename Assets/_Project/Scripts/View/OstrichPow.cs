using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Build 121. The fifth bird of a flurry is the bat. A gold "POW" and a warm flash,
    // playful and all-ages: no blood, no red damage mark. The word pops and fades on
    // its own so the fight does not have to babysit it.
    public static class OstrichPow
    {
        public const string Word = "POW";
        public const float Life = 0.52f;
        public const int Order = 64;

        public static float Alpha(float t)
        {
            if (t <= 0f || t >= Life) return 0f;
            float u = t / Life;
            float inn = Mathf.Clamp01(u / 0.14f);
            float outt = Mathf.Clamp01((1f - u) / 0.40f);
            return inn * outt;
        }

        public static float Scale(float t)
        {
            if (t <= 0f) return 0.35f;
            float u = Mathf.Clamp01(t / Life);
            if (u < 0.18f)
            {
                float pop = Mathf.Sin(u / 0.18f * Mathf.PI * 0.5f);
                return Mathf.Lerp(0.35f, 1.16f, pop);
            }
            float settle = Mathf.Clamp01((u - 0.18f) / 0.82f);
            return Mathf.Lerp(1.16f, 0.88f, settle);
        }

        public static GameObject Spawn(Vector3 pos, Transform parent)
        {
            var root = new GameObject("OstrichPow");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var flashGo = WorldBuilder.Sprite("PowFlash", SpriteCatalog.Glow, pos, 0.85f, Order - 1, root.transform);
            var flash = flashGo != null ? flashGo.GetComponent<SpriteRenderer>() : null;
            if (flash != null) flash.color = new Color(1f, 0.90f, 0.42f, 0.92f);

            var wordGo = new GameObject("PowWord");
            wordGo.transform.SetParent(root.transform, false);
            var tm = wordGo.AddComponent<TextMesh>();
            tm.text = Word;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = 0.42f;
            tm.color = new Color(1f, 0.93f, 0.35f, 1f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) tm.font = font;
            var mr = wordGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = Order;
                if (font != null && font.material != null) mr.sharedMaterial = font.material;
            }
            var life = root.AddComponent<OstrichPowLife>();
            life.Arm(flash, tm);
            return root;
        }
    }

    public sealed class OstrichPowLife : MonoBehaviour
    {
        SpriteRenderer _flash;
        TextMesh _word;
        float _t;

        public void Arm(SpriteRenderer flash, TextMesh word)
        {
            _flash = flash;
            _word = word;
            Apply(0f);
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            _t += Time.deltaTime;
            if (_t >= OstrichPow.Life)
            {
                Destroy(gameObject);
                return;
            }
            Apply(_t);
        }

        void Apply(float t)
        {
            float a = OstrichPow.Alpha(t);
            float s = OstrichPow.Scale(t);
            if (_word != null)
            {
                var c = _word.color;
                c.a = a;
                _word.color = c;
                _word.transform.localScale = new Vector3(s, s, 1f);
            }
            if (_flash != null)
            {
                var c = _flash.color;
                c.a = a * 0.9f;
                _flash.color = c;
                _flash.transform.localScale = Vector3.one * (0.85f * s);
            }
        }
    }
}
