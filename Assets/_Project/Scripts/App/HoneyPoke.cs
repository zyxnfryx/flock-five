using UnityEngine;

namespace FlockFive
{
    // A tap on a honeycomb is not a flip. The first taps leave a honey smudge
    // that fades. Later taps still squish and still do not flip.
    public static class HoneyPoke
    {
        public const int Cap = 5;
        public const float FadeSec = 1.35f;

        public struct Smudge
        {
            public Vector2 At;
            public float Born;
            public float Size;
        }

        static readonly Smudge[] _sm = new Smudge[Cap];
        static int _placed;
        static Texture2D _blob;

        public static int Placed => _placed;

        public static void Clear() => _placed = 0;

        public static Smudge At(int i) => _sm[i];

        // True when this tap stored a smudge. False once the cap is full.
        // Either way the caller plays the squish and does not flip.
        public static bool Place(float now, Vector2 at, float size)
        {
            if (_placed >= Cap) return false;
            _sm[_placed] = new Smudge { At = at, Born = now, Size = size };
            _placed++;
            return true;
        }

        public static float Alpha(float now, float born)
        {
            float u = (now - born) / FadeSec;
            if (u <= 0f) return 1f;
            if (u >= 1f) return 0f;
            return 1f - u * u;
        }

        public static void Draw(float now)
        {
            if (_placed <= 0) return;
            var tex = Blob();
            if (tex == null) return;
            for (int i = 0; i < _placed; i++)
            {
                float a = Alpha(now, _sm[i].Born);
                if (a < 0.03f) continue;
                float sz = Mathf.Max(8f, _sm[i].Size);
                var r = new Rect(_sm[i].At.x - sz * 0.5f, _sm[i].At.y - sz * 0.5f, sz, sz);
                GUI.color = new Color(0.95f, 0.72f, 0.16f, 0.78f * a);
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        static Texture2D Blob()
        {
            if (_blob != null) return _blob;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "HoneySmudge"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _blob = tex;
            return tex;
        }
    }
}
