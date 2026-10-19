using UnityEngine;

namespace FlockFive
{
    // Build 115. The ostrich garden's ground. The painted garden backdrop has no floor at the
    // ostrich's foot height (his feet land mid-screen, well above the stage level the other
    // gardens use), so he read as floating. This draws a grass-lipped ledge whose top edge
    // sits at his feet, fading into the backdrop below, plus a soft contact shadow that follows
    // his feet. Pure sprites: no colliders, no input, nothing the tutorial gate can hit-test.
    // Owned by OstrichMark: it shows and hides with the ostrich, so it never appears before
    // the ostrich does (tutorial cue included) and goes when he flees.
    public sealed class OstrichGround : MonoBehaviour
    {
        // Above the backdrop (-20) and life sheets (-12), below the body/legs (-3).
        public const int LedgeOrder = -8;
        public const int ShadowOrder = -5;
        // Ledge top edge rides this far above the toe line so the toes sit in the grass lip.
        public const float LedgeLift = 0.10f;
        // How far the ledge's soil runs below the toe line before it has faded out.
        public const float LedgeDepth = 1.60f;
        // Full width of the portrait play area plus a little bleed at each side.
        public const float LedgeWidth = WorldBuilder.CamOrtho * 9f / 16f * 2f + 1.0f;
        // Contact shadow under the feet (world units) and its darkness.
        public const float ShadowWidth = 3.60f;
        public const float ShadowHeight = 0.46f;
        public const float ShadowAlpha = 0.38f;
        public const float ShadowDrop = 0.03f;

        const int TexW = 128, TexH = 64;
        static Sprite _ledge, _shadow;

        SpriteRenderer _ledgeSr, _shadowSr;
        Transform _shadowT;

        public SpriteRenderer Ledge => _ledgeSr;
        public SpriteRenderer Shadow => _shadowSr;
        public bool Shown => _ledgeSr != null && _ledgeSr.enabled;

        public static OstrichGround Attach(Transform parent)
        {
            var go = new GameObject("OstrichGround");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<OstrichGround>();
            g.Build();
            return g;
        }

        void Build()
        {
            var l = new GameObject("Ledge");
            l.transform.SetParent(transform, false);
            _ledgeSr = l.AddComponent<SpriteRenderer>();
            _ledgeSr.sprite = LedgeSprite();
            FlockSort.Apply(_ledgeSr, LedgeOrder);
            l.transform.localScale = new Vector3(LedgeWidth, LedgeDepth / (TexH / (float)TexW), 1f);

            var s = new GameObject("ContactShadow");
            s.transform.SetParent(transform, false);
            _shadowT = s.transform;
            _shadowSr = s.AddComponent<SpriteRenderer>();
            _shadowSr.sprite = ShadowSprite();
            _shadowSr.color = new Color(0f, 0f, 0f, ShadowAlpha);
            FlockSort.Apply(_shadowSr, ShadowOrder);
            Show(false);
        }

        // Ledge top edge world y for a given toe line.
        public static float LedgeTop(float footY) => footY + LedgeLift;

        // footX is the middle of the planted feet; turn is the body's turn squash (0.18..1).
        public void Place(float footX, float footY, float turn)
        {
            if (_ledgeSr == null) return;
            _ledgeSr.transform.position = new Vector3(0f, LedgeTop(footY), 0.5f);
            float w = ShadowWidth * Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(turn));
            _shadowT.position = new Vector3(footX, footY - ShadowDrop, 0.4f);
            _shadowT.localScale = new Vector3(w, ShadowHeight / 0.25f, 1f);
        }

        public void Show(bool on)
        {
            if (_ledgeSr != null) _ledgeSr.enabled = on;
            if (_shadowSr != null) _shadowSr.enabled = on;
        }

        // Ledge: pivot at the top-center. Grass lip on top, soil below, fading to clear.
        public static Texture2D LedgeTexture()
        {
            var t = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[TexW * TexH];
            for (int y = 0; y < TexH; y++)
            {
                float d = 1f - (y + 0.5f) / TexH; // 0 at the top edge, 1 at the bottom
                for (int x = 0; x < TexW; x++)
                    px[y * TexW + x] = LedgePixel(x, d);
            }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        // d = depth fraction below the top edge. Exposed so tests can read the profile.
        public static Color32 LedgePixel(int x, float d)
        {
            float lip = 0.10f + 0.012f * Mathf.Sin(x * 0.9f);
            float a;
            Color c;
            if (d < lip)
            {
                c = Color.Lerp(new Color(0.44f, 0.62f, 0.24f), new Color(0.32f, 0.50f, 0.20f), d / lip);
                a = Mathf.Clamp01(d / 0.015f); // soft top edge
            }
            else if (d < lip + 0.05f)
            {
                c = new Color(0.22f, 0.30f, 0.14f); // dark seam under the grass
                a = 1f;
            }
            else
            {
                float s = Mathf.Clamp01((d - lip - 0.05f) / (1f - lip - 0.05f));
                c = Color.Lerp(new Color(0.45f, 0.32f, 0.19f), new Color(0.36f, 0.25f, 0.15f), s);
                a = Mathf.Pow(1f - s, 1.5f);
            }
            return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));
        }

        // Shadow: soft white-alpha ellipse, tinted black by the renderer.
        public static Texture2D ShadowTexture()
        {
            const int w = 64, h = 16;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        static Sprite LedgeSprite()
        {
            if (_ledge != null) return _ledge;
            var tex = LedgeTexture();
            _ledge = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), new Vector2(0.5f, 1f), TexW);
            _ledge.hideFlags = HideFlags.HideAndDontSave;
            return _ledge;
        }

        static Sprite ShadowSprite()
        {
            if (_shadow != null) return _shadow;
            var tex = ShadowTexture();
            _shadow = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            _shadow.hideFlags = HideFlags.HideAndDontSave;
            return _shadow;
        }
    }
}
