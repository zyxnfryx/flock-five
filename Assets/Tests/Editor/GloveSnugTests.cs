#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: the poker Back glove rode in from mid-right and crossed the whole screen. Angled starts stay snug.
    // Brandon: the Back tip sits on the bottom (it may cover WIN and the bet controls), draws above the glove,
    // and the glove hop stays in the top corner so the flight does not cross that tip.
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
            Check("back-tip-band-is-bottom-seat",
                coach.Contains("return PokerBackBottomBand(band, h, safe);")
                && coach.Contains("band = PokerBackBand(band, screenH, safe, s);")
                && coach.Contains("if (!bee && !adHand && !bottomSeat)"), "");

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

            int px = CaptionLayout.BannerFont(w, h);
            float bandH = CaptionLayout.BannerBandHeight(px * 2.2f, px);
            var band = FlockFiveApp.PokerBackBottomBand(new Rect(0f, 0f, w, bandH), h, screenSafe);
            float floor = h - 34f - 4f;
            check("back-tip-bottom-" + tag, band.y > h * 0.5f && Mathf.Abs(band.yMax - floor) < 1.5f && band.width >= w - 1f,
                band.ToString());

            float btn = FlockFiveApp.PokerActSize(w, s);
            var bet = new Rect(16f, h - 12f - btn, w * 0.55f, btn);
            check("back-tip-may-cover-bet-" + tag, band.Overlaps(bet), "band " + band + " bet " + bet);
            var pinned = band;
            FlockFiveApp.ClampGardenBand(ref band, w, h, screenSafe);
            check("back-tip-not-lifted-off-bet-" + tag, Mathf.Abs(band.y - pinned.y) < 0.5f, band.ToString());

            var start = FlockFiveApp.PokerBackGloveStart(aim, s, safe, w, h);
            var old = FlockFiveApp.AngledStartSnug(FlockFiveApp.GloveMiddleRightEntry(safe), aim, s, safe);
            float dh = FlockFiveApp.GloveDh(s);
            float newD = Vector2.Distance(start, aim);
            float oldD = Vector2.Distance(old, aim);
            float ang = FlockFiveApp.GloveApproachDeg(start, aim);
            float lean = Mathf.Abs(Mathf.DeltaAngle(ang, -90f));
            float over = FlockFiveApp.GloveQuadOverflow(start, ang, false, FlockFiveApp.GloveAuditDw(dh), dh, w, h);
            // Level with Back (the old middle-right line was ~40° off horizontal). On the glass,
            // so the face lock does not walk the hop back across the page.
            bool snug = start.x > aim.x && newD < oldD - 0.5f && newD >= FlockFiveApp.GloveMinStartHands * dh - 0.5f
                && lean <= 32f && over <= 0.5f;
            check("back-glove-hop-snug-" + tag, snug, "new " + newD.ToString("0.0") + " old " + oldD.ToString("0.0")
                + " ang " + ang.ToString("0.0") + " over " + over.ToString("0.0"));

            float newLow = FlightBottom(start, aim, s);
            float oldLow = FlightBottom(old, aim, s);
            bool clears = FlockFiveApp.PokerBackFlightClears(start, aim, s, band) && newLow < band.y - CaptionLayout.GloveBannerGap * s && newLow < oldLow - 1f;
            check("back-glove-flight-clears-tip-" + tag, clears, "newLow " + newLow.ToString("0.0") + " oldLow " + oldLow.ToString("0.0") + " bandY " + band.y.ToString("0.0"));
        }

        // Lowest glove edge along the locked straight hop, dip up and down. GUI y grows downward.
        static float FlightBottom(Vector2 start, Vector2 aim, float s)
        {
            float dh = FlockFiveApp.GloveDh(s);
            float ang = FlockFiveApp.GloveApproachDeg(start, aim);
            float low = 0f;
            for (int i = 0; i <= 4; i++)
            {
                var p = Vector2.Lerp(start, aim, i / 4f);
                float a = FlockFiveApp.GloveAuditBox(p, ang, dh, 0f).yMax;
                float b = FlockFiveApp.GloveAuditBox(p, ang, dh, 1f).yMax;
                if (a > low) low = a;
                if (b > low) low = b;
            }
            return low;
        }
    }
}
#endif
