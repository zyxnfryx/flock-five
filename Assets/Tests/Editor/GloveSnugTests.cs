#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: the poker Back glove rode in from mid-right and crossed the whole screen. Angled starts stay snug.
    // The Back tip starts above the hand, to the right of the medal. The settled fingertip is the medal center.
    static class GloveSnugTests
    {
        public static void Run()
        {
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                Debug.Log((ok ? "[glove-snug] PASS " : "[glove-snug] FAIL ") + name + " " + detail);
                if (!ok) Debug.LogError("[glove-snug] FAIL " + name + " " + detail);
            }

            foreach (var sz in new[] { new Vector2(402, 874), new Vector2(820, 1180), new Vector2(412, 915) })
            {
                var safe = new Rect(8, 50, sz.x - 16, sz.y - 90);
                float s = 1f;
                var back = new Vector2(40, 90);
                var raw = FlockFiveApp.GloveMiddleRightEntry(safe);
                var p = FlockFiveApp.AngledStartSnug(raw, back, s, safe);
                float cap = 1.6f * FlockFiveApp.GloveDh(s) + 1f;
                float d = Vector2.Distance(p, back);
                bool ok = d <= cap && d < Vector2.Distance(raw, back) * 0.5f;
                Check("angled-start-snug-" + sz.x, ok, "dist " + d + " cap " + cap);
            }

            var keep = DeviceFit.TestWindow;
            try
            {
                foreach (var sz in new[] { new Vector2(820, 1180), new Vector2(402, 874), new Vector2(412, 915) })
                    CheckBackTip(sz, Check);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }

            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int hint = app.IndexOf("void DrawPokerBackHint(");
            int hintEnd = hint >= 0 ? app.IndexOf("public struct PokerChrome", hint) : -1;
            string hintBody = hint >= 0 && hintEnd > hint ? app.Substring(hint, hintEnd - hint) : "";
            int glove = hintBody.IndexOf("DrawTutorOverlay(");
            int tip = hintBody.IndexOf("DrawPokerStandardLine(");
            Check("back-tip-draws-above-glove", glove > 0 && tip > glove, "glove " + glove + " tip " + tip);

            int tick = app.IndexOf("bool TickPokerBackHint(");
            int tickEnd = tick >= 0 ? app.IndexOf("bool PokerBackHintLive(", tick) : -1;
            string tickBody = tick > 0 && tickEnd > tick ? app.Substring(tick, tickEnd - tick) : "";
            Check("back-glove-uses-snug-hop", tickBody.Contains("PokerBackGloveStart(") && !tickBody.Contains("GloveMiddleRightEntry("), "");
            Check("back-tip-band-clears-glove-lane",
                coach.Contains("return PokerBackTipBand(width, h, safe, s, band.height, back, aim);")
                && coach.Contains("band = PokerBackBand(band, screenH, safe, s);")
                && coach.Contains("if (!bee && !adHand && !bottomSeat)")
                && coach.Contains("_pokerBackGloveHeld"), "");

            Debug.Log("[glove-snug] " + (fail == 0 ? "PASS" : "FAIL " + fail));
        }

        static void CheckBackTip(Vector2 sz, System.Action<string, bool, string> check)
        {
            float w = sz.x, h = sz.y;
            string tag = w.ToString("0");
            DeviceFit.TestWindow = w / h;
            float s = DeviceFit.Ui(w, h);
            float m = 8f * s;
            var safe = new Rect(m, m, Mathf.Max(8f, w - 2f * m), Mathf.Max(8f, h - 2f * m));
            var screenSafe = new Rect(0f, 0f, w, h);
            var chrome = FlockFiveApp.LayoutPokerChrome(w, h, screenSafe, s);
            var aim = chrome.Back.center;
            if (chrome.Back.width < 2f || aim.y > h * 0.35f || aim.x > w * 0.4f)
                aim = new Vector2(64f, Mathf.Min(72f, h * 0.12f));

            check("back-settle-inside-hit-" + tag, chrome.Back.width > 2f && chrome.Back.Contains(aim), chrome.Back + " aim " + aim);

            int px = CaptionLayout.BannerFont(w, h);
            float bandH = CaptionLayout.BannerBandHeight(px * 2.2f, px);
            var lane = FlockFiveApp.PokerBackGloveLane(aim, s);
            var band = FlockFiveApp.PokerBackTipBand(w, h, screenSafe, s, bandH, chrome.Back, aim);
            bool above = band.yMin < lane.yMin && band.yMin < aim.y;
            bool clear = !band.Overlaps(lane) && !band.Overlaps(chrome.Back);
            bool readable = band.width >= 140f && band.height >= 48f && band.xMax <= w + 0.5f && band.yMin >= 0f;
            check("back-tip-above-glove-" + tag, above && clear && readable,
                "band " + band + " lane " + lane + " aim " + aim);

            var start = FlockFiveApp.PokerBackGloveStart(aim, s, safe, w, h);
            var old = FlockFiveApp.AngledStartSnug(FlockFiveApp.GloveMiddleRightEntry(safe), aim, s, safe);
            float dh = FlockFiveApp.GloveDh(s);
            float newD = Vector2.Distance(start, aim);
            float oldD = Vector2.Distance(old, aim);
            float ang = FlockFiveApp.GloveApproachDeg(start, aim);
            float lean = Mathf.Abs(Mathf.DeltaAngle(ang, 0f));
            float over = FlockFiveApp.GloveQuadOverflow(start, ang, false, FlockFiveApp.GloveAuditDw(dh), dh, w, h);
            // Straight up onto the medal, shorter than the old middle-right diagonal.
            bool snug = Mathf.Abs(start.x - aim.x) < 1.5f && start.y > aim.y
                && newD < oldD - 0.5f && newD >= FlockFiveApp.GloveMinStartHands * dh - 0.5f
                && lean <= 18f && over <= 0.5f;
            check("back-glove-hop-snug-" + tag, snug, "new " + newD.ToString("0.0") + " old " + oldD.ToString("0.0")
                + " ang " + ang.ToString("0.0") + " over " + over.ToString("0.0") + " start " + start);

            bool flight = FlockFiveApp.PokerBackFlightClears(start, aim, s, band)
                && FlockFiveApp.PokerBackFlightClears(aim, aim, s, band);
            check("back-glove-flight-clears-tip-" + tag, flight, "band " + band);
        }
    }
}
#endif
