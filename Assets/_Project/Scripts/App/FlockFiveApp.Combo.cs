using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Combo wordmark. Slots are the sprite bounds (baked navy shadow included)
    // plus a small positive gap from the two letter widths. The gap stays
    // above zero so wide letters (M, B) and two-digit counts cannot overlap.
    public sealed partial class FlockFiveApp
    {
        struct ComboGlyph
        {
            public Transform Xf;
            public SpriteRenderer Face;
            public float X, Y, W, H, Tilt, Delay;
            public Vector3 Rest;
            public Color Ink;
        }

        sealed class ComboLane
        {
            public float Min, Max;
            public bool Live;
        }

        const float ComboPopPeak = 1.085f;
        // Side bearing, as a fraction of each glyph's width. Positive tracking only.
        const float ComboLetterGap = 0.04f;
        static readonly List<ComboLane> ComboLanes = new List<ComboLane>(4);
        Rect _comboStreakGui;
        float _comboStreakUntil;

        void NoteComboStreak(Rect gui)
        {
            _comboStreakGui = gui;
            _comboStreakUntil = Time.unscaledTime + 0.25f;
        }

        // Ease-out back. Starts near half size, overshoots a little, settles at 1.
        static float ComboPop(float u)
        {
            u = Mathf.Clamp01(u);
            float s = Mathf.Lerp(0.14f, 1f, u);
            const float c1 = 1.55f;
            const float c3 = c1 + 1f;
            float t = s - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        static Color ComboTier(int combo)
        {
            float h = Mathf.Clamp01((combo - 2) / 10f);
            var cream = new Color(1f, 0.95f, 0.72f);
            var amber = new Color(1f, 0.72f, 0.26f);
            var hot = new Color(1f, 0.38f, 0.16f);
            if (h < 0.5f) return Color.Lerp(cream, amber, h * 2f);
            return Color.Lerp(amber, hot, (h - 0.5f) * 2f);
        }

        static float ComboTilt(bool word, int i)
        {
            if (word)
            {
                switch (i)
                {
                    case 0: return -2.6f;
                    case 1: return -1.0f;
                    case 2: return 0.7f;
                    case 3: return 1.6f;
                    default: return -2.0f;
                }
            }
            if (i == 0) return -3.4f;
            return (i & 1) == 0 ? -1.8f : 2.2f;
        }

        static void ComboHalf(float w, float h, float deg, out float hx, out float hy)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(r));
            float s = Mathf.Abs(Mathf.Sin(r));
            hx = w * 0.5f * c + h * 0.5f * s;
            hy = w * 0.5f * s + h * 0.5f * c;
        }

        // Gap between two glyph boxes. Each side contributes ComboLetterGap of its own width.
        static float ComboAir(float leftW, float rightW)
        {
            return (leftW + rightW) * (ComboLetterGap * 0.5f);
        }

        static bool ComboGlyphSize(char ch, float cap, out float w, out float h, out float scale, out Sprite spr)
        {
            spr = SpriteCatalog.Glyph(ch);
            w = h = scale = 0f;
            if (spr == null) return false;
            float bh = Mathf.Max(0.01f, spr.bounds.size.y);
            float bw = Mathf.Max(0.01f, spr.bounds.size.x);
            float use = cap;
            if (ch == 'x' || ch == 'X') use *= 0.78f;
            scale = use / bh;
            w = bw * scale;
            h = bh * scale;
            return true;
        }

        int ComboLayRow(string text, bool word, float cap, float y, float delay0, float step,
            ComboGlyph[] dst, int n, Color ink)
        {
            int count = text.Length;
            var w = new float[count];
            var h = new float[count];
            var sc = new float[count];
            var spr = new Sprite[count];
            var tilt = new float[count];
            var hx = new float[count];
            var gap = new float[count];
            var ok = new bool[count];
            float span = 0f;
            int live = 0;
            float prevW = 0f;
            for (int i = 0; i < count; i++)
            {
                tilt[i] = ComboTilt(word, i);
                ok[i] = ComboGlyphSize(text[i], cap, out w[i], out h[i], out sc[i], out spr[i]);
                if (!ok[i]) continue;
                ComboHalf(w[i], h[i], tilt[i], out hx[i], out _);
                if (live > 0)
                {
                    gap[i] = ComboAir(prevW, w[i]);
                    span += gap[i];
                }
                span += hx[i] * 2f;
                prevW = w[i];
                live++;
            }
            if (live == 0) return n;
            float cursor = -span * 0.5f;
            bool placed = false;
            for (int i = 0; i < count; i++)
            {
                if (!ok[i]) continue;
                if (placed) cursor += gap[i];
                float x = cursor + hx[i];
                cursor += hx[i] * 2f;
                placed = true;
                var go = WorldBuilder.Sprite(word ? "Hit" + text[i] : "HitMul" + text[i], spr[i], Vector3.zero, 1f, 57, null);
                var g = new ComboGlyph
                {
                    Xf = go.transform,
                    Face = go.GetComponent<SpriteRenderer>(),
                    X = x,
                    Y = y,
                    W = w[i],
                    H = h[i],
                    Tilt = tilt[i],
                    Delay = delay0 + i * step,
                    Rest = new Vector3(sc[i], sc[i], 1f),
                    Ink = ink
                };
                g.Xf.localScale = g.Rest;
                if (g.Face != null) g.Face.color = ink;
                dst[n++] = g;
            }
            return n;
        }

        static void ComboBounds(ComboGlyph[] g, int n, float drop, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = minY = 1e9f;
            maxX = maxY = -1e9f;
            for (int i = 0; i < n; i++)
            {
                ComboHalf(g[i].W, g[i].H, g[i].Tilt, out float hx, out float hy);
                float x0 = g[i].X - hx;
                float x1 = g[i].X + hx;
                float y0 = g[i].Y - hy;
                float y1 = g[i].Y + hy;
                if (x0 < minX) minX = x0;
                if (x1 > maxX) maxX = x1;
                if (y0 < minY) minY = y0;
                if (y1 > maxY) maxY = y1;
            }
            maxY += drop;
        }

        bool ComboCam(out Camera cam, out float camX, out float camY, out float halfW, out float halfH)
        {
            CamPlane(out camX, out camY, out halfW, out float h);
            halfH = h;
            cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            return cam != null;
        }

        float ComboWorldY(Camera cam, float camY, float halfH, float guiFromTop)
        {
            if (cam != null)
            {
                float dist = Mathf.Max(0.01f, Mathf.Abs(cam.transform.position.z));
                float sy = Mathf.Max(0f, Screen.height - guiFromTop);
                return cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, sy, dist)).y;
            }
            float t = Screen.height > 1f ? guiFromTop / Screen.height : 0f;
            return camY + halfH - t * halfH * 2f;
        }

        float ComboWorldX(Camera cam, float camX, float halfW, float screenX)
        {
            if (cam != null)
            {
                float dist = Mathf.Max(0.01f, Mathf.Abs(cam.transform.position.z));
                return cam.ScreenToWorldPoint(new Vector3(screenX, Screen.height * 0.5f, dist)).x;
            }
            float t = Screen.width > 1f ? screenX / Screen.width : 0.5f;
            return camX - halfW + t * halfW * 2f;
        }

        float ComboFeederBottom(Transform root)
        {
            float y = float.PositiveInfinity;
            bool found = false;
            if (root != null)
            {
                var feeders = root.GetComponentsInChildren<FeederView>(true);
                for (int i = 0; i < feeders.Length; i++)
                {
                    var art = feeders[i] != null ? feeders[i].Art : null;
                    if (art == null || art.sprite == null || !art.enabled) continue;
                    float bot = art.bounds.min.y;
                    if (!found || bot < y) y = bot;
                    found = true;
                }
            }
            if (!found) return WorldBuilder.FeederY - 1.85f;
            return y;
        }

        void ComboSafe(Transform root, out float x0, out float x1, out float y0, out float y1)
        {
            ComboCam(out var cam, out float camX, out float camY, out float halfW, out float halfH);
            var safe = Screen.safeArea;
            bool ok = safe.width > 2f && safe.height > 2f;
            float left = ok ? safe.xMin : 0f;
            float right = ok ? safe.xMax : Screen.width;
            x0 = ComboWorldX(cam, camX, halfW, left) + 0.16f;
            x1 = ComboWorldX(cam, camX, halfW, right) - 0.16f;
            if (x1 - x0 < 1.2f)
            {
                x0 = camX - halfW + 0.2f;
                x1 = camX + halfW - 0.2f;
            }

            float s = Mathf.Max(Screen.height / 720f, 1f);
            HudLayout(out _, out float top, out _, out var restart, out _);
            float yTop = ComboWorldY(cam, camY, halfH, top);
            yTop = Mathf.Min(yTop, ComboFeederBottom(root) - 0.28f);
            if (Time.unscaledTime < _comboStreakUntil)
                yTop = Mathf.Min(yTop, ComboWorldY(cam, camY, halfH, _comboStreakGui.yMax) - 0.2f);
            if (_coachFade > 0.2f && (_coach || _adHand || _cueHand || _pestCue != 0))
            {
                // Build 61: the painted plate when one is up (a pest intro is three lines, not
                // the old 72s guess), so a wordmark that pops under a caption stays clear of it.
                float lineTop = _coachLineHeld ? _coachLineHold : top + 8f * s;
                float lineBot = lineTop + 72f * s;
                if (_tutorPlateOn && _tutorPlateFrame >= 0 && Time.frameCount - _tutorPlateFrame <= 2)
                    lineBot = Mathf.Max(lineBot, _tutorPlate.yMax + 4f * s);
                yTop = Mathf.Min(yTop, ComboWorldY(cam, camY, halfH, lineBot) - 0.16f);
            }
            float yBot = ComboWorldY(cam, camY, halfH, restart.y) + 0.2f;
            float viewBot = camY - halfH + 0.25f;
            float viewTop = camY + halfH - 0.25f;
            y0 = Mathf.Max(yBot, viewBot);
            y1 = Mathf.Min(yTop, viewTop);
            if (y1 < y0 + 1.4f)
            {
                float mid = (viewBot + viewTop) * 0.5f;
                y0 = mid - 1.6f;
                y1 = mid + 1.6f;
            }
        }

        bool ComboHits(float y, float half, float gap)
        {
            float a = y - half - gap;
            float b = y + half + gap;
            for (int i = 0; i < ComboLanes.Count; i++)
            {
                var lane = ComboLanes[i];
                if (lane == null || !lane.Live) continue;
                if (b > lane.Min && a < lane.Max) return true;
            }
            return false;
        }

        float ComboFree(float y0, float y1)
        {
            float best = 0f;
            float cursor = y0;
            int n = ComboLanes.Count;
            var lo = new float[n];
            var hi = new float[n];
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                var lane = ComboLanes[i];
                if (lane == null || !lane.Live) continue;
                float a = Mathf.Max(y0, lane.Min);
                float b = Mathf.Min(y1, lane.Max);
                if (b <= a) continue;
                lo[m] = a;
                hi[m] = b;
                m++;
            }
            for (int i = 1; i < m; i++)
            {
                float a = lo[i];
                float b = hi[i];
                int j = i;
                while (j > 0 && lo[j - 1] > a)
                {
                    lo[j] = lo[j - 1];
                    hi[j] = hi[j - 1];
                    j--;
                }
                lo[j] = a;
                hi[j] = b;
            }
            for (int i = 0; i < m; i++)
            {
                float gap = lo[i] - cursor;
                if (gap > best) best = gap;
                if (hi[i] > cursor) cursor = hi[i];
            }
            float tail = y1 - cursor;
            if (tail > best) best = tail;
            return best;
        }

        float ComboPlaceY(float prefer, float half, float y0, float y1)
        {
            const float gap = 0.26f;
            float lo = y0 + half;
            float hi = y1 - half;
            if (hi < lo) return (y0 + y1) * 0.5f;
            float want = Mathf.Clamp(prefer, lo, hi);
            if (!ComboHits(want, half, gap)) return want;
            float span = y1 - y0;
            for (float d = 0.08f; d <= span; d += 0.08f)
            {
                float down = want - d;
                if (down >= lo && !ComboHits(down, half, gap)) return down;
                float up = want + d;
                if (up <= hi && !ComboHits(up, half, gap)) return up;
            }
            return want;
        }

        IEnumerator PlayComboPop(Transform parent, int combo, bool celebrate = false)
        {
            if (parent == null || combo < 2)
            {
                if (celebrate) FinaleShow.StopLaunching();
                yield break;
            }
            combo = Mathf.Clamp(combo, 2, Palette.ComboMax);
            float tier = Mathf.InverseLerp(2f, Palette.ComboMax, combo);
            float cap = Mathf.Lerp(1.18f, 1.46f, tier);
            if (celebrate) cap *= 1.32f;
            float mulCap = cap * 0.90f;
            float vAir = 0.28f * cap;
            float drop = 0.12f * cap;
            var ink = ComboTier(combo);
            var face = Color.Lerp(Color.white, ink, Mathf.Lerp(0.06f, 0.48f, tier));

            float wordHalf = cap * 0.5f;
            float mulHalf = mulCap * 0.5f;
            float sep = wordHalf + mulHalf + vAir;
            var glyphs = new ComboGlyph[8];
            int n = 0;
            n = ComboLayRow("COMBO", true, cap, sep * 0.5f, 0f, 0.026f, glyphs, n, face);
            n = ComboLayRow("x" + combo, false, mulCap, -sep * 0.5f, 0.08f, 0.03f, glyphs, n, face);
            if (n == 0)
            {
                if (celebrate) FinaleShow.StopLaunching();
                yield break;
            }

            ComboBounds(glyphs, n, 0f, out float minX, out float maxX, out float minY, out float maxY);
            float shiftX = (minX + maxX) * 0.5f;
            float shiftY = (minY + maxY) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                glyphs[i].X -= shiftX;
                glyphs[i].Y -= shiftY;
            }
            ComboBounds(glyphs, n, drop, out minX, out maxX, out minY, out maxY);
            float localHalfW = (maxX - minX) * 0.5f + cap * 0.28f;
            float localHalfH = (maxY - minY) * 0.5f + cap * 0.22f;
            // Rest tilt swings the whole word. Reserve the axis-aligned box so the
            // aura stays off the feeder, the HUD, and the banner above or below.
            float tiltRad = 2.8f * Mathf.Deg2Rad;
            float tiltC = Mathf.Cos(tiltRad);
            float tiltS = Mathf.Sin(tiltRad);
            float boxW = localHalfW * tiltC + localHalfH * tiltS;
            float boxH = localHalfW * tiltS + localHalfH * tiltC;

            ComboSafe(parent, out float x0, out float x1, out float y0, out float y1);
            float fit = 1f;
            float needW = boxW * 2f * ComboPopPeak;
            float slotW = Mathf.Max(0.4f, x1 - x0);
            if (needW > slotW) fit = slotW / needW;
            float needH = boxH * 2f * ComboPopPeak;
            float freeH = ComboFree(y0, y1);
            if (needH * fit > freeH && freeH > 0.5f) fit *= freeH / (needH * fit);
            fit = Mathf.Clamp(fit, 0.62f, 1f);
            float worldHalfH = boxH * ComboPopPeak * fit;
            float worldHalfW = boxW * ComboPopPeak * fit;
            float y = ComboPlaceY(2.15f, worldHalfH, y0, y1);
            float left = x0 + worldHalfW;
            float right = x1 - worldHalfW;
            float x = right >= left ? Mathf.Clamp(0f, left, right) : (x0 + x1) * 0.5f;

            var lane = new ComboLane { Min = y - worldHalfH, Max = y + worldHalfH + 0.12f };

            var hold = new GameObject("ComboHit").transform;
            hold.SetParent(parent, false);
            hold.position = new Vector3(x, y, 0f);
            var basePos = hold.position;

            // Halo stays inside the reserved box (padding above is larger than this).
            float glowW = (maxX - minX) + cap * 0.42f;
            float glowH = (maxY - minY - drop) + cap * 0.36f;
            var glowGo = WorldBuilder.Sprite("ComboGlow", SpriteCatalog.Glow, hold.position, 1f, 53, hold);
            glowGo.transform.localPosition = Vector3.zero;
            glowGo.transform.localScale = new Vector3(glowW, glowH, 1f);
            var glowSr = glowGo.GetComponent<SpriteRenderer>();
            if (glowSr != null) glowSr.color = new Color(ink.r, ink.g, ink.b, 0f);
            float glowA = Mathf.Lerp(0.20f, 0.40f, tier);
            if (celebrate) glowA = Mathf.Min(0.70f, glowA * 1.45f);

            int sparkN = 6 + Mathf.RoundToInt(tier * 3f);
            if (celebrate) sparkN += 8;
            var sparks = new SpriteRenderer[sparkN];
            var sparkAge = new float[sparks.Length];
            var sparkVel = new Vector3[sparks.Length];
            const float sparkLife = 0.42f;
            bool sparked = false;

            for (int i = 0; i < n; i++)
            {
                glyphs[i].Xf.SetParent(hold, false);
                glyphs[i].Xf.localPosition = new Vector3(glyphs[i].X, glyphs[i].Y + drop, 0f);
                glyphs[i].Xf.localRotation = Quaternion.Euler(0f, 0f, glyphs[i].Tilt);
                glyphs[i].Xf.localScale = glyphs[i].Rest;
                if (glyphs[i].Face != null)
                {
                    var c = glyphs[i].Ink;
                    c.a = 0f;
                    glyphs[i].Face.color = c;
                }
            }

            float lastDelay = 0f;
            for (int i = 0; i < n; i++)
                if (glyphs[i].Delay > lastDelay) lastDelay = glyphs[i].Delay;
            const float popDur = 0.32f;
            float popEnd = lastDelay + popDur;
            float t = 0f;
            if (celebrate) _comboPopLive++;
            try
            {
                lane.Live = true;
                ComboLanes.Add(lane);
                while (t < popEnd && hold != null)
                {
                    t += Time.deltaTime;
                    float popU = Mathf.Clamp01(t / 0.30f);
                    float pop = ComboPop(popU);
                    hold.localScale = new Vector3(fit * pop, fit * pop, 1f);
                    float tilt = Mathf.Lerp(-2.8f, -0.9f, EaseOut(popU));
                    hold.localRotation = Quaternion.Euler(0f, 0f, tilt);
                    for (int i = 0; i < n; i++)
                    {
                        var g = glyphs[i];
                        if (g.Xf == null) continue;
                        float u = Mathf.Clamp01((t - g.Delay) / 0.16f);
                        float e = EaseOut(u);
                        var lp = g.Xf.localPosition;
                        lp.x = g.X;
                        lp.y = Mathf.Lerp(g.Y + drop, g.Y, e);
                        g.Xf.localPosition = lp;
                        g.Xf.localRotation = Quaternion.Euler(0f, 0f, g.Tilt);
                        g.Xf.localScale = g.Rest;
                        if (g.Face != null)
                        {
                            float a = Mathf.Clamp01(u * 1.6f);
                            float flash = 0f;
                            float land = Mathf.Clamp01((t - g.Delay - 0.10f) / 0.12f);
                            if (land > 0f && land < 1f) flash = Mathf.Sin(land * Mathf.PI) * (1f - land);
                            var c = Color.Lerp(g.Ink, Color.white, flash * 0.65f);
                            c.a = a;
                            g.Face.color = c;
                        }
                    }
                    if (glowSr != null)
                    {
                        float a = glowA * Mathf.Clamp01(popU * 1.4f);
                        glowSr.color = new Color(ink.r, ink.g, ink.b, a);
                    }
                    if (!sparked && t >= 0.12f)
                    {
                        sparked = true;
                        Vector3 origin = hold.position;
                        float rx = Mathf.Max(0.9f, (maxX - minX) * 0.5f * fit * ComboPopPeak + 0.22f);
                        float ry = Mathf.Max(0.55f, (maxY - minY - drop) * 0.5f * fit * ComboPopPeak + 0.16f);
                        for (int s = 0; s < sparks.Length; s++)
                        {
                            float ang = (s / (float)sparks.Length) * Mathf.PI * 2f + 0.2f;
                            var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                            var at = origin + new Vector3(dir.x * rx, dir.y * ry, 0f);
                            float sc = Random.Range(0.07f, 0.11f) * (1f + 0.2f * tier);
                            var sgo = WorldBuilder.Sprite("ComboSpark", SpriteCatalog.Sparkle, at, sc, 58, parent);
                            sparks[s] = sgo.GetComponent<SpriteRenderer>();
                            bool hot = (s & 1) == 0;
                            sparks[s].color = hot
                                ? new Color(1f, 0.98f, 0.92f, 1f)
                                : new Color(ink.r, ink.g, ink.b, 1f);
                            sparkVel[s] = dir * Random.Range(0.45f, 0.85f);
                            sparkAge[s] = 0f;
                        }
                    }
                    ComboStepSparks(sparks, sparkVel, sparkAge, sparkLife);
                    yield return null;
                }

                if (hold != null)
                {
                    hold.localScale = new Vector3(fit, fit, 1f);
                    hold.localRotation = Quaternion.Euler(0f, 0f, -0.9f);
                    for (int i = 0; i < n; i++)
                    {
                        if (glyphs[i].Xf == null) continue;
                        glyphs[i].Xf.localPosition = new Vector3(glyphs[i].X, glyphs[i].Y, 0f);
                        glyphs[i].Xf.localRotation = Quaternion.Euler(0f, 0f, glyphs[i].Tilt);
                        glyphs[i].Xf.localScale = glyphs[i].Rest;
                        if (glyphs[i].Face != null) glyphs[i].Face.color = glyphs[i].Ink;
                    }
                    if (glowSr != null) glowSr.color = new Color(ink.r, ink.g, ink.b, glowA);
                }

                float holdDur = 0.36f + 0.02f * Mathf.Min(combo, 12);
                if (celebrate) holdDur *= 1.65f;
                float hT = 0f;
                while (hT < holdDur && hold != null)
                {
                    hT += Time.deltaTime;
                    ComboStepSparks(sparks, sparkVel, sparkAge, sparkLife);
                    yield return null;
                }

                var fadeRs = hold != null ? hold.GetComponentsInChildren<SpriteRenderer>() : null;
                var fadeA = fadeRs != null ? new float[fadeRs.Length] : null;
                if (fadeRs != null)
                {
                    for (int i = 0; i < fadeRs.Length; i++)
                        fadeA[i] = fadeRs[i] != null ? fadeRs[i].color.a : 0f;
                }
                float fadeT = 0f;
                const float fadeDur = 0.32f;
                while (fadeT < fadeDur && hold != null)
                {
                    fadeT += Time.deltaTime;
                    float u = Mathf.Clamp01(fadeT / fadeDur);
                    float e = EaseOut(u);
                    float aMul = (1f - u) * (1f - u);
                    hold.localScale = new Vector3(fit, fit, 1f) * Mathf.Lerp(1f, 0.98f, e);
                    hold.position = basePos + new Vector3(0f, 0.08f * e, 0f);
                    if (fadeRs != null)
                    {
                        for (int i = 0; i < fadeRs.Length; i++)
                        {
                            if (fadeRs[i] == null) continue;
                            var c = fadeRs[i].color;
                            c.a = fadeA[i] * aMul;
                            fadeRs[i].color = c;
                        }
                    }
                    ComboStepSparks(sparks, sparkVel, sparkAge, sparkLife);
                    yield return null;
                }
            }
            finally
            {
                if (celebrate)
                {
                    _comboPopLive = Mathf.Max(0, _comboPopLive - 1);
                    FinaleShow.StopLaunching();
                }
                lane.Live = false;
                ComboLanes.Remove(lane);
                if (sparks != null)
                {
                    for (int i = 0; i < sparks.Length; i++)
                        if (sparks[i] != null) Object.Destroy(sparks[i].gameObject);
                }
                if (hold != null) Object.Destroy(hold.gameObject);
            }
        }

        static void ComboStepSparks(SpriteRenderer[] rs, Vector3[] vel, float[] age, float life)
        {
            if (rs == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                age[i] += dt;
                float u = Mathf.Clamp01(age[i] / life);
                rs[i].transform.position += vel[i] * dt;
                vel[i] *= FramePace.Damp(0.94f, dt);
                var c = rs[i].color;
                c.a = (1f - u) * (1f - u);
                rs[i].color = c;
            }
        }
    }
}
