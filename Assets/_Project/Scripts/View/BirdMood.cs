using UnityEngine;

namespace FlockFive
{
    public static class BirdMood
    {
        public struct Pose
        {
            public float BobHz;
            public float BobAmp;
            public float Tilt;
            public float Lean;
            public float Squash;
            public float Scale;
            public float HeadX;
            public float HeadY;
            public float FaceScale;
            public float BlinkEvery;
        }

        static readonly Sprite[] Faces = new Sprite[Palette.Max];

        public static Pose Of(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby:
                    return new Pose
                    {
                        BobHz = 8.4f, BobAmp = 0.028f, Tilt = 9f, Lean = -7f,
                        Squash = 0.06f, Scale = 1.04f, HeadX = 0.11f, HeadY = 0.20f,
                        FaceScale = 0.34f, BlinkEvery = 2.4f
                    };
                case BirdColor.Gold:
                    return new Pose
                    {
                        BobHz = 4.1f, BobAmp = 0.012f, Tilt = 3.2f, Lean = 5f,
                        Squash = 0.03f, Scale = 1.07f, HeadX = 0.10f, HeadY = 0.18f,
                        FaceScale = 0.32f, BlinkEvery = 3.6f
                    };
                case BirdColor.Teal:
                    return new Pose
                    {
                        BobHz = 5.6f, BobAmp = 0.020f, Tilt = 12f, Lean = 0f,
                        Squash = 0.045f, Scale = 1f, HeadX = 0.12f, HeadY = 0.19f,
                        FaceScale = 0.36f, BlinkEvery = 1.8f
                    };
                case BirdColor.Peach:
                    return new Pose
                    {
                        BobHz = 6.8f, BobAmp = 0.022f, Tilt = 6.5f, Lean = -3f,
                        Squash = 0.05f, Scale = 1.02f, HeadX = 0.10f, HeadY = 0.19f,
                        FaceScale = 0.33f, BlinkEvery = 2.8f
                    };
                default:
                    return new Pose
                    {
                        BobHz = 3.6f, BobAmp = 0.014f, Tilt = 4.5f, Lean = 6f,
                        Squash = 0.025f, Scale = 0.96f, HeadX = 0.09f, HeadY = 0.17f,
                        FaceScale = 0.30f, BlinkEvery = 4.2f
                    };
            }
        }

        public static Sprite Face(BirdColor c)
        {
            int i = (int)c;
            if (i < 0 || i >= Faces.Length) i = 0;
            if (Faces[i] == null) Faces[i] = Draw(c);
            return Faces[i];
        }

        static Sprite Draw(BirdColor c)
        {
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pix = new Color[n * n];
            for (int k = 0; k < pix.Length; k++) pix[k] = clear;

            switch (c)
            {
                case BirdColor.Ruby:
                    Eager(pix, n);
                    break;
                case BirdColor.Gold:
                    Smug(pix, n);
                    break;
                case BirdColor.Teal:
                    Curious(pix, n);
                    break;
                case BirdColor.Peach:
                    Sweet(pix, n);
                    break;
                default:
                    Dreamy(pix, n);
                    break;
            }

            tex.SetPixels(pix);
            tex.Apply();
            var spr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 220f);
            spr.name = "mood-" + c;
            return spr;
        }

        static void Eager(Color[] pix, int n)
        {
            Oval(pix, n, 30, 58, 11, 9, Color.white);
            Oval(pix, n, 58, 58, 11, 9, Color.white);
            Oval(pix, n, 33, 56, 5, 6, new Color(0.12f, 0.04f, 0.06f, 1f));
            Oval(pix, n, 61, 56, 5, 6, new Color(0.12f, 0.04f, 0.06f, 1f));
            Dot(pix, n, 35, 58, 1.6f, Color.white);
            Dot(pix, n, 63, 58, 1.6f, Color.white);
            Stroke(pix, n, 20, 72, 38, 66, 2.4f, new Color(0.18f, 0.05f, 0.08f, 1f));
            Stroke(pix, n, 76, 72, 58, 66, 2.4f, new Color(0.18f, 0.05f, 0.08f, 1f));
        }

        static void Smug(Color[] pix, int n)
        {
            Oval(pix, n, 30, 56, 12, 7, Color.white);
            Oval(pix, n, 62, 56, 12, 7, Color.white);
            Oval(pix, n, 31, 54, 7, 4.2f, new Color(0.18f, 0.10f, 0.04f, 1f));
            Oval(pix, n, 63, 54, 7, 4.2f, new Color(0.18f, 0.10f, 0.04f, 1f));
            Dot(pix, n, 34, 56, 2.1f, new Color(1f, 0.95f, 0.7f, 1f));
            Lid(pix, n, 30, 62, 13, new Color(0.22f, 0.12f, 0.05f, 1f));
            Lid(pix, n, 62, 62, 13, new Color(0.22f, 0.12f, 0.05f, 1f));
        }

        static void Curious(Color[] pix, int n)
        {
            Oval(pix, n, 30, 56, 13, 13, Color.white);
            Oval(pix, n, 64, 56, 13, 13, Color.white);
            Oval(pix, n, 34, 58, 6.5f, 6.5f, new Color(0.06f, 0.16f, 0.18f, 1f));
            Oval(pix, n, 68, 60, 6.5f, 6.5f, new Color(0.06f, 0.16f, 0.18f, 1f));
            Dot(pix, n, 36, 61, 2f, Color.white);
            Dot(pix, n, 70, 63, 2f, Color.white);
            Oval(pix, n, 30, 72, 5, 2.2f, new Color(0.08f, 0.22f, 0.24f, 0.85f));
            Oval(pix, n, 64, 73, 5, 2.2f, new Color(0.08f, 0.22f, 0.24f, 0.85f));
        }

        static void Sweet(Color[] pix, int n)
        {
            Oval(pix, n, 30, 57, 11, 10, Color.white);
            Oval(pix, n, 62, 57, 11, 10, Color.white);
            Oval(pix, n, 32, 56, 5.5f, 6f, new Color(0.28f, 0.10f, 0.12f, 1f));
            Oval(pix, n, 64, 56, 5.5f, 6f, new Color(0.28f, 0.10f, 0.12f, 1f));
            Dot(pix, n, 34, 59, 2f, new Color(1f, 0.88f, 0.82f, 1f));
            Dot(pix, n, 66, 59, 2f, new Color(1f, 0.88f, 0.82f, 1f));
            Oval(pix, n, 22, 42, 7, 4.5f, new Color(1f, 0.55f, 0.52f, 0.55f));
            Oval(pix, n, 74, 42, 7, 4.5f, new Color(1f, 0.55f, 0.52f, 0.55f));
            Stroke(pix, n, 36, 28, 48, 22, 2.1f, new Color(0.32f, 0.10f, 0.12f, 0.95f));
            Stroke(pix, n, 48, 22, 60, 28, 2.1f, new Color(0.32f, 0.10f, 0.12f, 0.95f));
        }

        static void Dreamy(Color[] pix, int n)
        {
            Oval(pix, n, 31, 55, 11, 10, Color.white);
            Oval(pix, n, 62, 55, 11, 10, Color.white);
            Oval(pix, n, 32, 53, 6, 6.5f, new Color(0.16f, 0.08f, 0.28f, 1f));
            Oval(pix, n, 63, 53, 6, 6.5f, new Color(0.16f, 0.08f, 0.28f, 1f));
            Dot(pix, n, 34, 56, 1.8f, new Color(0.92f, 0.86f, 1f, 1f));
            Dot(pix, n, 65, 56, 1.8f, new Color(0.92f, 0.86f, 1f, 1f));
            Stroke(pix, n, 22, 64, 40, 70, 1.8f, new Color(0.14f, 0.07f, 0.24f, 0.9f));
            Stroke(pix, n, 74, 64, 56, 70, 1.8f, new Color(0.14f, 0.07f, 0.24f, 0.9f));
            Dot(pix, n, 78, 74, 2.4f, new Color(1f, 0.92f, 1f, 0.95f));
        }

        static void Oval(Color[] pix, int n, float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
            int x1 = Mathf.Min(n - 1, Mathf.CeilToInt(cx + rx + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
            int y1 = Mathf.Min(n - 1, Mathf.CeilToInt(cy + ry + 1));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x - cx) / rx;
                float v = (y - cy) / ry;
                float d = u * u + v * v;
                if (d > 1.05f) continue;
                float a = d > 0.88f ? Mathf.SmoothStep(1f, 0f, (d - 0.88f) / 0.17f) : 1f;
                Blend(pix, n, x, y, new Color(col.r, col.g, col.b, col.a * a));
            }
        }

        static void Dot(Color[] pix, int n, float cx, float cy, float r, Color col) =>
            Oval(pix, n, cx, cy, r, r, col);

        static void Lid(Color[] pix, int n, float cx, float cy, float w, Color col)
        {
            Oval(pix, n, cx, cy, w, 4.2f, col);
        }

        static void Stroke(Color[] pix, int n, float x0, float y0, float x1, float y1, float thick, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f);
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)Mathf.Max(1, steps);
                float x = Mathf.Lerp(x0, x1, u);
                float y = Mathf.Lerp(y0, y1, u);
                Oval(pix, n, x, y, thick, thick * 0.7f, col);
            }
        }

        static void Blend(Color[] pix, int n, int x, int y, Color c)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return;
            int i = y * n + x;
            var d = pix[i];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0.001f) { pix[i] = d; return; }
            pix[i] = new Color(
                (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
                a);
        }
    }
}
