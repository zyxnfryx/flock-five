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

            // ---- Second pass: tap gate, multiplier cap, gear inset, bee captions, honeycomb smudge ----
            {
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                string coachSrc = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
                // Global tap gate: off-target taps die while a lesson glove is fresh; the glove target, the gear and no-lesson pass.
                Check("global gate truth table",
                    !FlockFiveApp.GloveFocusAllows(true, true, false, false)
                    && FlockFiveApp.GloveFocusAllows(true, true, true, false)
                    && FlockFiveApp.GloveFocusAllows(true, true, false, true)
                    && FlockFiveApp.GloveFocusAllows(false, true, false, false)
                    && FlockFiveApp.GloveFocusAllows(true, false, false, false), "");
                Check("global gate wired (rect gate, hit gate, HUD)",
                    coachSrc.Contains("GateGlove") && coachSrc.Contains("GloveFocusAllows(true, true, inside") && app.Contains("CueGloveOwnsHud() &&"), "");
                // Exact gift step: its own seat, tap still reaches HandleTap/OpenBonus (HUD guard only covers restart/hive), glove stays cleared.
                bool giftCaption = CoachTap.Caption(CoachStep.GiftStuck) == "Stuck? Tap the gift\nbranch for a bonus."
                    && !FlockFiveApp.IsGardenBottomLine(CoachTap.Caption(CoachStep.GiftStuck));
                int guardAt = app.IndexOf("CueGloveOwnsHud() &&", System.StringComparison.Ordinal);
                string guardLine = guardAt > 0 ? app.Substring(guardAt, 160) : "";
                Check("gift step: tap opens ad offer, glove does not return",
                    giftCaption && guardLine.Contains("restart.Contains(gui)") && guardLine.Contains("hive.Contains(gui)")
                    && coachSrc.Contains("ClearGiftHint") && coachSrc.Contains("CoachReject"), "gift tap is not a HUD rect");
            }
            // Multiplier badge cap: percent of screen width of a reference badge (28% of width) per device.
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, win = w / devs[d].y;
                bool phone = DeviceFit.Wide(win) < 0.001f;
                var badge = new Rect(0f, 0f, w * 0.28f, w * 0.28f);
                var capped = StreakBadge.Cap(badge, DeviceFit.ControlK(win));
                bool ok = phone ? capped == badge : capped.width < badge.width * 0.9f && Mathf.Abs(capped.center.x - badge.center.x) < 0.01f;
                Check("multiplier badge cap " + tags[d], ok, (capped.width / w * 100f).ToString("0.0") + "% of width (was 28.0%)");
            }
            // Settings gear: tablet-only extra corner inset.
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                DeviceFit.TestWindow = w / h;
                bool phone = DeviceFit.Wide(w / h) < 0.001f;
                float inset = FlockFiveApp.SettingsGearTabletInset(h, w / h);
                var safe = new Rect(0f, 0f, w, h);
                var g = FlockFiveApp.SettingsGearDrawnRect(w, h, safe);
                float baseX = FlockFiveApp.SettingsGearMargin(h);
                Check("gear inset " + tags[d], phone ? (inset == 0f && Mathf.Abs(g.x - baseX) < 0.01f) : (inset > 8f && Mathf.Abs(g.x - (baseX + inset)) < 0.01f),
                    inset.ToString("0.0") + "px = " + (inset / w * 100f).ToString("0.00") + "% of width");
            }
            DeviceFit.TestWindow = null;
            // Bees-on-a-branch caption: exact text, two lines max, bottom seat table; hive-garden banner lowered.
            {
                string bee = CaptionLayout.BeePestLine.Replace("\n", " ");
                bool exactTwo = CaptionLayout.BeePestLine == "Bees on a branch!\nRemove birds to clear";
                bool lines = true; string lineNote = "";
                for (int d = 0; d < devs.Length; d++)
                    lines = lines && CaptionLayout.BeePestLine.Split('\n').Length <= 2; // banner fit to two rows per device: Build88CaptionTests line-5
                Check("bees on a branch caption", bee == "Bees on a branch! Remove birds to clear" && exactTwo && lines && FlockFiveApp.IsGardenBottomLine(CaptionLayout.BeePestLine)
                    && !FlockFiveApp.IsGardenBottomLine(CaptionLayout.HiveGardenLine), bee + " lines>2 " + lineNote + " bottomSeat " + FlockFiveApp.IsGardenBottomLine(CaptionLayout.BeePestLine));
                Check("hive garden caption lowered", CaptionLayout.BeeGardenDropFrac >= 0.03f, CaptionLayout.BeeGardenDropFrac.ToString());
            }
            // Honeycomb smudge: the comb pad runs before the card flip pad, caps, fades; hits sit inside the card face.
            {
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                int poke = app.IndexOf("PokeHoneycombs(big, ix)", System.StringComparison.Ordinal);
                int flip = app.IndexOf("BeginHiveFlip(ix);", poke, System.StringComparison.Ordinal);
                Check("smudge runs before flip and blocks it", poke > 0 && flip > poke && app.Contains("!honeyAte && !PageSwipeAte"), "");
                HoneyPoke.Clear();
                bool five = true;
                for (int i = 0; i < HoneyPoke.Cap; i++) five &= HoneyPoke.Place(1f + i, new Vector2(5f, 5f), 20f);
                Check("smudge cap and fade", five && !HoneyPoke.Place(9f, Vector2.zero, 20f) && HoneyPoke.Alpha(1f + HoneyPoke.FadeSec, 1f) < 0.001f, "");
                HoneyPoke.Clear();
                var card = new Rect(100f, 100f, 600f, 840f);
                var cells = new Rect[5];
                int n = FlockFiveApp.CardText.HoneyBand(FlockFiveApp.CardText.FaceOf(card), 5, true, cells);
                bool inside = n == 5;
                for (int i = 0; i < n; i++) inside &= card.Contains(cells[i].center) && cells[i].width > 4f;
                Check("honeycomb cells found on card back", inside, "n " + n);
            }

            // Album lessons: enlarge popup is centred and clear; glove comes right to left; flip glove sits low under the text.
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                DeviceFit.TestWindow = w / h;
                float bot = d == 4 ? 0f : h * 0.03f;
                var safe = new Rect(0f, bot, w, h - bot - h * 0.04f);
                var card = FlockFiveApp.AlbumGridRect(w, h, safe, 4);
                var pager = new Rect(w * 0.2f, h * 0.86f, w * 0.6f, h * 0.05f);
                var p1 = FlockFiveApp.PoseLesson(1, card, w, h, "Tap a card you have\nto enlarge it.", 0, pager);
                bool centred = Mathf.Abs(p1.Plate.center.x - w * 0.5f) < 2f;
                bool clear = !p1.Plate.Overlaps(card) && !p1.Plate.Overlaps(p1.Glove) && !p1.Plate.Overlaps(pager);
                Check("enlarge popup centred and clear " + tags[d], centred && clear && p1.Aim.x > card.center.x, "plateY " + p1.Plate.y.ToString("0") + " aimX>card " + (p1.Aim.x > card.center.x) + " card" + card + " G" + p1.Glove + " plate" + p1.Plate + " aim" + p1.Aim);
                var insp = FlockFiveApp.AlbumInspectRect(w, h);
                var p2 = FlockFiveApp.PoseLesson(2, insp, w, h, "Tap again to flip.", 0, pager);
                float ink = p2.Title.yMax;
                if (p2.Body.width > 2f) ink = Mathf.Max(ink, p2.Body.y + p2.Body.height * 0.6f);
                if (p2.Flavor.width > 2f) ink = Mathf.Max(ink, p2.Flavor.y + p2.Flavor.height * 0.6f);
                bool low = p2.Aim.y >= insp.y + insp.height * 0.55f && p2.Aim.y >= ink && !p2.Glove.Overlaps(p2.Title) && p2.Glove.yMin >= ink - 0.3f * p2.Glove.height && p2.Approach == -90f;
                Check("flip glove low under text " + tags[d], low, "aimY " + p2.Aim.y.ToString("0") + " card " + insp.y.ToString("0") + ".." + insp.yMax.ToString("0") + " clear " + p2.Clear + " T" + p2.Title + " B" + p2.Body + " F" + p2.Flavor + " G" + p2.Glove + " P" + p2.Plate);
            }
            DeviceFit.TestWindow = null;
            {
                string app2 = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                Check("album lesson with zero cards is escapable", app2.Contains("void NoteEmptyAlbumTap") && app2.Contains("AdvanceAlbumTutor(3);"), "empty-book sleeve tap advances");
                Check("honeycomb text has a gap before the flip hint", CaptionLayout.AlbumHoneyBlock.Contains("rarity.\n\nTap to flip!") && app2.Contains("t > 0.4f && !_albumTutorOn"), "");
            }

            // Album header: plate clear of the Back medal (medal already scaled by ControlK); equal padding around the pager.
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.UiH(h);
                float size = Mathf.Max(44f, 101.2f * Mathf.Clamp(s, 1f, 1.35f) * DeviceFit.ControlK(w / h));
                var back = new Rect(16f, 40f, size, size);
                float hx = FlockFiveApp.AlbumHeaderX(back, s, h);
                float plateX = hx - CaptionLayout.BoxPad(h).x;
                Check("album header clear of back medal " + tags[d], plateX - back.xMax >= Mathf.Max(12f * s, back.width * 0.12f) - 0.01f, "gap " + (plateX - back.xMax).ToString("0.0") + "px");
                float bot = d == 4 ? 0f : h * 0.03f;
                var safe = new Rect(0f, bot, w, h - bot - h * 0.04f);
                var grid = FlockFiveApp.CardText.GridOf(w, h, safe, h * 0.2f);
                float above = grid.TabY - grid.CardsBottom, below = grid.BookY - (grid.TabY + grid.PagerHit);
                Check("pager padding equal above and below " + tags[d], Mathf.Abs(above - below) < 0.5f && above >= 10f && grid.BookY + grid.PagerLab <= h - safe.yMin, "above " + above.ToString("0.0") + " below " + below.ToString("0.0"));
            }
            DeviceFit.TestWindow = null;

            // Round card carousel: touch only. No auto-advance, release stays near the finger, no awkward overlap, dots follow the centre.
            {
                string app3 = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                Check("carousel has no auto-advance", !app3.Contains("HiveCarouselStep") && !app3.Contains("_levelHiveAuto >="), "");
                float pos = FlockFiveApp.HiveReleasePos(0f, -60f, 100f, 4); // finger dragged left 0.6 card: cards move to 0.6
                Check("carousel release stays where left", Mathf.Abs(pos - 0.6f) < 0.001f && FlockFiveApp.HiveReleasePick(pos, 4) == 1
                    && FlockFiveApp.HiveReleasePick(FlockFiveApp.HiveReleasePos(2f, 30f, 100f, 4), 4) == 2 && FlockFiveApp.HiveReleasePick(FlockFiveApp.HiveReleasePos(0f, 60f, 100f, 4), 4) == 3, pos.ToString("0.00"));
                float stride = FlockFiveApp.HiveRestStride(100f);
                Check("carousel neighbours do not overlap", stride >= 100f * (0.5f + FlockFiveApp.HiveNeighborScale * 0.5f), "stride " + stride);
                Check("carousel dots follow centre card", app3.Contains("bool on = i == centerIx;"), "");
            }

            // Home avatar bird + tag never overlap the right rail column (rail present or absent).
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                float s = DeviceFit.UiH(h);
                float icon = FlockFiveApp.HomeAvatarBirdPx(h);
                float railW = Mathf.Max(60f, 0.12f * w), railX = w - railW - 12f * s;
                int bad = 0, shifted = 0;
                for (int slots = 0; slots <= 3; slots++)
                    for (float by = h * 0.35f; by < h * 0.8f; by += h * 0.02f)
                    {
                        var col = slots == 0 ? default : new Rect(railX, h * 0.45f, railW, slots * railW * 1.2f);
                        var anchor = new Vector2(w - 0.22f * w, by);
                        var bird = new Rect(anchor.x - icon * 0.5f, anchor.y - icon * 0.5f, icon, icon);
                        var plate = new Rect(anchor.x - icon * 0.4f, anchor.y + icon * 0.5f, icon * 0.8f, 30f * s);
                        float dx = FlockFiveApp.AvatarRailShift(bird, plate, col, 8f * s);
                        if (dx > 0f) shifted++;
                        bird.x -= dx; plate.x -= dx;
                        if (col.width > 2f && (bird.Overlaps(col) || plate.Overlaps(col))) bad++;
                        if (col.width < 2f && dx != 0f) bad++;
                    }
                Check("avatar bird clear of right rail " + tags[d], bad == 0, "shifted cases " + shifted + " icon " + icon.ToString("0") + "px");
            }
        }
    }
}
#endif
