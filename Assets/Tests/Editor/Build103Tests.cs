#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b103: iPad control cap, restart bottom anchor, wheel labels rigid, no home avatar sparkle, poker bottom plate,
    // popup label padding, Go VIP label centred, sparkle hold keeps moving, gift tap, ad audio stall, poker back hold.
    public static class Build103Tests
    {
        const string Out = "/tmp/flock-five-b103-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build103] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            var devs = new[] { new Vector2(1179f, 2556f), new Vector2(1290f, 2796f), new Vector2(1536f, 2048f), new Vector2(2048f, 2732f), new Vector2(750f, 1334f), new Vector2(1080f, 2400f) };
            string[] tags = { "iphone", "tall-iphone", "ipad4:3", "ipad-pro", "se", "android" };
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.Ui(w, h);
                bool phone = DeviceFit.Wide(w / h) < 0.001f;
                float bot = d == 4 ? 0f : (w > h * 0.7f ? h * 0.02f : h * 0.04f);
                var safe = new Rect(0f, bot, w, h - bot - h * 0.04f);
                // Restart: phones keep the old 72-ref buffer; tablets sit on the small fixed buffer.
                CornerAnchor.Garden(w, h, safe, out float sc, out _, out _, out var rs, out var hv);
                float buffer = h - safe.yMin - rs.yMax;
                float want = (phone ? CornerAnchor.BottomMarginRef : Mathf.Lerp(CornerAnchor.BottomMarginRef, CornerAnchor.TabletBottomMarginRef, DeviceFit.Wide(w / h))) * sc;
                Check("restart bottom buffer " + tags[d], Mathf.Abs(buffer - want) < 0.5f && rs.yMax <= h - safe.yMin + 0.01f && hv.y == rs.y,
                    "buffer " + buffer.ToString("0") + " want " + want.ToString("0") + " rest " + rs.yMin.ToString("0") + ".." + rs.yMax.ToString("0"));
                // Banner still covers the restart button.
                float refH = FlockFiveApp.GardenRefBandH(w, h, safe, s, 0f);
                float by = FlockFiveApp.GardenFixedSeatY(w, h, safe, s, 0f);
                float pad = 4f * s;
                Check("banner covers moved restart " + tags[d], by <= rs.yMin - pad && by + refH >= rs.yMax + pad && by + refH <= h,
                    "band " + by.ToString("0") + ".." + (by + refH).ToString("0") + " restart " + rs.yMin.ToString("0") + ".." + rs.yMax.ToString("0"));
                // Control sizes as % of screen width (report) and phone = unchanged.
                var lvl = SplashPress.LevelRect(w, h, safe);
                float deal = FlockFiveApp.PokerActSize(w, s);
                var tab = FlockFiveApp.PokerPayTabBox(w, safe, h * 0.1f, s);
                float backSz = 101.2f * Mathf.Clamp(s, 1f, 1.35f) * DeviceFit.ControlK();
                float oldBack = 101.2f * Mathf.Clamp(s, 1f, 1.35f);
                Check("control cap " + tags[d], phone ? Mathf.Approximately(backSz, oldBack) : backSz < oldBack,
                    "level " + (lvl.width / w * 100f).ToString("0.0") + "% deal " + (deal / w * 100f).ToString("0.0") + "% paytab " + (tab.width / w * 100f).ToString("0.0")
                    + "% back " + (backSz / w * 100f).ToString("0.0") + "% k " + DeviceFit.ControlK().ToString("0.00"));
                // Poker bottom plate: below the back button and glove, inside the screen.
                float plateH = 120f * s;
                float top = FlockFiveApp.PokerBottomPlateTop(plateH, h, safe, s);
                Check("poker bottom plate seat " + tags[d], top > h * 0.5f && top + plateH <= h - safe.yMin && top > 101.2f * 1.35f + 120f * s,
                    "top " + top.ToString("0") + " bottom " + (top + plateH).ToString("0") + " h " + h.ToString("0"));
            }
            DeviceFit.TestWindow = null;

            // Go VIP label centred on the disc.
            foreach (bool priced in new[] { true, false })
            {
                var disc = new Rect(100f, 200f, 300f, 300f);
                FlockFiveApp.VipOffer.BuyLabelRects(disc, priced, out var verb, out var price);
                float top2 = verb.y, bot2 = priced ? price.yMax : verb.yMax;
                float mid = (top2 + bot2) * 0.5f;
                Check("go vip label centred " + (priced ? "priced" : "unpriced"), Mathf.Abs(mid - disc.center.y) < 0.5f, "offset " + (mid - disc.center.y).ToString("0.0"));
            }

            // Daily wheel: every label rect is identical before, during and after the award.
            {
                var wheel = new Rect(50f, 50f, 600f, 600f);
                int bad = 0;
                for (int wdg = 0; wdg < DailyBonus.WedgeCount; wdg++)
                    for (float a = 0f; a < 360f; a += 7.5f)
                    {
                        var live = FlockFiveApp.WheelWedgeLabelRect(wheel, wdg, a);
                        var fixedR = FlockFiveApp.WheelWedgeLabelRect(wheel, wdg, a, false);
                        var landed = FlockFiveApp.WheelWedgeLabelRect(wheel, wdg, a, false);
                        if (live != fixedR || live != landed) bad++;
                    }
                string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
                Check("wheel labels never move after award", bad == 0 && daily.Contains("WheelWedgeLabelRect(wheel, i, angle, false)"), "bad " + bad);
            }

            // Popup button labels keep clear padding.
            Check("popup label padding", FlockFiveApp.PopupWordPadX >= 0.10f && FlockFiveApp.PopupWordPadY >= 0.20f, "x " + FlockFiveApp.PopupWordPadX + " y " + FlockFiveApp.PopupWordPadY);

            // Sparkle hold keeps moving: positions change every step across the covered hold and the swap.
            {
                float W = 1179f, H = 2556f;
                int moving = 0, total = 0;
                float dMin = float.MaxValue;
                for (int key = 0; key < 400; key += 3)
                {
                    float lx = W * 0.5f + key, ly = H * 0.5f;
                    float t0 = PressRainbow.SettleAt(key) + 0.20f; // eased in
                    float so = PressRainbow.RevealAt;               // earliest the fall may start
                    var hist = new System.Collections.Generic.List<Vector2>();
                    for (float mt = t0; mt < so + 0.0f; mt += 1f / 60f)
                    {
                        PressRainbow.MotionClock = mt;
                        PressRainbow.LiveT = mt;
                        // The swap gate holds the SEQUENCE clock at CoverAt; drift runs on the free clock.
                        float seq = Mathf.Min(mt, PressRainbow.CoverAt);
                        if (seq < PressRainbow.SettleAt(key) + 0.001f) { hist.Clear(); continue; }
                        if (!PressRainbow.Flight(key, lx, ly, 90f, false, seq, W, H, W * 0.5f, H * 0.8f, out float x, out float y, out _, out _, out _, out _)) { hist.Clear(); continue; }
                        hist.Add(new Vector2(x, y));
                        if (hist.Count >= 3)
                        {
                            total++;
                            float dd = (hist[hist.Count - 1] - hist[hist.Count - 3]).magnitude;
                            if (dd > 0.01f) moving++;
                            if (dd < dMin) dMin = dd;
                        }
                    }
                }
                PressRainbow.LiveT = -1f;
                Check("sparkles keep moving through the covered hold", total > 50 && moving >= total * 0.98f, "steps " + total + " moving " + moving + " minStep " + dMin.ToString("0.000"));
            }

            // Gift tap: honoured tap clears the hint for good.
            {
                float until = 5f; bool resume = true, nudged = false;
                FlockFiveApp.ClearGiftHint(ref until, ref resume, ref nudged);
                string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
                int hide = coach.IndexOf("CoachHideNow();\n                // b103");
                int clear = coach.IndexOf("ClearGiftHint(ref _coachGiftUntil");
                Check("gift tap ends the hint, glove cannot return", until < 0f && !resume && nudged && hide > 0 && clear > hide, "clear after hide " + (clear > hide));
            }

            // Ad audio: the stall timeout never releases a displayed ad; award during the ad keeps it silent; restore is pre-ad only.
            {
                Check("ad stall limit", Ads.StallLimitFor(true) > AdAudioHold.SafetySeconds && Mathf.Approximately(Ads.StallLimitFor(false), Ads.StallLimit),
                    "shown " + Ads.StallLimitFor(true) + " notShown " + Ads.StallLimitFor(false));
                Ads.ForceClear();
                AudioListener.pause = false;
                bool pre = AudioListener.pause;
                Ads.BeginShow();
                bool silent1 = MixDesk.MasterGain == 0f && AudioListener.pause && Ads.IsShowing;
                Ads.DeliverRewardedCallback(); // award during the ad
                bool silent2 = MixDesk.MasterGain == 0f && AudioListener.pause && Ads.IsShowing;
                Ads.AdvanceStageGap(100f);
                AudioGuard.Restore(); // focus-return style restore under an open ad
                bool silent3 = MixDesk.MasterGain == 0f && AudioListener.pause && Ads.IsShowing;
                Ads.EndShow();
                bool restored = !Ads.IsShowing && MixDesk.MasterGain > 0f && AudioListener.pause == pre;
                Check("ad duck/restore state machine (award during ad)", silent1 && silent2 && silent3 && restored,
                    silent1 + "/" + silent2 + "/" + silent3 + "/" + restored);
                // Pre-ad user mix comes back exactly: music muted and a half slider stay that way.
                AudioGuard.MusicEnabled = false; AudioGuard.MasterVolume = 0.5f;
                Ads.BeginShow(); Ads.DeliverRewardedCallback(); Ads.EndShow();
                bool keptMute = AudioGuard.MusicScale == 0f && Mathf.Approximately(MixDesk.MasterGain, 0.5f);
                AudioGuard.MusicEnabled = true; AudioGuard.MasterVolume = 1f;
                Check("restore returns the pre-ad mix only", keptMute, "musicScale muted kept, master 0.5");
                AudioListener.pause = false;
                Ads.ForceClear();
            }

            // Poker back: held while a hand is dealt and during the first-visit lesson, open at the result step.
            {
                Check("poker back held while hand dealt", FlockFiveApp.PokerHandHoldsBack(BirdPoker.Phase.Dealt)
                    && !FlockFiveApp.PokerHandHoldsBack(BirdPoker.Phase.Idle) && !FlockFiveApp.PokerHandHoldsBack(BirdPoker.Phase.Drawn), "");
                Check("poker lesson holds back until result step",
                    FlockFiveApp.PokerLessonHolds(true, true, false, false, false)
                    && !FlockFiveApp.PokerLessonHolds(true, true, false, true, false)   // final step: glove on Back
                    && !FlockFiveApp.PokerLessonHolds(true, true, true, false, false)   // already done
                    && !FlockFiveApp.PokerLessonHolds(true, true, false, false, true)   // broke: never trapped
                    && !FlockFiveApp.PokerLessonHolds(true, false, false, false, false), "");
            }
        }
    }
}
#endif
