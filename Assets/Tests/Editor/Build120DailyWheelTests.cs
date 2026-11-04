#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R3 (Readability, 820 pillar): one label per wedge, inside the rim and its own wedge,
    // drawn where the wedge is in GUI space (pivot in GUI space, not raw screen px), and the
    // Claim pop scales about the panel center so the wheel does not slide off-center.
    public static class Build120DailyWheelTests
    {
        const string Out = "/tmp/flock-five-b120-daily-wheel-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-wheel] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // 402, 412 portrait (no pillar) and 820 iPad (pillar OffX) at dpr 2.
            var devs = new[] { (804, 1748), (824, 1830), (1640, 2360) };
            foreach (var (sw, sh) in devs)
            {
                bool pillar = (float)sw / sh > DeviceFit.PhoneAspect + 0.03f;
                int w = pillar ? Mathf.Min(sw, Mathf.RoundToInt(sh * DeviceFit.PhoneAspect)) : sw;
                float off = pillar ? (sw - w) * 0.5f : 0f;
                var bas = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
                float d = w * 0.78f;
                var wheel = new Rect((w - d) * 0.5f, sh * 0.35f, d, d);
                string tag = sw + "x" + sh;
                float rim = d * 0.5f;
                foreach (float spin in new[] { 0f, 13f, 22.5f, 97f, 200f })
                {
                    var centers = new Vector2[DailyBonus.WedgeCount];
                    bool inside = true, ownWedge = true, placed = true;
                    for (int i = 0; i < DailyBonus.WedgeCount; i++)
                    {
                        var lab = FlockFiveApp.WheelWedgeLabelRect(wheel, i, spin);
                        float deg = DailyBonus.WedgeLabelDegrees(i, spin);
                        var m = PhoneArea.RotateAboutLocal(bas, deg, lab.center);
                        var want = bas.MultiplyPoint3x4(wheel.center + DailyBonus.WedgeOffset(i, spin, (lab.center - wheel.center).magnitude));
                        Vector2 got = m.MultiplyPoint3x4(lab.center);
                        if ((got - (Vector2)want).magnitude > 0.5f) placed = false;
                        centers[i] = got;
                        // Rotated label corners stay inside the rim and inside the wedge's arc.
                        var cs = new[] { new Vector2(lab.xMin, lab.yMin), new Vector2(lab.xMax, lab.yMin), new Vector2(lab.xMin, lab.yMax), new Vector2(lab.xMax, lab.yMax) };
                        foreach (var c in cs)
                        {
                            Vector2 p = (Vector2)m.MultiplyPoint3x4(c) - (Vector2)bas.MultiplyPoint3x4(wheel.center);
                            if (p.magnitude > rim) inside = false;
                            float a = Mathf.Atan2(p.x, -p.y) * Mathf.Rad2Deg;
                            float mid = i * DailyBonus.WedgeArc + spin;
                            if (Mathf.Abs(Mathf.DeltaAngle(a, mid)) > DailyBonus.WedgeArc * 0.5f + 0.5f) ownWedge = false;
                        }
                    }
                    Check("label on its wedge (GUI-space pivot) " + tag + " spin " + spin, placed, "");
                    Check("labels inside rim " + tag + " spin " + spin, inside, "");
                    Check("label inside own wedge " + tag + " spin " + spin, ownWedge, "");
                    float minGap = float.MaxValue;
                    for (int i = 0; i < centers.Length; i++)
                        for (int j = i + 1; j < centers.Length; j++)
                            minGap = Mathf.Min(minGap, (centers[i] - centers[j]).magnitude);
                    Check("no stacked labels " + tag + " spin " + spin, minGap > d * 0.12f, minGap.ToString("0"));
                }
                // Claim pop: scaling about the card center keeps the card center fixed on screen.
                var card = new Rect(w * 0.08f, sh * 0.2f, w * 0.84f, sh * 0.6f);
                foreach (float k in new[] { 0.6f, 0.9f, 1.1f })
                {
                    var m = PhoneArea.ScaleAboutLocal(bas, new Vector2(k, k), card.center);
                    Vector2 c = m.MultiplyPoint3x4(card.center);
                    Vector2 want = bas.MultiplyPoint3x4(card.center);
                    Check("pop keeps center " + tag + " k " + k, (c - want).magnitude < 0.5f, (c - want).ToString());
                }
            }
        }
    }
}
#endif
