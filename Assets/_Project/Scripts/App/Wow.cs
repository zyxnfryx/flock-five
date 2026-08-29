using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public static class Wow
    {
        public static Color Of(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby: return new Color(1f, 0.28f, 0.32f);
                case BirdColor.Gold: return new Color(1f, 0.82f, 0.22f);
                case BirdColor.Teal: return new Color(0.15f, 0.9f, 0.78f);
                default: return new Color(0.72f, 0.38f, 1f);
            }
        }

        public static IEnumerator Burst(Vector3 pos, BirdColor col, Transform parent, int combo = 1)
        {
            var tint = Of(col);
            int n = 14 + 6 * Mathf.Clamp(combo - 1, 0, 3);
            var bits = new SpriteRenderer[n];
            var vel = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float spd = Random.Range(2.4f, 4.6f) * (1f + 0.18f * (combo - 1));
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.35f, 0f) * spd;
                bool star = i % 2 == 0;
                var go = WorldBuilder.Sprite("Wow" + i, star ? SpriteCatalog.Sparkle : SpriteCatalog.Glow, pos, star ? 0.16f : 0.28f, 18, parent);
                bits[i] = go.GetComponent<SpriteRenderer>();
                bits[i].color = star ? Color.white : new Color(tint.r, tint.g, tint.b, 0.9f);
            }
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                float u = t / 0.7f;
                for (int i = 0; i < n; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].transform.position += vel[i] * Time.deltaTime;
                    vel[i] *= 0.92f;
                    var c = bits[i].color;
                    c.a = (1f - u) * (1f - u);
                    bits[i].color = c;
                    float s = bits[i].transform.localScale.x;
                    bits[i].transform.localScale = Vector3.one * (s * (1f + Time.deltaTime * 0.8f));
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (bits[i] != null) Object.Destroy(bits[i].gameObject);
        }
    }
}
