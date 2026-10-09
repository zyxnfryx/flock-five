#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 73. Outline samples are one integer disc, and the fill shares their snapped origin.
    static class Build73Tests
    {
        [MenuItem("Flock Five/Build 73 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build73] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckDisc(1, Check);
            CheckDisc(2, Check);
            CheckDisc(5, Check);
            CheckDisc(7, Check);
            CheckDisc(8, Check);

            bool round = Has(2, 2, 1) && Has(2, 2, 0) && Has(2, 1, 1) && !Has(2, 2, 2);
            Check("r2-round", round, "axis and (2,1) in, (2,2) out");
            Check("r1-corners",
                Count(1) == 8 && Has(1, 1, 1) && Has(1, 1, 0) && !Has(1, 0, 0),
                "count=" + Count(1));
            int n8 = Count(8);
            Check("r8-dense", n8 > 100 && n8 < 17 * 17, "count=" + n8);

            var id = Matrix4x4.identity;
            var a = LabelRim.Lock(new Rect(10.10f, 20.10f, 180.4f, 48.2f), id, 1f);
            var b = LabelRim.Lock(new Rect(10.40f, 20.40f, 180.4f, 48.2f), id, 1f);
            var c = LabelRim.Lock(new Rect(10.60f, 20.60f, 180.4f, 48.2f), id, 1f);
            bool same = a.ScreenX == b.ScreenX && a.ScreenY == b.ScreenY
                && Mathf.Abs(a.ScreenX - 10f) < 0.001f
                && Mathf.Abs(a.ScreenY - 20f) < 0.001f;
            bool step = Mathf.Abs(c.ScreenX - 11f) < 0.001f && Mathf.Abs(c.ScreenY - 21f) < 0.001f;
            ScreenOf(a, id, 0, 0, out float afx, out float afy);
            ScreenOf(a, id, 3, -2, out float aox, out float aoy);
            bool delta = Mathf.Abs(aox - afx - 3f) < 0.001f && Mathf.Abs(aoy - afy - (-2f)) < 0.001f
                && Mathf.Abs(a.Width - 180.4f) < 0.001f;
            Check("snap-identity", same && step && delta,
                "ab=(" + a.ScreenX + "," + a.ScreenY + ") c=(" + c.ScreenX + "," + c.ScreenY
                + ") outline=(" + aox + "," + aoy + ")");

            var pivot = new Vector2(80f, 40f);
            const float k = 1.13f;
            var scale = Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(k, k, 1f))
                * Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
            var moved = Matrix4x4.TRS(new Vector3(3.25f, -7.6f, 0f), Quaternion.identity, Vector3.one) * scale;
            var seat = LabelRim.Lock(new Rect(12.37f, 40.81f, 200f, 64f), moved, 1f);
            ScreenOf(seat, moved, 0, 0, out float fx, out float fy);
            ScreenOf(seat, moved, 5, -3, out float ox, out float oy);
            bool grid = Mathf.Abs(fx - LabelRim.Snap(fx, 1f)) < 0.001f
                && Mathf.Abs(fy - LabelRim.Snap(fy, 1f)) < 0.001f;
            bool locked = Mathf.Abs(ox - fx - 5f) < 0.02f && Mathf.Abs(oy - fy - (-3f)) < 0.02f;
            bool once = Mathf.Abs(seat.ScreenX - fx) < 0.02f && Mathf.Abs(seat.ScreenY - fy) < 0.02f;
            Check("snap-scale", grid && locked && once,
                "fill=(" + fx.ToString("0.00") + "," + fy.ToString("0.00")
                + ") out=(" + ox.ToString("0.00") + "," + oy.ToString("0.00") + ")");

            var hi = LabelRim.Lock(new Rect(10.30f, 8.20f, 90f, 28f), id, 2f);
            ScreenOf(hi, id, 0, 0, out float hx, out float hy);
            ScreenOf(hi, id, 4, 1, out float hox, out float hoy);
            bool half = Mathf.Abs(hx * 2f - Mathf.Round(hx * 2f)) < 0.001f
                && Mathf.Abs(hy * 2f - Mathf.Round(hy * 2f)) < 0.001f
                && Mathf.Abs(hox - hx - 4f) < 0.001f
                && Mathf.Abs(hoy - hy - 1f) < 0.001f;
            float again = LabelRim.Snap(hx, 2f);
            Check("snap-ppp2", half && Mathf.Abs(again - hx) < 0.001f,
                "fill=(" + hx + "," + hy + ") out=(" + hox + "," + hoy + ")");

            Check("radius-zero", LabelRim.Count(0) == 0 && LabelRim.Radius(0.2f) == 0, "empty");

            Debug.Log(fail == 0
                ? "build73 tests DONE " + pass + "/" + (pass + fail)
                : "build73 tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static void CheckDisc(int radius, System.Action<string, bool, string> check)
        {
            int n = Count(radius);
            int side = radius * 2 + 5;
            int origin = side / 2;
            var seen = new bool[side, side];
            bool dup = false;
            bool sym = true;
            bool inside = true;
            bool range = true;
            for (int i = 0; i < n; i++)
            {
                LabelRim.At(radius, i, out int x, out int y);
                if (x == 0 && y == 0) inside = false;
                if (x < -radius - 1 || x > radius + 1 || y < -radius - 1 || y > radius + 1) range = false;
                int gx = x + origin;
                int gy = y + origin;
                if (gx < 0 || gy < 0 || gx >= side || gy >= side) { range = false; continue; }
                if (seen[gx, gy]) dup = true;
                seen[gx, gy] = true;
                if (!Has(radius, -x, y) || !Has(radius, x, -y) || !Has(radius, -x, -y)
                    || !Has(radius, y, x) || !Has(radius, -y, x) || !Has(radius, y, -x) || !Has(radius, -y, -x))
                    sym = false;
            }
            int lim = (2 * radius + 1) * (2 * radius + 1);
            bool gaps = false;
            int expect = 0;
            for (int y = -radius - 1; y <= radius + 1; y++)
            {
                for (int x = -radius - 1; x <= radius + 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    int d = x * x + y * y;
                    if ((d << 2) > lim) continue;
                    expect++;
                    if (!seen[x + origin, y + origin]) gaps = true;
                }
            }
            bool ok = !dup && sym && inside && range && !gaps && n == expect && n > 0;
            check("disc-r" + radius, ok,
                "n=" + n + " expect=" + expect + " dup=" + dup + " sym=" + sym + " gaps=" + gaps);
        }

        static int Count(int radius) => LabelRim.Count(radius);

        static bool Has(int radius, int x, int y)
        {
            int n = Count(radius);
            for (int i = 0; i < n; i++)
            {
                LabelRim.At(radius, i, out int ax, out int ay);
                if (ax == x && ay == y) return true;
            }
            return false;
        }

        static void ScreenOf(LabelRim.Seat seat, Matrix4x4 matrix, int dx, int dy, out float x, out float y)
        {
            seat.Local(dx, dy, out float lx, out float ly);
            Vector3 s = matrix.MultiplyPoint3x4(new Vector3(lx, ly, 0f));
            x = s.x;
            y = s.y;
        }
    }
}
#endif
