using UnityEngine;

namespace FlockFive
{
    // A tap on a honeycomb is not a flip. The first taps leave a honey smudge
    // that fades. Later taps still squish and still do not flip.
    public static class HoneyPoke
    {
        // b109 (Brandon): the old store was a fixed 5 that never recycled, so after five pokes the comb ignored
        // every tap and the smudges faded in 1.35 s. Now a ring of 10: every tap lands, the oldest is
        // overwritten, and a smudge fades by how many taps ago it was left (about ten taps to gone).
        public const int Cap = 10;
        public const float FadeSec = 1.35f; // legacy single-smudge time fade, kept for the cell flash only

        public struct Smudge
        {
            public Vector2 At;
            public float Born;
            public float Size;
            public int Serial;
        }

        static readonly Smudge[] _sm = new Smudge[Cap];
        static int _placed;
        static int _head;
        static int _serial;
        static Texture2D _blob;

        public static int Placed => _placed;
        public static int Serial => _serial;

        // The drag latch ("this press already poked cell N") lives here so Clear() (card put away, restart) drops it too.
        public static int LastCell = -1;
        public static void Clear() { _placed = 0; _head = 0; _serial = 0; LastCell = -1; }
        public static void PressStart() { LastCell = -1; }
        // True when a drag into cell i should poke (first entry of this press).
        public static bool DragInto(int i) { if (LastCell == i) return false; LastCell = i; return true; }
        // True when a release in cell i should poke. False when the drag already poked this cell. Always unlatches.
        public static bool Release(int i) { bool skip = LastCell == i; LastCell = -1; return !skip; }

        public static Smudge At(int i) => _sm[i];

        // Always stores. The caller plays the squish and does not flip.
        public static bool Place(float now, Vector2 at, float size)
        {
            _serial++;
            _sm[_head] = new Smudge { At = at, Born = now, Size = size, Serial = _serial };
            _head = (_head + 1) % Cap;
            if (_placed < Cap) _placed++;
            return true;
        }

        // 1 for the newest tap, falling to 0 as ten newer taps land.
        public static float AgeAlpha(int serial) => Mathf.Clamp01(1f - (_serial - serial) / (float)Cap);

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
                float a = AgeAlpha(_sm[i].Serial);
                if (a < 0.03f) continue;
                float sz = Mathf.Max(8f, _sm[i].Size);
                var c = _sm[i].At;
                // Honey-coloured fingerprint: amber pad, darker ridge ring, bright whorl and a glint.
                var pad = new Rect(c.x - sz * 0.5f, c.y - sz * 0.58f, sz, sz * 1.16f);
                GUI.color = new Color(0.98f, 0.66f, 0.10f, 0.78f * a);
                GUI.DrawTexture(pad, tex, ScaleMode.StretchToFill, true);
                for (int k = 0; k < 3; k++)
                {
                    float f = 0.82f - k * 0.22f;
                    var ring = new Rect(c.x - sz * 0.5f * f, c.y - sz * 0.58f * f, sz * f, sz * 1.16f * f);
                    GUI.color = new Color(k % 2 == 0 ? 0.62f : 1f, k % 2 == 0 ? 0.34f : 0.80f, k % 2 == 0 ? 0.04f : 0.26f, 0.55f * a);
                    GUI.DrawTexture(ring, tex, ScaleMode.StretchToFill, true);
                }
                var hot = new Rect(c.x - sz * 0.26f, c.y - sz * 0.36f, sz * 0.30f, sz * 0.24f);
                GUI.color = new Color(1f, 0.93f, 0.60f, 0.70f * a);
                GUI.DrawTexture(hot, tex, ScaleMode.ScaleToFit, true);
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
                    float lobe = 0.07f * Mathf.Sin(dx * 5.5f + dy * 2f);
                    float d = Mathf.Sqrt(dx * dx * 0.86f + (dy + lobe) * (dy + lobe) * 1.12f);
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
