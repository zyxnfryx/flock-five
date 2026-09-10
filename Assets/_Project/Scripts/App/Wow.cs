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
                case BirdColor.Peach: return new Color(1f, 0.52f, 0.42f);
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

        public static IEnumerator Shed(Vector3 pos, BirdColor col, Transform parent)
        {
            if (parent == null) yield break;
            var tint = Of(col);
            const int n = 5;
            var bits = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var spin = new float[n];
            var phase = new float[n];
            var size = new float[n];
            var spr = SpriteCatalog.Feather;
            for (int fi = 0; fi < n; fi++)
            {
                float side = Random.Range(-1.15f, 1.15f);
                vel[fi] = new Vector3(side * Random.Range(0.7f, 1.8f), Random.Range(1.4f, 2.8f), 0f);
                spin[fi] = Random.Range(-280f, 280f);
                phase[fi] = Random.Range(0f, 6.3f);
                size[fi] = Random.Range(0.11f, 0.18f);
                var go = WorldBuilder.Sprite("Feather", spr, pos, size[fi], 41, parent);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-50f, 50f));
                bits[fi] = go.GetComponent<SpriteRenderer>();
                bits[fi].color = new Color(tint.r, tint.g, tint.b, 0.95f);
            }
            float life = 0.92f;
            float shedT = 0f;
            while (shedT < life)
            {
                shedT += Time.deltaTime;
                float u = shedT / life;
                for (int fi = 0; fi < n; fi++)
                {
                    if (bits[fi] == null) continue;
                    vel[fi].y -= 5.6f * Time.deltaTime;
                    vel[fi].x += Mathf.Sin(shedT * 7f + phase[fi]) * 1.1f * Time.deltaTime;
                    bits[fi].transform.position += vel[fi] * Time.deltaTime;
                    bits[fi].transform.Rotate(0f, 0f, spin[fi] * Time.deltaTime);
                    var c = bits[fi].color;
                    c.a = 0.95f * (1f - u) * (1f - u * 0.35f);
                    bits[fi].color = c;
                }
                yield return null;
            }
            for (int fi = 0; fi < n; fi++)
                if (bits[fi] != null) Object.Destroy(bits[fi].gameObject);
        }

        public static IEnumerator SlamCombo(Transform parent, int combo)
        {
            if (parent == null || combo < 2) yield break;
            combo = Mathf.Clamp(combo, 2, Palette.ComboMax);
            var hold = new GameObject("ComboHit").transform;
            hold.SetParent(parent, false);
            hold.position = new Vector3(0f, 2.22f, 0f);
            float grow = 1f + 0.042f * Mathf.Min(combo - 2, 8);

            var glowGo = WorldBuilder.Sprite("ComboGlow", SpriteCatalog.Glow, hold.position, 1f, 54, hold);
            glowGo.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            glowGo.transform.localScale = new Vector3(6.8f * grow, 3.1f * grow, 1f);
            var glowSr = glowGo.GetComponent<SpriteRenderer>();
            glowSr.color = new Color(1f, 0.92f, 0.48f, 0f);

            const string word = "COMBO";
            string mul = "x" + combo;
            int n = word.Length + mul.Length;
            var letters = new Transform[n];
            var rest = new Vector3[n];
            var restY = new float[n];
            var restZ = new float[n];
            var delay = new float[n];
            var drop = new float[n];
            var fromY = new float[n];
            var srs = new SpriteRenderer[n];
            bool mulHit = false;

            float gap = 0.80f * grow;
            float x0 = -0.5f * (word.Length - 1) * gap;
            float[] wordTilt = { -3.6f, -1.4f, 0.6f, 2.0f, -2.4f };
            const float wordY = 0.30f;
            const float dropWord = 0.13f;
            for (int wi = 0; wi < word.Length; wi++)
            {
                var wordSpr = SpriteCatalog.Letter(word[wi]);
                var wordGo = WorldBuilder.Sprite("Hit" + word[wi], wordSpr, hold.position, 1f, 56, hold);
                float wordH = wordSpr != null ? Mathf.Max(0.01f, wordSpr.bounds.size.y) : 1f;
                float wordS = 1.22f * grow / wordH;
                rest[wi] = new Vector3(wordS, wordS, 1f);
                restY[wi] = wordY;
                restZ[wi] = wordTilt[wi];
                delay[wi] = wi * 0.016f;
                drop[wi] = dropWord;
                fromY[wi] = wordY + 1.28f;
                wordGo.transform.localPosition = new Vector3(x0 + wi * gap, fromY[wi], 0f);
                wordGo.transform.localRotation = Quaternion.Euler(0f, 0f, restZ[wi] * 2.4f);
                wordGo.transform.localScale = rest[wi] * 0.70f;
                letters[wi] = wordGo.transform;
                srs[wi] = wordGo.GetComponent<SpriteRenderer>();
            }

            float mgap = 0.68f * grow;
            float mx0 = -0.5f * (mul.Length - 1) * mgap;
            float mulStart = 0.20f;
            const float mulY = -0.78f;
            const float dropMul = 0.15f;
            for (int mi = 0; mi < mul.Length; mi++)
            {
                int mk = word.Length + mi;
                char ch = mul[mi];
                var mulSpr = SpriteCatalog.Glyph(ch);
                var mulGo = WorldBuilder.Sprite("HitMul" + ch, mulSpr, hold.position, 1f, 57, hold);
                float mulH = mulSpr != null ? Mathf.Max(0.01f, mulSpr.bounds.size.y) : 1f;
                float mulS = (ch == 'x' || ch == 'X' ? 0.68f : 1.00f) * grow / mulH;
                rest[mk] = new Vector3(mulS, mulS, 1f);
                restY[mk] = mulY;
                restZ[mk] = (mi == 0 ? -7.5f : (mul.Length == 2 ? 5.5f : (mi == 1 ? 0f : 6.5f)));
                delay[mk] = mulStart + mi * 0.024f;
                drop[mk] = dropMul;
                fromY[mk] = mulY + 1.55f;
                mulGo.transform.localPosition = new Vector3(mx0 + mi * mgap, fromY[mk], 0f);
                mulGo.transform.localRotation = Quaternion.Euler(0f, 0f, restZ[mk] * 1.8f);
                mulGo.transform.localScale = rest[mk] * 0.62f;
                letters[mk] = mulGo.transform;
                srs[mk] = mulGo.GetComponent<SpriteRenderer>();
            }

            float popT = 0f;
            float popEnd = delay[n - 1] + drop[n - 1] + 0.10f;
            while (popT < popEnd)
            {
                popT += Time.deltaTime;
                for (int li = 0; li < n; li++)
                {
                    if (letters[li] == null) continue;
                    float fallU = Mathf.Clamp01((popT - delay[li]) / drop[li]);
                    float fall = fallU * fallU * fallU;
                    float y = Mathf.Lerp(fromY[li], restY[li], fall);
                    float squash = 1f;
                    float stretch = 1f;
                    if (fallU > 0.78f)
                    {
                        float sq = (fallU - 0.78f) / 0.22f;
                        float bump = Mathf.Sin(sq * Mathf.PI);
                        squash = 1f - 0.20f * bump;
                        stretch = 1f + 0.18f * bump;
                    }
                    var lp = letters[li].localPosition;
                    lp.y = y;
                    letters[li].localPosition = lp;
                    letters[li].localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(restZ[li] * 2.4f, restZ[li], fall));
                    float appear = 0.70f + 0.30f * fallU;
                    letters[li].localScale = new Vector3(rest[li].x * stretch * appear, rest[li].y * squash * appear, 1f);
                    if (srs[li] != null)
                    {
                        float flash = fallU > 0.82f && fallU < 0.96f ? 1f : 0f;
                        srs[li].color = Color.Lerp(Color.white, new Color(1f, 0.96f, 0.78f), flash);
                    }
                }
                if (!mulHit && popT >= delay[word.Length] + drop[word.Length] * 0.82f)
                {
                    mulHit = true;
                    if (CamShake.Live != null)
                        CamShake.Live.Punch(0.11f, 0.07f, 2.2f, 0.08f);
                }
                if (glowSr != null)
                {
                    float wordLand = Mathf.Clamp01((popT - 0.10f) / 0.08f);
                    float mulLand = Mathf.Clamp01((popT - (mulStart + dropMul * 0.8f)) / 0.08f);
                    float g = 0.18f + 0.32f * wordLand * (1f - 0.45f * mulLand) + 0.28f * mulLand;
                    glowSr.color = new Color(1f, 0.92f, 0.48f, g);
                    glowGo.transform.localScale = new Vector3((6.8f + 0.8f * mulLand) * grow, (3.1f + 0.4f * mulLand) * grow, 1f);
                }
                float punch = 1f;
                if (popT > 0.12f && popT < 0.28f)
                    punch = 1f + 0.06f * Mathf.Sin((popT - 0.12f) / 0.16f * Mathf.PI);
                hold.localScale = Vector3.one * punch;
                yield return null;
            }
            for (int pi = 0; pi < n; pi++)
            {
                if (letters[pi] == null) continue;
                letters[pi].localPosition = new Vector3(letters[pi].localPosition.x, restY[pi], 0f);
                letters[pi].localRotation = Quaternion.Euler(0f, 0f, restZ[pi]);
                letters[pi].localScale = rest[pi];
                if (srs[pi] != null) srs[pi].color = Color.white;
            }
            hold.localScale = Vector3.one;

            float holdDur = 0.42f + 0.035f * Mathf.Min(combo, 10);
            float hT = 0f;
            while (hT < holdDur && hold != null)
            {
                hT += Time.deltaTime;
                float settle = 1f + 0.012f * Mathf.Exp(-hT * 8f) * Mathf.Sin(hT * 22f);
                hold.localScale = Vector3.one * settle;
                if (glowSr != null)
                    glowSr.color = new Color(1f, 0.92f, 0.48f, 0.26f * (1f - 0.35f * hT / holdDur));
                yield return null;
            }

            var fadeRs = hold.GetComponentsInChildren<SpriteRenderer>();
            var fadeA = new float[fadeRs.Length];
            for (int fi = 0; fi < fadeRs.Length; fi++)
                fadeA[fi] = fadeRs[fi] != null ? fadeRs[fi].color.a : 0f;
            float fadeT = 0f;
            const float fade = 0.20f;
            while (fadeT < fade && hold != null)
            {
                fadeT += Time.deltaTime;
                float fadeU = Mathf.Clamp01(fadeT / fade);
                hold.localScale = Vector3.one * (1f + 0.10f * fadeU);
                for (int fj = 0; fj < fadeRs.Length; fj++)
                {
                    if (fadeRs[fj] == null) continue;
                    var fc = fadeRs[fj].color;
                    fc.a = fadeA[fj] * (1f - fadeU);
                    fadeRs[fj].color = fc;
                }
                yield return null;
            }
            if (hold != null) Object.Destroy(hold.gameObject);
        }

        static readonly BirdColor[] Parade =
        {
            BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach
        };

        public static IEnumerator FlockOver(Transform parent, int combo)
        {
            if (parent == null || combo < 2) yield break;
            int n = Mathf.Clamp(combo, 2, Palette.ComboMax);
            bool upRight = combo % 2 == 0;
            // Start well past the portrait frustum (~±6 world x) so fanfare birds
            // never peek in as a stray same-color bird at the screen edge.
            var a = upRight ? new Vector3(-11.5f, -4.2f, 0f) : new Vector3(-11.5f, 8.0f, 0f);
            var b = upRight ? new Vector3(11.5f, 8.0f, 0f) : new Vector3(11.5f, -4.2f, 0f);
            var dir = (b - a).normalized;
            var perp = new Vector3(-dir.y, dir.x, 0f);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var gos = new GameObject[n];
            var srs = new SpriteRenderer[n];
            var col = new BirdColor[n];
            var off = new Vector3[n];
            const int trails = 3;
            var lines = new SpriteRenderer[n * trails];
            for (int i = 0; i < n; i++)
            {
                int rank = i == 0 ? 0 : (i + 1) / 2;
                float side = i == 0 ? 0f : (i % 2 == 0 ? -1f : 1f);
                float lane = 0.92f + 0.14f * Mathf.Min(n - 2, 6);
                float trail = 0.46f + 0.08f * Mathf.Min(n - 2, 6);
                off[i] = perp * (side * rank * lane) - dir * (rank * trail);
                col[i] = Parade[i % Parade.Length];
                var go = WorldBuilder.Sprite("ComboBird" + i, SpriteCatalog.Bird(col[i]), a, 1f, 52, parent);
                go.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                go.transform.localScale = new Vector3(0.46f, 0.30f, 1f);
                srs[i] = go.GetComponent<SpriteRenderer>();
                srs[i].flipX = false;
                srs[i].sortingOrder = 52;
                srs[i].enabled = false;
                gos[i] = go;
                var tint = Of(col[i]);
                for (int k = 0; k < trails; k++)
                {
                    var lg = WorldBuilder.Sprite("Speed" + i + k, SpriteCatalog.Glow, a, 1f, 50, parent);
                    lg.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                    var lr = lg.GetComponent<SpriteRenderer>();
                    lr.color = new Color(tint.r, tint.g, tint.b, 0f);
                    lr.sortingOrder = 50;
                    lines[i * trails + k] = lr;
                }
            }

            int streaks = 8 + Mathf.Min(n, 6);
            var dash = new SpriteRenderer[streaks];
            var dashU = new float[streaks];
            var dashP = new Vector3[streaks];
            var dashL = new float[streaks];
            for (int s = 0; s < streaks; s++)
            {
                float u = Random.Range(0.08f, 0.92f);
                dashP[s] = Vector3.Lerp(a, b, u) + perp * Random.Range(-2.4f, 2.4f);
                dashL[s] = Random.Range(1.1f, 2.4f);
                dashU[s] = Random.Range(0.12f, 0.78f);
                var dg = WorldBuilder.Sprite("Dash" + s, SpriteCatalog.Glow, dashP[s], 1f, 46, parent);
                dg.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                dg.transform.localScale = new Vector3(dashL[s], 0.045f, 1f);
                dash[s] = dg.GetComponent<SpriteRenderer>();
                dash[s].color = new Color(1f, 0.96f, 0.82f, 0f);
                dash[s].sortingOrder = 46;
            }

            float dur = 0.70f + 0.02f * Mathf.Min(n, 8);
            float t = 0f;
            float stagger = 0.078f;
            float span = dur + n * stagger;
            while (t < span)
            {
                t += Time.deltaTime;
                float pulse = Mathf.Clamp01(t / dur);
                for (int s = 0; s < streaks; s++)
                {
                    if (dash[s] == null) continue;
                    float w = 1f - Mathf.Abs(pulse - dashU[s]) * 3.4f;
                    w = Mathf.Clamp01(w) * 0.42f * Mathf.Sin(Mathf.PI * pulse);
                    var c = dash[s].color;
                    c.a = w;
                    dash[s].color = c;
                }
                for (int i = 0; i < n; i++)
                {
                    if (srs[i] == null) continue;
                    float u = Mathf.Clamp01((t - i * stagger) / dur);
                    float ease = u * u * (3f - 2f * u);
                    var pos = Vector3.Lerp(a, b, ease) + off[i];
                    // Hide until inside the playfield so an off-screen spawn cannot
                    // read as an unrelated flock bird during collect.
                    bool on = u > 0.02f && u < 0.98f && Mathf.Abs(pos.x) < 6.1f;
                    srs[i].enabled = on;
                    if (!on)
                    {
                        for (int k = 0; k < trails; k++)
                            if (lines[i * trails + k] != null)
                            {
                                var lc = lines[i * trails + k].color;
                                lc.a = 0f;
                                lines[i * trails + k].color = lc;
                            }
                        continue;
                    }
                    gos[i].transform.position = pos;
                    gos[i].transform.rotation = Quaternion.Euler(0f, 0f, ang);
                    gos[i].transform.localScale = new Vector3(0.50f, 0.28f, 1f);
                    srs[i].sprite = SpriteCatalog.BirdFrame(col[i], t * 22f, true);
                    var tint = Of(col[i]);
                    for (int k = 0; k < trails; k++)
                    {
                        var lr = lines[i * trails + k];
                        if (lr == null) continue;
                        float back = 0.32f + k * 0.38f;
                        lr.transform.position = pos - dir * back;
                        lr.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                        lr.transform.localScale = new Vector3(0.85f + k * 0.55f, 0.055f + k * 0.012f, 1f);
                        float ta = (1f - u) * (0.55f - k * 0.12f) * Mathf.Sin(Mathf.PI * u);
                        lr.color = new Color(Mathf.Lerp(1f, tint.r, 0.45f), Mathf.Lerp(0.97f, tint.g, 0.45f), Mathf.Lerp(0.88f, tint.b, 0.45f), Mathf.Max(0f, ta));
                    }
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
            {
                if (gos[i] != null) Object.Destroy(gos[i]);
                for (int k = 0; k < trails; k++)
                    if (lines[i * trails + k] != null) Object.Destroy(lines[i * trails + k].gameObject);
            }
            for (int s = 0; s < streaks; s++)
                if (dash[s] != null) Object.Destroy(dash[s].gameObject);
        }

        public static IEnumerator SkyBurst(Transform parent, Vector3 pos, BirdColor col, bool bang, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (parent == null) yield break;
            if (bang) Sfx.Firework();
            var tint = Of(col);
            const int bits = 14;
            var rs = new SpriteRenderer[bits];
            var vel = new Vector3[bits];
            for (int i = 0; i < bits; i++)
            {
                float ang = (i / (float)bits) * Mathf.PI * 2f + Random.Range(-0.14f, 0.14f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * Random.Range(2.2f, 5.2f);
                bool star = i % 3 != 0;
                var go = WorldBuilder.Sprite("ComboBoom", star ? SpriteCatalog.Sparkle : SpriteCatalog.Glow, pos,
                    star ? 0.13f : 0.24f, 48, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                rs[i].color = star ? Color.Lerp(Color.white, tint, 0.32f) : new Color(tint.r, tint.g, tint.b, 0.92f);
            }
            float b = 0f;
            const float life = 0.72f;
            while (b < life)
            {
                b += Time.deltaTime;
                float u = b / life;
                for (int i = 0; i < bits; i++)
                {
                    if (rs[i] == null) continue;
                    vel[i].y -= 5.4f * Time.deltaTime;
                    rs[i].transform.position += vel[i] * Time.deltaTime;
                    var c = rs[i].color;
                    c.a = (1f - u) * (1f - u);
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < bits; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }
    }
}
