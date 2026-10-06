using UnityEngine;

namespace FlockFive
{
    // Square clip plus corner covers so foil stays inside the card face.
    public static class CardClip
    {
        static Texture2D _corner;

        public static void Begin(Rect face) => GUI.BeginGroup(face);

        public static void End() => GUI.EndGroup();

        public static void Corners(Rect local, Color fill, float radius)
        {
            if (radius < 2f) return;
            var tex = CornerTex();
            if (tex == null) return;
            float r = Mathf.Min(radius, Mathf.Min(local.width, local.height) * 0.5f);
            GUI.color = fill;
            Blot(new Rect(local.x, local.y, r, r), tex, 0f);
            Blot(new Rect(local.xMax - r, local.y, r, r), tex, 90f);
            Blot(new Rect(local.x, local.yMax - r, r, r), tex, -90f);
            Blot(new Rect(local.xMax - r, local.yMax - r, r, r), tex, 180f);
            GUI.color = Color.white;
        }

        static void Blot(Rect r, Texture2D tex, float ang)
        {
            var prev = GUI.matrix;
            if (Mathf.Abs(ang) > 0.1f) GUIUtility.RotateAroundPivot(ang, r.center);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
        }

        static Texture2D CornerTex()
        {
            if (_corner != null) return _corner;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "CardCorner"
            };
            var px = new Color32[n * n];
            float rad = n - 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = rad - x;
                    float dy = rad - y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(d - rad + 1.2f);
                    if (a <= 0f) continue;
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _corner = tex;
            return tex;
        }
    }

    // Even column of tiny bees beside a pulled-out card. Same size, equal gaps.
    // One draw path for both flanks: faceInward mirrors the right column so bees
    // look toward the card (sprite faces right by default).
    public static class BeeFlare
    {
        public const int Count = 5;

        public static void Draw(Rect card, bool left, float s, float alpha) =>
            DrawColumn(card, left, s, alpha, faceInward: true);

        public static void DrawColumn(Rect card, bool left, float s, float alpha, bool faceInward)
        {
            var bee = SpriteCatalog.Bee;
            if (bee == null || bee.texture == null || alpha < 0.04f) return;
            float sz = Mathf.Clamp(card.height * 0.042f, 12f * Mathf.Max(1f, s), 26f * s);
            float gap = sz * 0.62f;
            float colH = Count * sz + (Count - 1) * gap;
            float y0 = card.center.y - colH * 0.5f;
            float x = left ? card.x - sz - 8f * s : card.xMax + 8f * s;
            if (x < 6f) x = 6f;
            if (x + sz > Screen.width - 6f) x = Screen.width - 6f - sz;
            // Default art faces right. Left column already looks in; right column flips.
            bool mirror = faceInward && !left;
            float t = Time.unscaledTime;
            for (int i = 0; i < Count; i++)
            {
                float phase = i * 0.85f + (left ? 0.2f : 1.4f);
                float bob = Mathf.Sin(t * 2.1f + phase) * sz * 0.16f;
                float flit = Mathf.Sin(t * 3.4f + phase * 1.7f) * sz * 0.10f;
                var r = new Rect(x + flit, y0 + i * (sz + gap) + bob, sz, sz);
                GUI.color = new Color(1f, 0.96f, 0.82f, alpha);
                if (mirror)
                {
                    var prev = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
                    GUI.DrawTexture(r, bee.texture, ScaleMode.ScaleToFit, true);
                    GUI.matrix = prev;
                }
                else
                    GUI.DrawTexture(r, bee.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }
    }
}
