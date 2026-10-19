#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b119 (Brandon): the Collection banner is three short sentences on three lines (explicit breaks, no wrap),
    // sized so each line fits on one line at the narrowest phone width, with the progress bar below the plate.
    public static class Build119CollectionBannerTests
    {
        const string Out = "/tmp/flock-five-b119-collection-banner-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build119] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("banner text is three sentences on three lines",
                src.Contains("const string HiveSubtitle = \"Find bees to earn cards.\";")
                && src.Contains("const string HiveHowTo = \"Tap a card to take a look.\\nTap again to flip.\";"), "");
            var devs = new[] { new Vector4(750f, 1334f, 40f, 0f), new Vector4(1179f, 2556f, 177f, 102f), new Vector4(1290f, 2796f, 204f, 102f), new Vector4(1080f, 2400f, 88f, 48f) };
            string[] lines = { "Find bees to earn cards.", "Tap a card to take a look.", "Tap again to flip." };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0");
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.UiH(h);
                var safe = new Rect(0f, bot, w, h - bot - top);
                FlockFiveApp.AlbumTextSpan(w, safe, s, out float l, out float r);
                var st = FlockFiveApp.CardText.MeasureStyle();
                int prevPx = st.fontSize; bool prevWrap = st.wordWrap;
                string body = string.Join("\n", lines);
                int px = FlockFiveApp.AlbumBodyPx(st, body, r - l, Mathf.Max(15, Mathf.RoundToInt(17f * s)));
                st.fontSize = px; st.wordWrap = false;
                float widest = 0f;
                foreach (var ln in lines) widest = Mathf.Max(widest, st.CalcSize(new GUIContent(ln)).x);
                float oneLine = st.CalcSize(new GUIContent(lines[0])).y;
                float h3 = st.CalcHeight(new GUIContent(body), r - l);
                st.fontSize = prevPx; st.wordWrap = prevWrap;
                Check("each line fits one line " + tag, widest <= r - l + 0.5f || px <= 9, "widest " + widest.ToString("0") + " span " + (r - l).ToString("0") + " px " + px);
                Check("height is exactly three lines " + tag, h3 < oneLine * 3.5f, "h " + h3.ToString("0") + " line " + oneLine.ToString("0"));
                float shell = FlockFiveApp.CardText.ShellBottom(w, h, safe);
                var g = FlockFiveApp.CardText.GridOf(w, h, safe, shell);
                Check("progress bar below the banner " + tag, shell > 0f && g.CardsBottom > shell - 0.01f, "shell " + shell.ToString("0"));
            }
            DeviceFit.TestWindow = null;
        }
    }
}
#endif
