#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b108: Collection page. Roomier bottom stack (cards, page tabs, footer), the right-hand title pill inset like
    // the Back medal, and the instruction box plus footer drawn as full-width banners. Base phone layout.
    public static class Build108Tests
    {
        const string Out = "/tmp/flock-five-b108-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build108] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            var devs = new[]
            {
                new Vector4(1179f, 2556f, 177f, 102f), new Vector4(1290f, 2796f, 204f, 102f), new Vector4(750f, 1334f, 40f, 0f),
                new Vector4(1080f, 2400f, 88f, 48f), new Vector4(1000f, 1786f, 24f, 20f), new Vector4(1180f, 2107f, 24f, 20f),
                new Vector4(1536f, 2048f, 48f, 40f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0") + " asp " + (w / h).ToString("0.00");
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.UiH(h);
                var safe = new Rect(0f, bot, w, h - bot - top);
                FlockFiveApp.AlbumTextSpan(w, safe, s, out float l, out float r);
                float backX = CornerAnchor.EdgeInset(safe);
                Check("text span mirrors the back inset " + tag, l >= backX - 0.01f && Mathf.Abs((w - r) - l) < 1.5f + Mathf.Abs(safe.xMin - (w - safe.xMax)),
                    "left " + l.ToString("0") + " right gap " + (w - r).ToString("0") + " back x " + backX.ToString("0"));
                var hang = CaptionLayout.BoxPad(h);
                float titlePlateRight = r + 0f;
                float titleRight = w - CornerAnchor.EdgeInsetRight(w, safe) - hang.x;
                Check("title pill inset like back " + tag, titleRight + hang.x <= w - backX + 0.01f && titleRight + hang.x <= w - 16f + 0.01f,
                    "plate right gap " + (w - (titleRight + hang.x)).ToString("0") + " vs back " + backX.ToString("0"));
                float shell = FlockFiveApp.CardText.ShellBottom(w, h, safe);
                var g = FlockFiveApp.CardText.GridOf(w, h, safe, shell);
                Check("bottom stack gaps " + tag, g.PagerGap >= 28f * g.S - 0.01f && g.TabY - g.CardsBottom >= 28f * g.S - 0.01f
                    && g.BookY - (g.TabY + g.PagerHit) >= 28f * g.S - 0.01f && g.BookY + g.PagerLab <= h - bot - 14f * g.S + 0.5f,
                    "gap " + g.PagerGap.ToString("0") + " card " + g.CardW.ToString("0") + "x" + g.CardH.ToString("0") + " footer bottom " + (g.BookY + g.PagerLab).ToString("0") + " of " + h.ToString("0"));
                Check("cards stay usable " + tag, g.CardW >= 60f && g.CardH >= 80f, g.CardW.ToString("0") + "x" + g.CardH.ToString("0"));
            }
            DeviceFit.TestWindow = null;
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("footer and body use the full-width banner", src.Contains("DrawAlbumBanner(bookR, false)") && src.Contains("DrawAlbumBanner(bodyR, true)"), "");
        }
    }
}
#endif
