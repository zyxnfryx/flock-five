using UnityEngine;

namespace FlockFive
{
    // Build 115, tuned in 121. The painted garden backdrop has no floor at the ostrich's
    // foot height (his feet land mid-screen, well above the stage level the other gardens
    // use), and the repo has no painted ostrich stand, so this is still a procedural
    // placeholder: a grass-lipped bank across the portrait, a shorter mound under the
    // claws, and a contact shadow that follows the feet. The mound's top edge is the
    // planted claw line. Pure sprites: no colliders, no input, nothing a tap can hit.
    // Owned by OstrichMark: it shows and hides with the ostrich (the entrance may fade
    // it in first) and goes when he flees. Other gardens never build one.
    public sealed class OstrichGround : MonoBehaviour
    {
        // Above the backdrop (-20) and life sheets (-12), below the body/legs (-3).
        // The mound sits between the bank and the shadow so the claws read on top of it.
        public const int LedgeOrder = -8;
        public const int PadOrder = -6;
        public const int ShadowOrder = -5;
        // Ledge top edge rides this far above the mesh toe line so the toes sit in the grass lip.
        public const float LedgeLift = 0.10f;
        // How far the ledge's soil runs below the toe line before it has faded out.
        // Tall enough to read as a bank between the branch rows, still clear of the bottom HUD.
        public const float LedgeDepth = 2.55f;
        // Full width of the portrait play area plus a little bleed at each side.
        public const float LedgeWidth = WorldBuilder.CamOrtho * 9f / 16f * 2f + 1.0f;
        // Mound under the claws. Pivot is the top edge, which kisses the painted claws.
        public const float PadWidth = 3.20f;
        public const float PadHeight = 0.64f;
        public const float PadKiss = 0.012f;
        public const int PadTexW = 96, PadTexH = 40;
        // Contact shadow under the feet (world units) and its darkness.
        public const float ShadowWidth = 4.05f;
        public const float ShadowHeight = 0.50f;
        public const float ShadowAlpha = 0.58f;
        public const float ShadowDrop = 0.03f;

        const int TexW = 128, TexH = 64;
        static Sprite _ledge, _shadow, _pad;

        SpriteRenderer _ledgeSr, _shadowSr, _padSr;
        Transform _shadowT, _padT;
        GardenScene _scene = GardenScene.Summer;
        float _weight = 1f;

        public SpriteRenderer Ledge => _ledgeSr;
        public SpriteRenderer Shadow => _shadowSr;
        public SpriteRenderer Pad => _padSr;
        public bool Shown => _ledgeSr != null && _ledgeSr.enabled;
        public float Weight => _weight;

        // Ledge tint per season (multiplies the grass/soil art). Summer is the reference.
        public static Color SceneTint(GardenScene scene)
        {
            switch (scene)
            {
                case GardenScene.Desert: return new Color(1.00f, 0.88f, 0.62f, 1f);  // dry sand-grass
                case GardenScene.Fall: return new Color(1.00f, 0.78f, 0.52f, 1f);    // amber leaf litter
                case GardenScene.Winter: return new Color(0.86f, 0.94f, 1.00f, 1f);  // frosty
                case GardenScene.Spring: return new Color(0.92f, 1.00f, 0.88f, 1f);  // fresh green
                default: return Color.white;
            }
        }

        public void SetScene(GardenScene scene)
        {
            _scene = scene;
            ApplyWeight();
        }

        // Entrance fade. 0 hides the stand before he arrives; 1 is the planted garden.
        public void SetWeight(float weight)
        {
            _weight = Mathf.Clamp01(weight);
            ApplyWeight();
        }

        void ApplyWeight()
        {
            var tint = SceneTint(_scene);
            tint.a = _weight;
            if (_ledgeSr != null) _ledgeSr.color = tint;
            if (_padSr != null) _padSr.color = tint;
            if (_shadowSr != null) _shadowSr.color = new Color(0f, 0f, 0f, ShadowAlpha * _weight);
        }

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

            var p = new GameObject("FootPad");
            p.transform.SetParent(transform, false);
            _padT = p.transform;
            _padSr = p.AddComponent<SpriteRenderer>();
            _padSr.sprite = PadSprite();
            FlockSort.Apply(_padSr, PadOrder);

            var s = new GameObject("ContactShadow");
            s.transform.SetParent(transform, false);
            _shadowT = s.transform;
            _shadowSr = s.AddComponent<SpriteRenderer>();
            _shadowSr.sprite = ShadowSprite();
            FlockSort.Apply(_shadowSr, ShadowOrder);
            ApplyWeight();
            Show(false);
        }

        // Ledge top edge world y for a given mesh toe line.
        public static float LedgeTop(float footY) => footY + LedgeLift;

        // Top edge of the mound. footY is the mesh toe line; the painted claws sit ClawLift above it.
        public static float PadTop(float footY) => footY + OstrichMark.ClawLift + PadKiss;

        // footX is the middle of the planted feet; turn is the body's turn squash (TurnFloor..1).
        public void Place(float footX, float footY, float turn)
        {
            if (_ledgeSr == null) return;
            _ledgeSr.transform.position = new Vector3(0f, LedgeTop(footY), 0.5f);
            float turnW = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(turn));
            if (_padT != null)
            {
                _padT.position = new Vector3(footX, PadTop(footY), 0.45f);
                _padT.localScale = new Vector3(PadWidth * turnW, PadHeight / (PadTexH / (float)PadTexW), 1f);
            }
            float w = ShadowWidth * turnW;
            _shadowT.position = new Vector3(footX, footY - ShadowDrop, 0.4f);
            _shadowT.localScale = new Vector3(w, ShadowHeight / 0.25f, 1f);
        }

        public void Show(bool on)
        {
            if (_ledgeSr != null) _ledgeSr.enabled = on;
            if (_padSr != null) _padSr.enabled = on;
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
            if (d < 0.02f)
            {
                // Lit lip, so the bank reads as a surface even against the jungle.
                c = Color.Lerp(new Color(0.72f, 0.86f, 0.40f), new Color(0.44f, 0.62f, 0.24f), d / 0.02f);
                a = Mathf.Clamp01(d / 0.006f);
            }
            else if (d < lip)
            {
                c = Color.Lerp(new Color(0.44f, 0.62f, 0.24f), new Color(0.32f, 0.50f, 0.20f), d / lip);
                a = 1f;
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
                a = Mathf.Lerp(0.92f, 0.22f, Mathf.Pow(s, 0.55f));
                if (s > 0.82f)
                {
                    float tail = Mathf.Clamp01((1f - s) / 0.18f);
                    a *= tail * tail;
                }
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

        // Mound: pivot on the top edge. Grass cap, soil below, faded sides.
        public static Color32 PadPixel(int x, int y)
        {
            float nx = Mathf.Abs((x + 0.5f) / PadTexW * 2f - 1f);
            float d = 1f - (y + 0.5f) / PadTexH;
            float lip = 0.24f;
            Color c;
            float a;
            if (d < lip)
            {
                c = Color.Lerp(new Color(0.62f, 0.80f, 0.34f), new Color(0.34f, 0.52f, 0.20f), d / lip);
                a = Mathf.Clamp01(d / 0.05f);
            }
            else
            {
                float s = (d - lip) / (1f - lip);
                c = Color.Lerp(new Color(0.42f, 0.30f, 0.16f), new Color(0.28f, 0.20f, 0.12f), s);
                a = Mathf.Pow(1f - s, 1.15f);
            }
            float side = nx < 0.70f ? 1f : Mathf.Clamp01((1f - nx) / 0.30f);
            a *= side;
            return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));
        }

        static Sprite PadSprite()
        {
            if (_pad != null) return _pad;
            var t = new Texture2D(PadTexW, PadTexH, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[PadTexW * PadTexH];
            for (int y = 0; y < PadTexH; y++)
                for (int x = 0; x < PadTexW; x++)
                    px[y * PadTexW + x] = PadPixel(x, y);
            t.SetPixels32(px);
            t.Apply(false, false);
            _pad = Sprite.Create(t, new Rect(0, 0, PadTexW, PadTexH), new Vector2(0.5f, 1f), PadTexW);
            _pad.hideFlags = HideFlags.HideAndDontSave;
            return _pad;
        }
    }
}
