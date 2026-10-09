using UnityEngine;

namespace FlockFive
{
    // Shared stroke for StampOutlined, StampReadable, StampLight, and the wordmark
    // fallback. IMGUI snaps each GUI.Label on its own (floor(v * pixelsPerPoint + 0.48)),
    // so a fractional sample (the old 0.707 diagonal) walks off the fill as the rect
    // moves by a fraction of a pixel. One snapped origin plus integer offsets cannot.
    // The offset set is the solid disc of that radius: the old 8-point ring, stepped
    // inward by 2, left holes that read as a second copy of the glyphs.
    public static class LabelRim
    {
        public const float SnapBias = 0.48f;

        // Callers top out near 16 (banner 0.16 of the point size, VIP ink clamped at 8).
        const int Built = 16;
        const int Max = 24;

        static readonly int[][] _x = new int[Max + 1][];
        static readonly int[][] _y = new int[Max + 1][];
        static readonly int[] _n = new int[Max + 1];

        static LabelRim()
        {
            for (int r = 0; r <= Built; r++) Build(r);
        }

        public static int Radius(float px)
        {
            int r = Mathf.RoundToInt(px);
            if (r < 1) return 0;
            if (r > Max) return Max;
            return r;
        }

        public static int Count(int radius)
        {
            if (radius < 1) return 0;
            if (radius > Max) radius = Max;
            if (_x[radius] == null) Build(radius);
            return _n[radius];
        }

        public static void At(int radius, int index, out int x, out int y)
        {
            x = _x[radius][index];
            y = _y[radius][index];
        }

        // Same rule as GUIUtility.RoundToPixelGrid / AlignPointToDevice.
        public static float Snap(float v, float pixelsPerPoint)
        {
            float s = pixelsPerPoint < 0.01f ? 1f : pixelsPerPoint;
            return Mathf.Floor(v * s + SnapBias) / s;
        }

        public struct Seat
        {
            public float ScreenX, ScreenY;
            public float LocalX, LocalY;
            public float Width, Height;
            public Matrix4x4 Inverse;
            public bool Simple;

            public void Local(int dx, int dy, out float x, out float y)
            {
                if (Simple)
                {
                    x = LocalX + dx;
                    y = LocalY + dy;
                    return;
                }
                Vector3 p = Inverse.MultiplyPoint3x4(new Vector3(ScreenX + dx, ScreenY + dy, 0f));
                x = p.x;
                y = p.y;
            }
        }

        // Snap the rect origin once in matrix space (the space GUI.matrix maps into).
        // Width and height stay put so every sample, including the fill, centers the
        // same way. Offsets are added in that snapped space, then mapped back.
        public static Seat Lock(Rect r, Matrix4x4 matrix, float pixelsPerPoint)
        {
            Vector3 s = matrix.MultiplyPoint3x4(new Vector3(r.x, r.y, 0f));
            float sx = Snap(s.x, pixelsPerPoint);
            float sy = Snap(s.y, pixelsPerPoint);
            bool simple = IsPureTranslation(matrix);
            float lx, ly;
            Matrix4x4 inv = Matrix4x4.identity;
            if (simple)
            {
                lx = sx - matrix.m03;
                ly = sy - matrix.m13;
            }
            else
            {
                float det = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;
                if (Mathf.Abs(det) < 1e-6f)
                {
                    simple = true;
                    lx = sx;
                    ly = sy;
                }
                else
                {
                    inv = matrix.inverse;
                    Vector3 back = inv.MultiplyPoint3x4(new Vector3(sx, sy, 0f));
                    lx = back.x;
                    ly = back.y;
                }
            }
            return new Seat
            {
                ScreenX = sx,
                ScreenY = sy,
                LocalX = lx,
                LocalY = ly,
                Width = r.width,
                Height = r.height,
                Inverse = inv,
                Simple = simple
            };
        }

        static bool IsPureTranslation(Matrix4x4 m)
        {
            return Mathf.Abs(m.m00 - 1f) < 0.0001f
                && Mathf.Abs(m.m11 - 1f) < 0.0001f
                && Mathf.Abs(m.m22 - 1f) < 0.0001f
                && Mathf.Abs(m.m01) < 0.0001f
                && Mathf.Abs(m.m02) < 0.0001f
                && Mathf.Abs(m.m10) < 0.0001f
                && Mathf.Abs(m.m12) < 0.0001f
                && Mathf.Abs(m.m20) < 0.0001f
                && Mathf.Abs(m.m21) < 0.0001f;
        }

        // Pixel centers inside a circle of radius R + 0.5. That is the solid stroke
        // of thickness R, diagonals included, with no hole a 1px stem can fall through.
        // (0,0) is the fill, drawn once by the caller.
        static void Build(int radius)
        {
            int lim = (2 * radius + 1) * (2 * radius + 1);
            int cap = lim;
            var xs = new int[cap];
            var ys = new int[cap];
            int n = 0;
            int reach = radius + 1;
            for (int y = -reach; y <= reach; y++)
            {
                for (int x = -reach; x <= reach; x++)
                {
                    if (x == 0 && y == 0) continue;
                    int d = x * x + y * y;
                    if ((d << 2) > lim) continue;
                    xs[n] = x;
                    ys[n] = y;
                    n++;
                }
            }
            _x[radius] = xs;
            _y[radius] = ys;
            _n[radius] = n;
        }
    }
}
