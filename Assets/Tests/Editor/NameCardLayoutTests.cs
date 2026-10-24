#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Name card ("Please give me a name!"). The plate used to reserve a fixed 18px hang
    // and then paint BoxPad a second time, so on a phone the dark card ran off both edges.
    // FitAvatarNameCard applies that hang once, inside shared gutters, on the area the GUI
    // actually uses (full glass on a phone, the centered 9:16 pillar on iPad).
    public static class NameCardLayoutTests
    {
        const string Out = "/tmp/flock-five-name-card-layout-tests.txt";

        [MenuItem("Flock Five/Name Card Layout Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string info = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + (info.Length > 0 ? "  (" + info + ")" : ""));
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[name-card] " + (ok ? "PASS " : "FAIL ") + name + (info.Length > 0 ? "  " + info : ""));
            }

            float? keep = DeviceFit.TestWindow;
            try
            {
                var devs = new[]
                {
                    new { tag = "iphone", w = 1179f, h = 2556f },
                    new { tag = "se", w = 750f, h = 1334f },
                    new { tag = "android", w = 1080f, h = 2400f },
                    new { tag = "ipad", w = 1536f, h = 2048f },
                };
                float prevPlate = -1f;
                foreach (var d in devs)
                {
                    float area = AreaW(d.w, d.h);
                    DeviceFit.TestWindow = area / d.h;
                    float s = DeviceFit.Ui(area, d.h);
                    var hang = CaptionLayout.BoxPad(d.h);
                    // Full-bleed channel: the rail gap plus bleed used to reach the glass.
                    var fit = FlockFiveApp.FitAvatarNameCard(area, 0f, area, s, 0f, area, area, hang.x);
                    float gutter = 12f * s;
                    Check(d.tag + " plate inside glass",
                        fit.PlateX >= -0.6f && fit.PlateR <= area + 0.6f,
                        "plate " + fit.PlateX.ToString("0.0") + ".." + fit.PlateR.ToString("0.0") + " area " + area.ToString("0"));
                    Check(d.tag + " plate inside gutter",
                        fit.PlateX >= fit.EdgeL - 0.6f && fit.PlateR <= fit.EdgeR + 0.6f,
                        "edges " + fit.EdgeL.ToString("0.0") + ".." + fit.EdgeR.ToString("0.0"));
                    Check(d.tag + " gutter scales",
                        fit.EdgeL >= gutter - 0.6f && area - fit.EdgeR >= gutter - 0.6f,
                        "edge " + fit.EdgeL.ToString("0.0") + " want " + gutter.ToString("0.0"));
                    float frac = (fit.PlateR - fit.PlateX) / area;
                    Check(d.tag + " snug not a sliver",
                        frac >= 0.75f && frac <= 0.96f,
                        "frac " + frac.ToString("0.000"));
                    Check(d.tag + " single hang",
                        Mathf.Abs((fit.PlateR - fit.PlateX) - (fit.InnerW + hang.x * 2f)) < 0.6f,
                        "plateW " + (fit.PlateR - fit.PlateX).ToString("0.0") + " inner " + fit.InnerW.ToString("0.0") + " pad " + hang.x.ToString("0.0"));

                    float oldLeft = OldPaintLeft(area, hang.x);
                    Check(d.tag + " old hang ran off the glass", oldLeft < -4f, "oldLeft " + oldLeft.ToString("0.0"));

                    if (d.tag == "ipad")
                    {
                        Check("ipad lays out in the phone pillar",
                            area < d.w - 100f && fit.PlateR <= area + 0.6f,
                            "pillar " + area.ToString("0") + " tablet " + d.w.ToString("0"));
                    }
                    if (prevPlate > 0f)
                        Check(d.tag + " width is not a hardcoded phone size",
                            Mathf.Abs((fit.PlateR - fit.PlateX) - prevPlate) > 8f, "");
                    prevPlate = fit.PlateR - fit.PlateX;
                }

                // A requested content width narrower than the glass stays centered, still inside.
                DeviceFit.TestWindow = 1179f / 2556f;
                float phoneS = DeviceFit.Ui(1179f, 2556f);
                float phonePad = CaptionLayout.BoxPad(2556f).x;
                var narrow = FlockFiveApp.FitAvatarNameCard(1179f, 0f, 1179f, phoneS, 0f, 1179f, 420f, phonePad);
                Check("narrow request stays narrow", narrow.InnerW <= 420.5f && narrow.InnerW >= 400f,
                    "inner " + narrow.InnerW.ToString("0.0"));
                Check("narrow request centered",
                    Mathf.Abs((narrow.PlateX + narrow.PlateR) * 0.5f - 1179f * 0.5f) < 1.5f, "");
                Check("narrow request inside",
                    narrow.PlateX >= narrow.EdgeL - 0.6f && narrow.PlateR <= narrow.EdgeR + 0.6f, "");

                // Rails leave a middle channel. The card stays in that channel when it fits.
                var channel = FlockFiveApp.FitAvatarNameCard(1179f, 0f, 1179f, phoneS, 280f, 900f, 1179f, phonePad);
                Check("channel card inside channel hang",
                    channel.InnerX >= 280f - 0.6f && channel.InnerX + channel.InnerW <= 900.6f,
                    "inner " + channel.InnerX.ToString("0.0") + ".." + (channel.InnerX + channel.InnerW).ToString("0.0"));
                Check("channel plate inside glass",
                    channel.PlateX >= channel.EdgeL - 0.6f && channel.PlateR <= channel.EdgeR + 0.6f, "");

                // A collapsed rail gap opens to the gutters instead of drawing a zero-width card.
                var collapsed = FlockFiveApp.FitAvatarNameCard(1179f, 0f, 1179f, phoneS, 80f, 80f, 1179f, phonePad);
                Check("collapsed channel opens",
                    collapsed.InnerW > 400f && collapsed.PlateX >= collapsed.EdgeL - 0.6f && collapsed.PlateR <= collapsed.EdgeR + 0.6f,
                    "inner " + collapsed.InnerW.ToString("0.0"));

                // Side safe inset (wider than the scale gutter) pushes the plate in.
                float safe = 90f;
                var inset = FlockFiveApp.FitAvatarNameCard(1179f, safe, 1179f - safe, phoneS, 0f, 1179f, 1179f, phonePad);
                Check("safe inset clears the glass edge",
                    inset.PlateX >= safe + 8f - 0.6f && inset.PlateR <= 1179f - safe - 8f + 0.6f,
                    "plate " + inset.PlateX.ToString("0.0") + ".." + inset.PlateR.ToString("0.0"));

                // An empty safe rect must not shove the card off one side.
                var empty = FlockFiveApp.FitAvatarNameCard(1179f, 0f, 0f, phoneS, 0f, 1179f, 1179f, phonePad);
                Check("empty safe stays symmetric",
                    Mathf.Abs(empty.EdgeL - (1179f - empty.EdgeR)) < 0.6f && empty.PlateX >= empty.EdgeL - 0.6f,
                    "edges " + empty.EdgeL.ToString("0.0") + ".." + empty.EdgeR.ToString("0.0"));
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }

            sb.Insert(0, "NameCardLayoutTests pass " + pass + " fail " + fail + "\n");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[name-card] pass " + pass + " fail " + fail);
        }

        // What the GUI is laid out in. Matches PhoneArea: phones keep the glass,
        // a window wider than 9:16 uses a centered phone-aspect pillar.
        static float AreaW(float sw, float sh)
        {
            if (sh < 1f) return sw;
            if (sw / sh <= DeviceFit.PhoneAspect + 0.03f) return sw;
            return Mathf.Min(sw, Mathf.Round(sh * DeviceFit.PhoneAspect));
        }

        // The old name card: clamp as if the plate hung 18px, then paint
        // (inner + 18 - 2*pad) because draw subtracted 18 and CoachPanelRect added pad again.
        static float OldPaintLeft(float area, float padX)
        {
            float edgeL = 8f;
            float edgeR = area - 8f;
            float width = Mathf.Max(80f, edgeR - edgeL);
            float innerW = width;
            float maxInner = (edgeR - edgeL) - 36f;
            if (maxInner < 80f) maxInner = Mathf.Max(40f, (edgeR - edgeL) - 8f);
            if (innerW > maxInner) innerW = maxInner;
            float x = edgeL + (width - innerW) * 0.5f;
            if (x - 18f < edgeL) x += edgeL - (x - 18f);
            if (x + innerW + 18f > edgeR) x -= (x + innerW + 18f) - edgeR;
            return x + 18f - 2f * padX;
        }
    }
}
#endif
