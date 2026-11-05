#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Daily wheel on a fresh save: wedge labels stay on their wedges (GUI-space pivots,
    // so an iPad pillar offset cannot orbit them), and a pop scale keeps the panel center.
    // 402 / 412 are phones (no pillar). 820 is the iPad width the layout is judged on.
    public static class DailyClaimWheelTests
    {
        const string Out = "/tmp/flock-five-daily-claim-wheel-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[daily-claim-wheel] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            var devs = new[] { (402, 874), (412, 915), (820, 1180), (804, 1748), (824, 1830), (1640, 2360) };
            foreach (var (sw, sh) in devs)
            {
                bool pillar = (float)sw / sh > DeviceFit.PhoneAspect + 0.03f;
                int w = pillar ? Mathf.Min(sw, Mathf.RoundToInt(sh * DeviceFit.PhoneAspect)) : sw;
                float off = pillar ? (sw - w) * 0.5f : 0f;
                var bas = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
                float d = w * 0.78f;
                var wheel = new Rect((w - d) * 0.5f, sh * 0.35f, d, d);
                string tag = sw + "x" + sh + (pillar ? " pillar" : " phone");
                float rim = d * 0.5f;
                foreach (float spin in new[] { 0f, 13f, 22.5f, 97f, 200f })
                {
                    var centers = new Vector2[DailyBonus.WedgeCount];
                    bool inside = true, ownWedge = true, placed = true;
                    string why = "";
                    for (int i = 0; i < DailyBonus.WedgeCount; i++)
                    {
                        var lab = FlockFiveApp.WheelWedgeLabelRect(wheel, i, spin, false);
                        var rigid = FlockFiveApp.WheelWedgeLabelRect(wheel, i, spin, true);
                        // Award used to pull the landed label inward. It must not.
                        if (lab.width < rigid.width - 0.5f || (lab.center - wheel.center).magnitude + 0.5f < (rigid.center - wheel.center).magnitude)
                        {
                            placed = false;
                            why = "inset w" + i;
                        }
                        float deg = DailyBonus.WedgeLabelDegrees(i, spin);
                        var m = PhoneArea.RotateAboutLocal(bas, deg, lab.center);
                        var want = bas.MultiplyPoint3x4(wheel.center + DailyBonus.WedgeOffset(i, spin, (lab.center - wheel.center).magnitude));
                        Vector2 got = m.MultiplyPoint3x4(lab.center);
                        if ((got - (Vector2)want).magnitude > 0.5f)
                        {
                            placed = false;
                            why = "pivot " + (got - (Vector2)want);
                        }
                        centers[i] = got;
                        var cs = new[]
                        {
                            new Vector2(lab.xMin, lab.yMin), new Vector2(lab.xMax, lab.yMin),
                            new Vector2(lab.xMin, lab.yMax), new Vector2(lab.xMax, lab.yMax)
                        };
                        foreach (var c in cs)
                        {
                            Vector2 p = (Vector2)m.MultiplyPoint3x4(c) - (Vector2)bas.MultiplyPoint3x4(wheel.center);
                            if (p.magnitude > rim)
                            {
                                inside = false;
                                why = "rim " + p.magnitude.ToString("0.0");
                            }
                            float a = Mathf.Atan2(p.x, -p.y) * Mathf.Rad2Deg;
                            float mid = i * DailyBonus.WedgeArc + spin;
                            if (Mathf.Abs(Mathf.DeltaAngle(a, mid)) > DailyBonus.WedgeArc * 0.5f + 0.5f)
                            {
                                ownWedge = false;
                                why = "wedge " + i;
                            }
                        }
                    }
                    Check("label on its wedge " + tag + " spin " + spin, placed && inside && ownWedge, why);
                    float minGap = float.MaxValue;
                    for (int i = 0; i < centers.Length; i++)
                        for (int j = i + 1; j < centers.Length; j++)
                            minGap = Mathf.Min(minGap, (centers[i] - centers[j]).magnitude);
                    Check("labels not stacked " + tag + " spin " + spin, minGap > d * 0.12f, minGap.ToString("0"));
                }

                var card = new Rect(w * 0.08f, sh * 0.2f, w * 0.84f, sh * 0.6f);
                foreach (float k in new[] { 0.6f, 0.9f, 1.1f })
                {
                    var m = PhoneArea.ScaleAboutLocal(bas, new Vector2(k, k), card.center);
                    Vector2 c = m.MultiplyPoint3x4(card.center);
                    Vector2 want = bas.MultiplyPoint3x4(card.center);
                    var wheelM = PhoneArea.RotateAboutLocal(m, 40f, wheel.center);
                    Vector2 disc = wheelM.MultiplyPoint3x4(wheel.center);
                    Vector2 discWant = m.MultiplyPoint3x4(wheel.center);
                    Check("disc stays on panel center " + tag + " k " + k,
                        (c - want).magnitude < 0.5f && (disc - discWant).magnitude < 0.5f,
                        "card " + (c - want) + " disc " + (disc - discWant));
                }
            }

            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int wheelFn = daily.IndexOf("void DrawDailyWheel(");
            int pointerFn = daily.IndexOf("void DrawWheelPointer(");
            string wheelBody = wheelFn >= 0 && pointerFn > wheelFn ? daily.Substring(wheelFn, pointerFn - wheelFn) : "";
            int radialFn = daily.IndexOf("void DrawRadialWheelLabel(");
            string radialBody = radialFn >= 0 && wheelFn > radialFn ? daily.Substring(radialFn, wheelFn - radialFn) : "";
            Check("wheel draw uses GUI-space pivots",
                wheelBody.Contains("PhoneArea.RotateAround(angle, wheel.center)")
                && wheelBody.Contains("DrawRadialWheelLabel(lab, text, DailyBonus.WedgeLabelDegrees(i, angle), jack)")
                && wheelBody.Contains("WheelWedgeLabelRect(wheel, i, angle, false)")
                && !wheelBody.Contains("StampOutlined(lab,")
                && !wheelBody.Contains("GUIUtility.RotateAroundPivot")
                && radialBody.Contains("PhoneArea.RotateAround(deg, lab.center)"),
                "len " + wheelBody.Length);
            var face = new Rect(40f, 80f, 200f, 140f);
            var word = FlockFiveApp.DailyClaimWordCenter(face);
            var wordBox = new Rect(face.x, face.y + face.height * 0.22f, face.width, face.height * 0.56f);
            // 820 pillar: OffX is horizontal. The fingertip must stay on the GUI aim
            // after that shift, at every approach angle. Phones are OffX 0.
            var aim = new Vector2(331f, 844f);
            foreach (float ang in new[] { -120f, -90f, -40f, 0f, 70f, 140f, 180f })
            {
                foreach (float off in new[] { 0f, 78f })
                {
                    var gui = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
                    var m = FlockFiveApp.GloveDrawMatrix(aim, ang, -1.07f, 0.88f, gui);
                    Vector2 tip = m.MultiplyPoint3x4(aim);
                    Vector2 want = aim + new Vector2(off, 0f);
                    Check("glove tip keeps pillar shift outside the rotation ang " + ang + " off " + off,
                        (tip - want).magnitude < 0.5f, tip.ToString() + " want " + want);
                }
            }
            Check("claim glove aims at the Claim word",
                (word - wordBox.center).magnitude < 0.01f
                && face.Contains(word)
                && word.x > face.x + face.width * 0.4f,
                word.ToString());
            // Phone GUI width and the iPad pillar's phone-aspect width. The arc is
            // GUI space; GuiBase (OffX) is applied outside the pose, so a traveling
            // tip and the landed tip both keep that shift horizontal.
            foreach (var (gw, gh, off) in new[] { (402f, 874f, 0f), (664f, 1180f, 78f) })
            {
                var safe = new Rect(0f, 0f, gw, gh);
                var wordAim = new Vector2(gw * 0.5f, gh * 0.72f);
                float dh = FlockFiveApp.GloveDh(1f);
                var perched = wordAim + new Vector2(-12f, -6f);
                var start = FlockFiveApp.ClaimGloveStart(perched, wordAim, dh, safe);
                float apart = (start - wordAim).magnitude;
                float arch = dh * 0.55f;
                var p0 = FlockFiveApp.ClaimGloveArc(start, wordAim, 0f, arch);
                var mid = FlockFiveApp.ClaimGloveArc(start, wordAim, 0.45f, arch);
                var landed = FlockFiveApp.ClaimGloveArc(start, wordAim, 1f, arch);
                var chord = Vector2.Lerp(start, wordAim, 0.45f);
                Check("claim travel start mid and land differ w " + gw,
                    apart > 70f
                    && (p0 - start).magnitude < 0.5f
                    && (mid - start).magnitude > 28f
                    && (mid - wordAim).magnitude > 28f
                    && (landed - wordAim).magnitude < 0.5f
                    && mid.y < chord.y - 12f
                    && !FlockFiveApp.ClaimGloveTravelDone(0f)
                    && FlockFiveApp.ClaimGloveTravelDone(0.82f),
                    "apart " + apart.ToString("0") + " mid " + mid + " land " + landed);
                foreach (var pose in new[] { p0, mid, landed })
                {
                    var gui = Matrix4x4.Translate(new Vector3(off, 0f, 0f));
                    var drawn = FlockFiveApp.GloveDrawMatrix(pose, -40f, -1.07f, 0.88f, gui);
                    Vector2 tip = drawn.MultiplyPoint3x4(pose);
                    Vector2 wantTip = pose + new Vector2(off, 0f);
                    Check("claim arc tip keeps pillar shift outside rotation w " + gw + " off " + off,
                        (tip - wantTip).magnitude < 0.5f, tip + " want " + wantTip);
                }
            }
            int pinAt = coach.IndexOf("void PinClaimGloveTip()");
            int pinEnd = pinAt >= 0 ? coach.IndexOf("public const float ClaimGloveMinHands", pinAt) : -1;
            string pinBody = pinAt >= 0 && pinEnd > pinAt ? coach.Substring(pinAt, pinEnd - pinAt) : "";
            Check("claim pin holds the word and leaves the arc start",
                pinBody.Contains("_gloveShown = _gloveLockAim")
                && !pinBody.Contains("_gloveTip = _gloveLockAim")
                && coach.Contains("ClaimGloveArc(")
                && coach.Contains("if (_claimGloveArrived && _gloveLockOn && string.Equals(_gloveStepLine, DailyClaimLine))"),
                "pin " + pinBody.Length);
            Check("claim glove drops the rail fade and stays through the tap",
                coach.Contains("void DropRailFadeForClaim(")
                && coach.Contains("if (enteredClaim)")
                && coach.Contains("void PinClaimGloveTip()")
                && coach.Contains("_gloveShown = _gloveLockAim")
                && daily.Contains("bool HoldGloveOnClaimTap(")
                && daily.Contains("if (_dailyGloveThroughTap) return 0;")
                && daily.Contains("FinishClaimGloveTap();"),
                "");
            string end = fail == 0 ? "DAILY_CLAIM_WHEEL_OK" : "DAILY_CLAIM_WHEEL_FAIL " + fail;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[daily-claim-wheel] " + end);
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
