#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: the 820 pillar shifts the GUI by OffX (~78px). That shift used to sit
    // inside the finger rotation, so a sideways tip landed about a row off the
    // bird (step 2 on the purple branch, ~78px left and ~78px high). The pose
    // matrix applies the shift after the rotation. Reanchor keeps the entry travel
    // while the settled end tracks the live target, on 820 and on the phones.
    static class GloveSettleTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[glove-settle] FAIL " + what); } }

            int areaW = Mathf.Min(820, Mathf.RoundToInt(1180f * DeviceFit.PhoneAspect));
            float off = (820 - areaW) * 0.5f;
            Check(areaW == 664 && Mathf.Abs(off - 78f) < 0.6f, "820 pillar off " + off + " area " + areaW);

            var gui = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
            // Step-2 lone red, screen pixels from the 820 capture, in area space.
            var pivot = new Vector2(699f - off, 468f);
            float[] angs = { 0f, -90f, 90f, -45f, 45f, -70f };
            foreach (float ang in angs)
            foreach (float sx in new[] { 1f, -1.07f })
            {
                var m = FlockFiveApp.GlovePoseMatrix(gui, pivot, ang, sx, 0.88f);
                var tip = m.MultiplyPoint3x4(new Vector3(pivot.x, pivot.y, 0f));
                Check(Mathf.Abs(tip.x - 699f) < 0.6f && Mathf.Abs(tip.y - 468f) < 0.6f,
                    "820 tip ang " + ang + " sx " + sx + " -> " + tip.x.ToString("0.0") + "," + tip.y.ToString("0.0"));
            }

            foreach (var sz in new[] { new Vector2(402f, 874f), new Vector2(412f, 915f) })
            {
                var aim = new Vector2(sz.x * 0.72f, sz.y * 0.42f);
                foreach (float ang in angs)
                {
                    var m = FlockFiveApp.GlovePoseMatrix(Matrix4x4.identity, aim, ang, -1f, 1f);
                    var tip = m.MultiplyPoint3x4(new Vector3(aim.x, aim.y, 0f));
                    Check((tip - (Vector3)aim).sqrMagnitude < 0.4f, sz + " tip " + ang + " " + tip);
                }
            }

            foreach (var sz in new[] { new Vector2(402f, 874f), new Vector2(820f, 1180f), new Vector2(412f, 915f) })
            {
                float k = sz.x / 402f;
                var live = new Vector2(100f, 345f) * k;
                var aim = new Vector2(66f, 130f) * k;
                var tip = aim + new Vector2(80f, -10f) * k;
                var travel = tip - aim;
                FlockFiveApp.GloveReanchor(ref aim, ref tip, live);
                var end = Vector2.Lerp(tip, aim, 1f);
                Check((aim - live).sqrMagnitude < 0.25f, sz + " lock " + aim);
                Check((tip - aim - travel).sqrMagnitude < 0.25f, sz + " travel changed");
                Check((end - live).sqrMagnitude < 0.25f, sz + " settle " + end);
                Check((tip - aim).sqrMagnitude > 1f, sz + " travel collapsed");
            }

            Debug.Log("[glove-settle] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
