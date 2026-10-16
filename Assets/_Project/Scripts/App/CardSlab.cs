using UnityEngine;

namespace FlockFive
{
    // Shared card-motion helpers: flip angle, slab footprint, parallax, and the OnGUI glare / brass-edge draws.
    // CardStage (3D) and the Lite OnGUI path both read the same numbers, so the two tiers flip the same way.
    public static class CardSlab
    {
        public const float Thick = 0.018f;          // slab thickness / card width
        public const float FlipSeconds = 0.28f;
        public const float MaxParallax = 0.030f;    // largest layer shift / card width (scope: 3%)

        // Layer depths (card plane = 0). Positive floats toward the viewer.
        public const float DepthArt = -0.5f, DepthGlow = 0.1f, DepthBee = 0.9f, DepthText = 0.5f;

        public static float Smoother(float x) { x = Mathf.Clamp01(x); return x * x * x * (x * (x * 6f - 15f) + 10f); }

        // 0..PI. flipU is the existing linear 0..1 over FlipSeconds. Face swap at flipU 0.5 == angle PI/2, so the old swap logic still holds.
        public static float Angle(float flipU) => Smoother(flipU) * Mathf.PI;

        // Face width factor |cos|.
        public static float Squash(float angle) => Mathf.Abs(Mathf.Cos(angle));

        // Small lift toward the viewer while turning (scope: +5%), as height scale of the card.
        public static float Lift(float angle) => 1f + 0.05f * Mathf.Sin(angle);

        // Screen footprint of the slab seen from the front in orthographic: face |cos| W, wall T |sin|.
        // The wall hangs off the left side before the half-turn and the right side after it.
        public static void Footprint(Rect card, float angle, out Rect face, out Rect wall)
        {
            float sq = Mathf.Max(0.02f, Squash(angle));
            float wallW = Mathf.Max(0f, card.width * Thick * Mathf.Sin(angle) * 2.2f);   // x2.2: reads at phone size, true T would be ~7 px
            float h = card.height * Lift(angle);
            float y = card.center.y - h * 0.5f;
            float fw = card.width * sq;
            float cx = card.center.x;
            bool left = angle < Mathf.PI * 0.5f;
            float total = fw + wallW;
            float x0 = cx - total * 0.5f;
            if (left)
            {
                wall = new Rect(x0, y, wallW, h);
                face = new Rect(x0 + wallW, y, fw, h);
            }
            else
            {
                face = new Rect(x0, y, fw, h);
                wall = new Rect(x0 + fw, y, wallW, h);
            }
        }

        // Layer shift in px for a layer depth, from the tilt. Opposite sign for layers behind the card plane.
        // Snapped to whole pixels for text (fractional offsets blur IMGUI type).
        public static Vector2 Parallax(Vector2 tilt, float depth, float cardW, bool snap)
        {
            var o = new Vector2(tilt.x, -tilt.y) * (depth * MaxParallax * cardW * 0.5f);
            if (snap) o = new Vector2(Mathf.Round(o.x), Mathf.Round(o.y));
            return o;
        }

        // ---------- glare (OnGUI) ----------
        static Texture2D _glare, _edge;

        // Soft gaussian band profile along x. Drawn rotated 28 degrees.
        static Texture2D GlareTex()
        {
            if (_glare != null) return _glare;
            const int W = 128, H = 2;
            var px = new Color32[W * H];
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W * 2f - 1f;
                byte a = (byte)Mathf.RoundToInt(255f * Mathf.Exp(-u * u * 5.5f));
                for (int y = 0; y < H; y++) px[y * W + x] = new Color32(255, 255, 255, a);
            }
            _glare = Tex(px, W, H, "CardGlare");
            return _glare;
        }

        // Brass edge: bright centre line, darker rims, fine brushed grain across the strip.
        static Texture2D EdgeTex()
        {
            if (_edge != null) return _edge;
            const int W = 16, H = 4;
            var px = new Color32[W * H];
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                float v = 0.62f + 0.38f * Mathf.Sin(u * Mathf.PI);
                if (u < 0.12f || u > 0.88f) v *= 0.68f;
                for (int y = 0; y < H; y++)
                    px[y * W + x] = new Color32((byte)(225f * v), (byte)(168f * v), (byte)(60f * v), 255);
            }
            _edge = Tex(px, W, H, "CardEdge");
            return _edge;
        }

        static Texture2D Tex(Color32[] px, int w, int h, string name)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = name
            };
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        // Brass edge strip of the slab (Lite flip). `lit` 0..1 brightens it as it turns toward the light.
        public static void DrawWall(Rect wall, float lit)
        {
            if (wall.width < 0.75f) return;
            float k = Mathf.Lerp(0.55f, 1.05f, Mathf.Clamp01(lit));
            GUI.color = new Color(k, k, k, 1f);
            GUI.DrawTexture(wall, EdgeTex(), ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        // Gentle glare band. `area` is the rect it is clipped to (call inside CardClip.Begin(area), pass the local rect).
        // Position follows the tilt only, so a card held still shows a still glare. Strength is capped low on purpose.
        public static void DrawGlare(Rect local, BeeFinish finish, Vector2 tilt, float strength)
        {
            if (strength <= 0.001f) return;
            float c = 0.45f + tilt.x * 0.75f - tilt.y * 0.25f;     // 0..1 across, from the scope's glare model
            float bandW = local.width * 0.55f;
            float len = Mathf.Sqrt(local.width * local.width + local.height * local.height) * 1.5f;
            var center = new Vector2(local.x + local.width * c, local.center.y);
            Color col = Color.white;
            if (finish != BeeFinish.Normal)
            {
                float h = Mathf.Repeat(0.55f + tilt.x * 0.55f + tilt.y * 0.30f, 1f);
                col = Color.Lerp(Color.white, Color.HSVToRGB(h, finish == BeeFinish.Holo ? 0.35f : 0.55f, 1f), 0.55f);
            }
            col.a = Mathf.Clamp01(strength) * (finish == BeeFinish.Normal ? 0.28f : 0.36f);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(28f, center);
            GUI.color = col;
            GUI.DrawTexture(new Rect(center.x - bandW * 0.5f, center.y - len * 0.5f, bandW, len), GlareTex(), ScaleMode.StretchToFill, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // Foil only: a faint tilt-driven prism wash over the art (replaces the free-running time sweep while the card is live).
        public static void DrawPrism(Rect local, BeeFinish finish, Vector2 tilt, float strength)
        {
            if (finish == BeeFinish.Normal || strength <= 0.001f) return;
            float h = Mathf.Repeat(0.52f + tilt.x * 0.45f + tilt.y * 0.35f, 1f);
            Color c = Color.HSVToRGB(h, finish == BeeFinish.Holo ? 0.40f : 0.60f, 1f);
            c.a = (finish == BeeFinish.Holo ? 0.07f : 0.09f) * Mathf.Clamp01(strength);
            GUI.color = c;
            GUI.DrawTexture(local, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
