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

        static Texture2D _disc;

        // Face, thin dark rim, gold sweep, outlined numeral, optional sparkle.
        // sparkle 0 still twinkles x10, so the reward card keeps a light glint.
        public static void Draw(Rect r, string text, GUIStyle st, Ink ink, float alpha, float sparkle)
        {
            if (r.width < 4f || alpha < 0.02f || st == null) return;
            var disc = Disc();
            if (disc == null) return;
            float a = Mathf.Clamp01(alpha);

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

            GUI.color = Color.white;
            int dark = Mathf.Clamp(Mathf.RoundToInt(st.fontSize * 0.20f), 3, 8);
            var numeral = ink.Numeral;
            numeral.a = a;
            DrawNumeral(r, text, st, numeral, dark);

            float glint = sparkle;
            if (ink.Shimmer && glint < 0.01f) glint = 0.55f;
            if (glint > 0.02f) DrawSparkle(r, glint * a, ink.Shimmer);
            GUI.color = Color.white;
        }

        // Slow pale band across the gold face. Disc stretched thin, not a glow sprite.
        static void DrawSweep(Rect face, float alpha, Texture disc)
        {
            float u = Mathf.Repeat(Time.unscaledTime * 0.16f, 1f);
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

        static Texture2D Disc()
        {
            if (_disc != null) return _disc;
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "StreakBadgeDisc"
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
    }
}
