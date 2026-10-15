using UnityEngine;

namespace FlockFive
{
    // One chip for every streak multiplier. Garden HUD and the reward stamp both
    // draw through here. Palette, face, gold sweep, numerals, and sparkle stay together.
    // Twinkles go through SparkleFx (SpriteCatalog.Sparkle). Not the bird selection glow.
    public static class StreakBadge
    {
        public struct Ink
        {
            public Color Deep;
            public Color Fill;
            public Color Sheen;
            public Color Edge;
            public Color Numeral;
            public bool Shimmer;
        }

        // x2 red, x3 green, x4 blue, x5 purple, x10 gold. 6-9 follow Display (x5).
        // Deep + sheen keep a chip readable on cream daylight and on the lilac dusk veil.
        // Edges stay near-black so a bright sky cannot eat the rim.
        public static Ink Colors(int raw)
        {
            int mul = StreakTier.Display(raw);
            if (mul >= 10) return Gold;
            if (mul >= 5) return Purple;
            if (mul == 4) return Blue;
            if (mul == 3) return Green;
            if (mul == 2) return Red;
            return Neutral;
        }

        static readonly Ink Red = new Ink
        {
            Deep = new Color(0.46f, 0.05f, 0.07f, 1f),
            Fill = new Color(0.86f, 0.14f, 0.16f, 1f),
            Sheen = new Color(1f, 0.62f, 0.54f, 1f),
            Edge = new Color(0.14f, 0.02f, 0.03f, 1f),
            Numeral = new Color(1f, 0.97f, 0.94f, 1f),
            Shimmer = false
        };

        static readonly Ink Green = new Ink
        {
            Deep = new Color(0.03f, 0.28f, 0.08f, 1f),
            Fill = new Color(0.12f, 0.62f, 0.24f, 1f),
            Sheen = new Color(0.72f, 0.96f, 0.58f, 1f),
            Edge = new Color(0.02f, 0.12f, 0.04f, 1f),
            Numeral = new Color(1f, 0.98f, 0.94f, 1f),
            Shimmer = false
        };

        static readonly Ink Blue = new Ink
        {
            Deep = new Color(0.04f, 0.14f, 0.38f, 1f),
            Fill = new Color(0.16f, 0.46f, 0.94f, 1f),
            Sheen = new Color(0.70f, 0.88f, 1f, 1f),
            Edge = new Color(0.02f, 0.06f, 0.16f, 1f),
            Numeral = new Color(1f, 0.98f, 0.96f, 1f),
            Shimmer = false
        };

        static readonly Ink Purple = new Ink
        {
            Deep = new Color(0.26f, 0.05f, 0.36f, 1f),
            Fill = new Color(0.60f, 0.18f, 0.82f, 1f),
            Sheen = new Color(0.92f, 0.72f, 1f, 1f),
            Edge = new Color(0.12f, 0.03f, 0.16f, 1f),
            Numeral = new Color(1f, 0.97f, 0.98f, 1f),
            Shimmer = false
        };

        // Antique bottom, warm gold face, pale metal cap.
        static readonly Ink Gold = new Ink
        {
            Deep = new Color(0.50f, 0.26f, 0.04f, 1f),
            Fill = new Color(0.92f, 0.64f, 0.12f, 1f),
            Sheen = new Color(1f, 0.97f, 0.80f, 1f),
            Edge = new Color(0.12f, 0.06f, 0.02f, 1f),
            // Cream, not the antique brown: brown sat on the gold face and disappeared.
            Numeral = new Color(1f, 0.98f, 0.94f, 1f),
            Shimmer = true
        };

        static readonly Ink Neutral = new Ink
        {
            Deep = new Color(0.40f, 0.36f, 0.32f, 1f),
            Fill = new Color(0.76f, 0.72f, 0.66f, 1f),
            Sheen = new Color(0.96f, 0.94f, 0.90f, 1f),
            Edge = new Color(0.16f, 0.13f, 0.11f, 1f),
            Numeral = new Color(0.22f, 0.12f, 0.06f, 1f),
            Shimmer = false
        };

        public const string DiscName = "StreakBadgeDisc";
        public const string CrackName = "StreakBadgeCrack";

        // How long the kept badge takes to seal, sweep, and pop.
        public const float RestoreSeconds = 1.72f;

        // Pose for one draw. Calm is the garden chip and the reward stamp.
        // AtRisk and Restored are the keep-multiplier popup only.
        public struct Risk
        {
            public float Scale;
            public float Roll;
            public float Crack;
            public float Mend;
            public float Sheen;
            public float SheenU;
            public float Glow;

            public static Risk Calm => new Risk { Scale = 1f };
        }

        // Gentle swell and a slow uneven tilt. The fissure stays open.
        public static Risk AtRisk(float time)
        {
            float swell = Mathf.Sin(time * 4.6f);
            float roll = Mathf.Sin(time * 3.6f) * 3.1f + Mathf.Sin(time * 7.4f + 0.6f) * 0.9f;
            float open = 0.5f + 0.5f * swell;
            return new Risk
            {
                Scale = 1f + 0.048f * swell,
                Roll = roll,
                Crack = 0.62f + 0.22f * open,
                Mend = 0f,
                Sheen = 0f,
                SheenU = 0f,
                Glow = 0f
            };
        }

        // Crack seals, then a pale sweep, then a glow pop. Times overlap just
        // enough that the badge never goes blank between beats.
        public static Risk Restored(float age)
        {
            float t = age < 0f ? 0f : age;
            if (t > RestoreSeconds) t = RestoreSeconds;
            float healU = Ease(t / 0.58f);
            float crack = Mathf.Lerp(0.84f, 0f, healU);
            float mend = Mathf.Sin(Mathf.Clamp01(t / 0.58f) * Mathf.PI) * 0.9f;
            float damp = 1f - healU;
            float sheenT = Mathf.InverseLerp(0.50f, 1.12f, t);
            float sheen = sheenT > 0f && sheenT < 1f ? Mathf.Sin(sheenT * Mathf.PI) : 0f;
            float glowT = Mathf.InverseLerp(1.02f, 1.62f, t);
            float glow = glowT > 0f && glowT < 1f ? Mathf.Sin(glowT * Mathf.PI) : 0f;
            float scale = Mathf.Lerp(1.02f, 1f, healU) * (1f + 0.08f * glow);
            float roll = Mathf.Sin(t * 3.6f) * 1.7f * damp;
            if (t >= RestoreSeconds - 0.02f)
            {
                crack = 0f;
                mend = 0f;
                sheen = 0f;
                glow = 0f;
                scale = 1f;
                roll = 0f;
            }
            return new Risk
            {
                Scale = scale,
                Roll = roll,
                Crack = crack,
                Mend = mend,
                Sheen = sheen,
                SheenU = Mathf.Clamp01(sheenT),
                Glow = glow
            };
        }

        static float Ease(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }

        static Texture2D _disc;

        // The one disc Draw paints. Wheel x2 and the streak chip share it.
        public static Texture2D SharedDisc()
        {
            return Disc();
        }

        // Top-right of the thing the multiplier modifies. Inset matches the reward stamp.
        public static Rect TopRight(Rect host, float diameter)
        {
            float d = diameter < 4f ? 4f : diameter;
            float inset = d * 0.22f;
            float cx = host.xMax - inset;
            float cy = host.y + inset;
            return new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d);
        }

        // Face, thin dark rim, gold sweep, outlined numeral, optional sparkle.
        // sparkle 0 still twinkles x10, so the reward card keeps a light glint.
        public static void Draw(Rect r, string text, GUIStyle st, Ink ink, float alpha, float sparkle)
        {
            Draw(r, text, st, ink, alpha, sparkle, Risk.Calm);
        }

        // Risk.Calm matches Draw. AtRisk and Restored add the keep-popup pose.
        public static void Draw(Rect r, string text, GUIStyle st, Ink ink, float alpha, float sparkle, Risk risk)
        {
            if (r.width < 4f || alpha < 0.02f || st == null) return;
            var disc = Disc();
            if (disc == null) return;
            float a = Mathf.Clamp01(alpha);
            if (risk.Scale < 0.5f) risk.Scale = 1f;

            var prev = GUI.matrix;
            bool xform = Mathf.Abs(risk.Scale - 1f) > 0.001f || Mathf.Abs(risk.Roll) > 0.05f;
            if (xform)
            {
                var pivot = r.center;
                if (Mathf.Abs(risk.Roll) > 0.05f)
                    GUIUtility.RotateAroundPivot(risk.Roll, pivot);
                if (Mathf.Abs(risk.Scale - 1f) > 0.001f)
                    GUIUtility.ScaleAroundPivot(new Vector2(risk.Scale, risk.Scale), pivot);
            }

            if (risk.Glow > 0.02f) DrawGlow(r, ink, risk.Glow, a);

            var shadow = new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.05f, r.width, r.height);
            GUI.color = new Color(0.05f, 0.03f, 0.02f, 0.42f * a);
            GUI.DrawTexture(shadow, disc, ScaleMode.ScaleToFit, true);

            var edge = ink.Edge;
            edge.a = a;
            GUI.color = edge;
            GUI.DrawTexture(r, disc, ScaleMode.ScaleToFit, true);

            float lip = r.width * 0.045f;
            var face = new Rect(r.x + lip, r.y + lip, Mathf.Max(2f, r.width - lip * 2f), Mathf.Max(2f, r.height - lip * 2f));
            var deep = ink.Deep;
            deep.a = a;
            GUI.color = deep;
            GUI.DrawTexture(face, disc, ScaleMode.ScaleToFit, true);

            float side = face.width * 0.05f;
            var upper = new Rect(face.x + side, face.y, face.width - side * 2f, face.height * 0.70f);
            var fill = ink.Fill;
            fill.a = a;
            GUI.color = fill;
            GUI.DrawTexture(upper, disc, ScaleMode.ScaleToFit, true);

            // Upper-left gloss, above the numeral. A wide cap across the middle
            // sat behind "x10" and washed the digits out.
            float capW = face.width * 0.34f;
            float capH = face.height * 0.20f;
            var cap = new Rect(face.x + face.width * 0.08f, face.y + face.height * 0.045f, capW, capH);
            var sheen = ink.Sheen;
            sheen.a = (ink.Shimmer ? 0.90f : 0.58f) * a;
            GUI.color = sheen;
            GUI.DrawTexture(cap, disc, ScaleMode.ScaleToFit, true);

            if (ink.Shimmer)
            {
                float dot = face.width * 0.10f;
                var spec = new Rect(face.x + face.width * 0.12f, face.y + face.height * 0.07f, dot, dot * 0.62f);
                GUI.color = new Color(1f, 0.99f, 0.94f, 0.55f * a);
                GUI.DrawTexture(spec, disc, ScaleMode.ScaleToFit, true);
                DrawSweep(face, a, disc);
            }

            if (risk.Mend > 0.02f || risk.Crack > 0.02f)
                DrawFissure(face, risk, a);
            if (risk.Sheen > 0.02f)
                DrawRestoreSheen(face, risk.Sheen, risk.SheenU, a);
            if (risk.Glow > 0.02f)
                DrawRestorePop(face, ink, risk.Glow, a);

            GUI.color = Color.white;
            int dark = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.20f), 3, 8);
            var numeral = ink.Numeral;
            numeral.a = a;
            DrawNumeral(r, text, st, numeral, dark);

            float glint = sparkle;
            if (ink.Shimmer && glint < 0.01f) glint = 0.55f;
            if (glint > 0.02f) DrawSparkle(r, glint * a, ink.Shimmer);
            GUI.color = Color.white;
            if (xform) GUI.matrix = prev;
        }

        // Soft bloom behind the chip. The face draws over the middle, so the
        // light reads as a ring. The pop is a wide sheen halo plus a tighter
        // warm rim, enough to read as restored on the dark card.
        static void DrawGlow(Rect r, Ink ink, float glow, float alpha)
        {
            var tex = GlowDisc();
            if (tex == null) return;
            float g = r.width * Mathf.Lerp(1.30f, 2.15f, glow);
            var halo = new Rect(r.center.x - g * 0.5f, r.center.y - g * 0.5f, g, g);
            var c = ink.Sheen;
            c.a = 0.88f * glow * alpha;
            GUI.color = c;
            GUI.DrawTexture(halo, tex, ScaleMode.ScaleToFit, true);
            float g2 = r.width * Mathf.Lerp(1.06f, 1.48f, glow);
            var rim = new Rect(r.center.x - g2 * 0.5f, r.center.y - g2 * 0.5f, g2, g2);
            var hot = Color.Lerp(ink.Sheen, new Color(1f, 0.99f, 0.94f, 1f), 0.40f);
            hot.a = 0.62f * glow * alpha;
            GUI.color = hot;
            GUI.DrawTexture(rim, tex, ScaleMode.ScaleToFit, true);
        }

        // Painted fissure on the glaze. While the streak is at risk the whole
        // hairline breathes. Once the ad has closed, the cut seals from its tip
        // back toward the root, and a warm bead of glaze follows the front.
        static void DrawFissure(Rect face, Risk risk, float alpha)
        {
            bool healing = risk.Mend > 0.04f;
            float open = healing ? Mathf.Clamp01(risk.Crack / 0.84f) : 1f;
            var tex = healing ? CrackVisible(open) : CrackTex();
            if (tex == null) return;
            if (risk.Crack > 0.02f)
            {
                float a = healing ? alpha : risk.Crack * alpha;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(face, tex, ScaleMode.ScaleToFit, true);
            }
            if (healing) DrawMendBead(face, open, risk.Mend, alpha);
        }

        // Wet clay laid along the fissure at the closing end. Stretched with
        // the cut, so it reads as a seal and not a round dab.
        static void DrawMendBead(Rect face, float open, float mend, float alpha)
        {
            var disc = Disc();
            if (disc == null || mend < 0.02f) return;
            float t = Mathf.Clamp01(open);
            var front = FaceOfTex(PointAlong(CrackMain, t));
            var back = FaceOfTex(PointAlong(CrackMain, Mathf.Min(1f, t + 0.10f)));
            float cx = face.x + front.x * face.width;
            float cy = face.y + front.y * face.height;
            float bx = face.x + back.x * face.width;
            float by = face.y + back.y * face.height;
            float ang = Mathf.Atan2(by - cy, bx - cx) * Mathf.Rad2Deg;
            float len = face.width * 0.16f;
            float thick = face.width * 0.042f;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, new Vector2(cx, cy));
            GUI.color = new Color(0.38f, 0.18f, 0.07f, 0.92f * mend * alpha);
            GUI.DrawTexture(new Rect(cx - len * 0.08f, cy - thick * 0.42f, len * 0.78f, thick), disc, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.93f, 0.72f, 0.32f, 0.88f * mend * alpha);
            GUI.DrawTexture(new Rect(cx + len * 0.02f, cy - thick * 0.22f, len * 0.46f, thick * 0.55f), disc, ScaleMode.StretchToFill, true);
            GUI.matrix = prev;
        }

        // One long fused brush across the light glaze, above the numeral.
        // The stroke is wide enough that a single frame reads as a sweep.
        static void DrawRestoreSheen(Rect face, float env, float u, float alpha)
        {
            var disc = Disc();
            if (disc == null || env < 0.02f) return;
            float travel = Mathf.Lerp(0.28f, 0.72f, Mathf.Clamp01(u));
            const int n = 36;
            const float span = 0.56f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (n - 1f);
                float bow = Mathf.Sin(k * Mathf.PI);
                float x = face.x + face.width * (travel + (k - 0.5f) * span);
                float y = face.y + face.height * (0.34f - 0.035f * bow);
                if (!OnGlaze(face, x, y)) continue;
                float grit = 0.88f + 0.12f * Hash01(i * 5, 17);
                float w = face.width * (0.055f + 0.04f * bow) * grit;
                float h = face.height * (0.040f + 0.028f * bow);
                GUI.color = new Color(1f, 0.98f, 0.88f, 0.92f * env * alpha * Mathf.Lerp(0.25f, 1f, bow));
                GUI.DrawTexture(new Rect(x - w * 0.5f, y - h * 0.5f, w, h), disc, ScaleMode.ScaleToFit, true);
            }
        }

        static bool OnGlaze(Rect face, float x, float y)
        {
            float lx = (x - face.center.x) / (face.width * 0.46f);
            float ly = (y - face.center.y) / (face.height * 0.46f);
            return lx * lx + ly * ly <= 1f;
        }

        // The pop sits on the glaze itself. The halo behind the chip is the bloom;
        // this is the face lighting up, under the numeral so "x3" stays sharp.
        static void DrawRestorePop(Rect face, Ink ink, float glow, float alpha)
        {
            var disc = Disc();
            if (disc == null || glow < 0.02f) return;
            float wash = face.width * 0.92f;
            var body = new Rect(face.center.x - wash * 0.5f, face.center.y - wash * 0.5f, wash, wash);
            var hot = Color.Lerp(ink.Sheen, new Color(1f, 0.98f, 0.90f, 1f), 0.65f);
            hot.a = 0.58f * glow * alpha;
            GUI.color = hot;
            GUI.DrawTexture(body, disc, ScaleMode.ScaleToFit, true);
            float s = face.width * Mathf.Lerp(0.16f, 0.36f, glow);
            var flare = new Rect(face.x + face.width * 0.09f, face.y + face.height * 0.05f, s, s * 0.62f);
            GUI.color = new Color(1f, 0.995f, 0.94f, 0.92f * glow * alpha);
            GUI.DrawTexture(flare, disc, ScaleMode.ScaleToFit, true);
        }

        // Slow pale band across the gold face. Disc stretched thin, not a glow sprite.
        static void DrawSweep(Rect face, float alpha, Texture disc)
        {
            float u = Mathf.Repeat(AdPause.Unscaled * 0.16f, 1f);
            float fade = Mathf.Sin(u * Mathf.PI);
            if (fade < 0.04f) return;
            // Stay in the upper-left gloss. A tall band crossed the numeral.
            float travel = Mathf.Lerp(0.06f, 0.32f, u);
            float w = face.width * 0.12f;
            float h = face.height * 0.18f;
            var band = new Rect(face.x + face.width * travel, face.y + face.height * 0.04f, w, h);
            GUI.color = new Color(1f, 0.99f, 0.90f, 0.24f * fade * alpha);
            GUI.DrawTexture(band, disc, ScaleMode.StretchToFill, true);
        }

        // Celebration twinkles on the chip's rim only (shared SparkleFx border mode), never
        // over the face or the numeral. The ring is the dark lip plus a hair of face.
        static void DrawSparkle(Rect r, float alpha, bool gold)
        {
            SparkleFx.DrawBorder(r, r.width * 0.07f, alpha, gold, sizeFrac: 0.12f, round: true);
        }

        // Box the caller fits "x10" into, so the thick outline and the drop shadow
        // stay inside the rim. Width is the tight axis on "x10".
        public static void NumeralFit(Rect disc, out float width, out float height)
        {
            width = disc.width * 0.56f;
            height = disc.height * 0.42f;
        }

        static void DrawNumeral(Rect r, string text, GUIStyle st, Color ink, int darkPx)
        {
            if (string.IsNullOrEmpty(text)) return;
            float a = Mathf.Clamp01(ink.a);
            if (a < 0.04f) return;
            var prevStyle = st.fontStyle;
            var prevAlign = st.alignment;
            var prevWrap = st.wordWrap;
            var prevClip = st.clipping;
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.clipping = TextClipping.Overflow;
            // Soft drop, down-right, under the outline.
            float sh = Mathf.Max(2f, st.fontSize * 0.08f);
            Paint(st, new Color(0.10f, 0.05f, 0.02f, a * 0.28f));
            GUI.Label(new Rect(r.x + sh * 0.55f, r.y + sh * 0.70f, r.width, r.height), text, st);
            Paint(st, new Color(0.08f, 0.04f, 0.02f, a * 0.45f));
            GUI.Label(new Rect(r.x + sh, r.y + sh * 1.15f, r.width, r.height), text, st);
            Paint(st, new Color(0.05f, 0.02f, 0.01f, a));
            int px = darkPx < 1 ? 1 : darkPx;
            for (int ring = 1; ring <= px; ring++)
            {
                int n = ring >= 4 ? 16 : ring >= 2 ? 12 : 8;
                for (int i = 0; i < n; i++)
                {
                    float ang = i * (Mathf.PI * 2f / n);
                    GUI.Label(new Rect(r.x + Mathf.Cos(ang) * ring, r.y + Mathf.Sin(ang) * ring, r.width, r.height), text, st);
                }
            }
            var face = ink;
            face.a = a;
            Paint(st, face);
            GUI.Label(r, text, st);
            st.fontStyle = prevStyle;
            st.alignment = prevAlign;
            st.wordWrap = prevWrap;
            st.clipping = prevClip;
        }

        static void Paint(GUIStyle st, Color c)
        {
            st.normal.textColor = c;
            st.hover.textColor = c;
            st.active.textColor = c;
            st.focused.textColor = c;
            st.onNormal.textColor = c;
            st.onHover.textColor = c;
            st.onActive.textColor = c;
        }

        static Texture2D _crack;
        static Texture2D _glow;
        static float _crackFrac;
        static int _crackRun;
        static Color32[] _crackSrc;
        static byte[] _crackAlong;
        static Texture2D _crackLive;
        static Color32[] _crackLivePx;
        static int _crackLiveKey = -1;

        const int CrackRes = 192;

        // Diagonal so no scanline fills (a flat stroke would read as a bar).
        // Ends sit in the open glaze beside the numeral; the middle passes under it.
        static readonly Vector2[] CrackMain =
        {
            new Vector2(16f, 156f),
            new Vector2(46f, 124f),
            new Vector2(78f, 92f),
            new Vector2(114f, 64f),
            new Vector2(174f, 30f)
        };

        static readonly Vector2[] CrackFork =
        {
            new Vector2(46f, 124f),
            new Vector2(62f, 108f),
            new Vector2(74f, 96f)
        };

        // Hairline coverage of the disc, and the widest solid run. A filled
        // mark (the old caution triangle) fails both.
        public static void CrackStats(out float frac, out int run)
        {
            CrackTex();
            frac = _crackFrac;
            run = _crackRun;
        }

        static Texture2D GlowDisc()
        {
            if (_glow != null) return _glow;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "StreakBadgeGlow"
            };
            var px = new Color32[n * n];
            float c = n * 0.5f;
            float rad = c - 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float u = Mathf.Sqrt(dx * dx + dy * dy) / rad;
                    if (u >= 1f) continue;
                    float a = Mathf.Clamp01(1f - u);
                    a = a * a * (0.85f + 0.15f * Mathf.Sin(x * 0.7f + y * 0.4f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _glow = tex;
            return tex;
        }

        // One fissure and a short fork, masked to the disc. Dark clay in the
        // cut, a pale glaze lip beside it. Not a triangle, not a sticker.
        // _crackAlong is 0 off the cut, 1 at the root, 255 at the tip, so a
        // heal can shorten the same stroke instead of fading it.
        static Texture2D CrackTex()
        {
            if (_crack != null) return _crack;
            const int n = CrackRes;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = CrackName
            };
            var px = new Color32[n * n];
            var alongPx = new byte[n * n];
            float c = n * 0.5f;
            float rad = c - 2f;
            int discN = 0;
            int inkN = 0;
            int run = 0;
            for (int y = 0; y < n; y++)
            {
                int rowRun = 0;
                var p = new Vector2(0f, y + 0.5f);
                for (int x = 0; x < n; x++)
                {
                    p.x = x + 0.5f;
                    float dx = p.x - c;
                    float dy = p.y - c;
                    float disc = rad - Mathf.Sqrt(dx * dx + dy * dy);
                    if (disc <= 0f) { rowRun = 0; continue; }
                    discN++;
                    float dMain = PathDist(p, CrackMain);
                    float dFork = PathDist(p, CrackFork);
                    bool fork = dFork + 0.35f < dMain;
                    float dist = dMain;
                    float width = 2.85f;
                    if (fork) { dist = dFork; width = 1.55f; }
                    float wobble = Mathf.Sin(p.x * 0.31f + p.y * 0.17f);
                    width *= 0.78f + 0.28f * wobble;
                    float u = dist / Mathf.Max(0.4f, width);
                    if (u > 1.65f) { rowRun = 0; continue; }
                    float a = Mathf.Clamp01(1.2f - u);
                    a *= a;
                    float speck = Hash01(x, y);
                    if (speck > 0.86f && u > 0.4f) a *= 0.3f;
                    if (speck < 0.035f && u < 0.55f) a *= 0.55f;
                    float edge = Mathf.Clamp01(disc);
                    if (edge < 1f) a *= edge;
                    if (a < 0.02f) { rowRun = 0; continue; }
                    var col = Color.Lerp(
                        new Color(0.15f, 0.06f, 0.03f, 1f),
                        new Color(0.95f, 0.88f, 0.68f, 1f),
                        Mathf.Clamp01(u * 0.85f));
                    col.a = a;
                    int idx = y * n + x;
                    px[idx] = col;
                    float tAlong = fork ? PathT(p, CrackFork) : PathT(p, CrackMain);
                    float along01 = fork ? Mathf.Lerp(0.58f, 1f, tAlong) : tAlong;
                    alongPx[idx] = (byte)(1 + Mathf.RoundToInt(Mathf.Clamp01(along01) * 254f));
                    if (a > 0.12f) inkN++;
                    if (a > 0.16f) rowRun++;
                    else rowRun = 0;
                    if (rowRun > run) run = rowRun;
                }
            }
            _crackFrac = discN > 0 ? inkN / (float)discN : 0f;
            _crackRun = run;
            _crackSrc = px;
            _crackAlong = alongPx;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _crack = tex;
            return tex;
        }

        // Shortens the baked fissure from the tip. Buckets so a frame does not
        // rebuild the texture. The full crack is the open pose.
        static Texture2D CrackVisible(float open)
        {
            CrackTex();
            if (_crackSrc == null || _crackAlong == null) return _crack;
            int key = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(open) * 24f), 0, 24);
            if (key >= 24) return _crack;
            if (_crackLive != null && _crackLiveKey == key) return _crackLive;
            const int n = CrackRes;
            if (_crackLive == null)
            {
                _crackLive = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "StreakBadgeCrackSeal"
                };
                _crackLivePx = new Color32[n * n];
            }
            byte limit = (byte)Mathf.Clamp(Mathf.RoundToInt(key / 24f * 255f), 0, 255);
            var src = _crackSrc;
            var along = _crackAlong;
            var dst = _crackLivePx;
            for (int i = 0; i < dst.Length; i++)
            {
                byte mark = along[i];
                if (mark == 0 || mark > limit)
                {
                    dst[i] = default;
                    continue;
                }
                var col = src[i];
                int edge = limit - mark;
                if (edge < 16) col.a = (byte)(col.a * edge / 16);
                dst[i] = col;
            }
            _crackLive.SetPixels32(dst);
            _crackLive.Apply(false, false);
            _crackLiveKey = key;
            return _crackLive;
        }

        // Texture pixels are y-up. GUI rects are y-down, so the bead sits on the stroke.
        static Vector2 FaceOfTex(Vector2 texPx)
        {
            return new Vector2(texPx.x / CrackRes, 1f - texPx.y / CrackRes);
        }

        static Vector2 PointAlong(Vector2[] path, float t)
        {
            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
                total += Vector2.Distance(path[i], path[i + 1]);
            float want = Mathf.Clamp01(t) * total;
            for (int i = 0; i < path.Length - 1; i++)
            {
                float len = Vector2.Distance(path[i], path[i + 1]);
                if (want <= len || i == path.Length - 2)
                {
                    float u = len < 0.01f ? 0f : Mathf.Clamp01(want / len);
                    return Vector2.Lerp(path[i], path[i + 1], u);
                }
                want -= len;
            }
            return path[path.Length - 1];
        }

        static float PathT(Vector2 p, Vector2[] path)
        {
            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
                total += Vector2.Distance(path[i], path[i + 1]);
            float best = 999f;
            float bestLen = 0f;
            float walked = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                float len = Vector2.Distance(path[i], path[i + 1]);
                float seg = SegDistT(p, path[i], path[i + 1], out float u);
                if (seg < best)
                {
                    best = seg;
                    bestLen = walked + u * len;
                }
                walked += len;
            }
            return total < 0.01f ? 0f : Mathf.Clamp01(bestLen / total);
        }

        static float SegDistT(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.01f) { t = 0f; return Vector2.Distance(p, a); }
            t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        static float PathDist(Vector2 p, Vector2[] path)
        {
            float d = 999f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                float seg = SegDist(p, path[i], path[i + 1]);
                if (seg < d) d = seg;
            }
            return d;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.01f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        static float Hash01(int x, int y)
        {
            uint n = (uint)(x * 374761393 + y * 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 255) / 255f;
        }

        static Texture2D Disc()
        {
            if (_disc != null) return _disc;
            var trace = StartupTrace.Begin("StreakBadge.Disc");
            try
            {
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = DiscName
            };
            var px = new Color32[n * n];
            float c = n * 0.5f;
            float rad = c - 1.2f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(rad - dist + 0.75f);
                    px[y * n + x] = a <= 0.004f
                        ? new Color32(0, 0, 0, 0)
                        : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _disc = tex;
            return tex;
            }
            finally
            {
                trace.Dispose();
            }
        }
    }
}
