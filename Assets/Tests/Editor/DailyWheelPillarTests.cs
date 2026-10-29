#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // iPad Daily Bonus wheel (Brandon's iPad photos, 2026-10-29): in pillar mode GuiBase is a
    // pure x translate (OffX), and GUIUtility.RotateAroundPivot pivots in raw screen pixels,
    // so the disc and every label orbited a point OffX px left of the wheel: wheel shifted
    // and clipped, labels off their wedges, the wedge under the pointer not the one paid.
    // PhoneArea.RotateAround pivots in GUI space. Checks the iPhone 18 Pro, iPad (A16) and
    // Samsung offsets: the drawn wedge under the pointer must equal the paid wedge.
    public static class DailyWheelPillarTests
    {
        const string Out = "/tmp/flock-five-daily-wheel-pillar-tests.txt";

        static Matrix4x4 OldPivot(Matrix4x4 m, float deg, Vector2 p)
        {
            var t = new Vector3(p.x, p.y, 0f);
            return Matrix4x4.Translate(t) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, deg)) * Matrix4x4.Translate(-t) * m;
        }

        static int DrawnUnderPointer(Matrix4x4 rot, Matrix4x4 bas, Vector2 c, float r)
        {
            Vector2 tip = bas.MultiplyPoint3x4(c + DailyBonus.PointerTipDir * r);
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < DailyBonus.WedgeCount; i++)
            {
                Vector2 at = rot.MultiplyPoint3x4(c + DailyBonus.WedgeOffset(i, 0f, r));
                float d = (at - tip).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line); File.WriteAllText(Out, sb.ToString()); Debug.Log("[dailywheelpillar] " + line);
            }
            // (name, screen w, h). Area width follows PhoneArea: min(w, h*9/16).
            var devices = new (string, int, int)[] { ("iphone18pro", 1206, 2622), ("ipadA16", 1640, 2360), ("samsung", 1080, 2340) };
            foreach (var (name, sw, sh) in devices)
            {
                bool pillar = (float)sw / sh > DeviceFit.PhoneAspect + 0.03f;
                int w = pillar ? Mathf.Min(sw, Mathf.RoundToInt(sh * DeviceFit.PhoneAspect)) : sw;
                int off = pillar ? (sw - w) / 2 : 0;
                var bas = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
                var c = new Vector2(w * 0.5f, sh * 0.42f);
                float r = w * 0.30f;
                bool allNew = true, centerNew = true, oldWrong = false;
                for (int wedge = 0; wedge < DailyBonus.WedgeCount; wedge++)
                {
                    float ang = DailyBonus.PointerAngle(wedge) + 360f * 5f;
                    if (DailyBonus.WedgeUnderPointer(ang) != wedge) allNew = false;
                    var m = PhoneArea.RotateAboutLocal(bas, ang, c);
                    if (DrawnUnderPointer(m, bas, c, r) != wedge) allNew = false;
                    if (((Vector2)m.MultiplyPoint3x4(c) - (Vector2)bas.MultiplyPoint3x4(c)).magnitude > 0.01f) centerNew = false;
                    var o = OldPivot(bas, ang, c);
                    if (((Vector2)o.MultiplyPoint3x4(c) - (Vector2)bas.MultiplyPoint3x4(c)).magnitude > 1f) oldWrong = true;
                }
                Check(name + "-paid-equals-drawn", allNew, "off=" + off);
                Check(name + "-disc-center-fixed", centerNew, "off=" + off);
                if (pillar) Check(name + "-old-pivot-reproduces-bug", oldWrong, "off=" + off);
                var sc = PhoneArea.ScaleAboutLocal(bas, new Vector2(0.9f, 0.9f), c);
                Check(name + "-scale-pivot-fixed", ((Vector2)sc.MultiplyPoint3x4(c) - (Vector2)bas.MultiplyPoint3x4(c)).magnitude < 0.01f, "");
            }
            sb.AppendLine("DONE pass=" + pass + " fail=" + fail);
            File.WriteAllText(Out, sb.ToString());
        }
    }
}
#endif
