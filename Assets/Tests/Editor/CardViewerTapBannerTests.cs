#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon's iPad, 2026-10-29: in "Cards collected this round" many taps read as drags and the
    // card did not flip; and the back's name banner was bigger (two lines) than the front's.
    public static class CardViewerTapBannerTests
    {
        const string Out = "/tmp/flock-five-card-viewer-tests.txt";
        public static void Run()
        {
            var sb = new StringBuilder(); int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d; if (ok) pass++; else fail++;
                sb.AppendLine(line); File.WriteAllText(Out, sb.ToString()); Debug.Log("[cardviewer] " + line);
            }
            // (name, dpi): iPhone 18 Pro ~460, iPad A16 264, Samsung ~420, unknown 0.
            var dev = new (string, float)[] { ("iphone18pro", 460f), ("ipadA16", 264f), ("samsung", 420f), ("nodpi", 0f) };
            foreach (var (n, dpi) in dev)
            {
                float ppt = dpi > 1f ? dpi / 163f : 1f;
                float slop = CardTilt.TapSlopPx(dpi);
                Check(n + "-slop-in-points", Mathf.Abs(slop / ppt - CardTilt.TapSlopPt) < 0.01f, "px=" + slop);
                // A real tap wobbles a few points: 6 pt, quick.
                Check(n + "-6pt-quick-is-tap", !CardTilt.IsDrag(new Vector2(6f * ppt, 2f * ppt), 0.12f, dpi), "");
                // A brushed quick tap at 15 pt still flips.
                Check(n + "-15pt-quick-is-tap", !CardTilt.IsDrag(new Vector2(15f * ppt, 0f), 0.15f, dpi), "");
                Check(n + "-15pt-slow-is-drag", CardTilt.IsDrag(new Vector2(15f * ppt, 0f), 0.6f, dpi), "");
                Check(n + "-30pt-is-drag", CardTilt.IsDrag(new Vector2(0f, 30f * ppt), 0.1f, dpi), "");
            }
            // Old rule: 14 raw px. On iPad 264 ppi that was 8.6 pt; a 10 pt tap failed.
            Check("ipad-old-slop-too-small", 14f / (264f / 163f) < 9f, "");

            foreach (bool full in new[] { true })
            foreach (bool honey in new[] { false, true })
            {
                var face = new Rect(100f, 200f, full ? 820f : 300f, full ? 1150f : 420f);
                FlockFiveApp.CardText.FaceBands(face, full, false, false, out var tf, out _);
                FlockFiveApp.CardText.FaceBands(face, full, true, honey, out var tb, out var fb);
                Check("banner-same-rect full=" + full + " honey=" + honey, tf == tb, tf + " vs " + tb);
                Check("back-flavor-below-banner full=" + full + " honey=" + honey, fb.y >= tb.yMax, "");
                FlockFiveApp.CardText.CopyArgs(full, false, out int f0, out float l0, out _, out _, out _);
                FlockFiveApp.CardText.CopyArgs(full, true, out int f1, out float l1, out _, out _, out _);
                Check("banner-same-fit full=" + full, f0 == f1 && Mathf.Approximately(l0, l1), "");
            }
            sb.AppendLine("DONE pass=" + pass + " fail=" + fail); File.WriteAllText(Out, sb.ToString());
        }
    }
}
#endif
