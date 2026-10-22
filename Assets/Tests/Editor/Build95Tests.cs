#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b95: Daily popup seated once (no recenter after the first spin), Go VIP sheen rests between
    // sweeps, LEVEL idle rim glimmers gone (press burst kept), plane idle 15s / 40s.
    public static class Build95Tests
    {
        const string Out = "/tmp/flock-five-b95-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build95] " + line);
            }
            float? keep = DeviceFit.TestWindow;
            try
            {
                // Daily popup: every chrome (claim flower, wheel only, collect, collect + watch) seats the
                // card at the same x/y/width, so nothing recenters after the first spin.
                var faces = new[] { new Vector2(1179f, 2556f), new Vector2(1536f, 2048f) };
                string[] tags = { "iphone", "ipad" };
                for (int d = 0; d < faces.Length; d++)
                {
                    DeviceFit.TestWindow = faces[d].x / faces[d].y;
                    float s = DeviceFit.Ui(faces[d].x, faces[d].y);
                    var c0 = FlockFiveApp.DailyAuditCard(s, 0);
                    bool same = true;
                    string det = "";
                    for (int c = 1; c <= 3; c++)
                    {
                        var r = FlockFiveApp.DailyAuditCard(s, c);
                        if (Mathf.Abs(r.x - c0.x) > 0.01f || Mathf.Abs(r.y - c0.y) > 0.01f || Mathf.Abs(r.width - c0.width) > 0.01f) same = false;
                        det += " c" + c + "=" + r.y.ToString("0.0");
                    }
                    Check("daily card fixed across chromes " + tags[d], same, "c0=" + c0.y.ToString("0.0") + det);
                }

                // Go VIP / shared sheen: a full sweep, then a rest of 1.5..2.5 s of u 0 (invisible).
                float period = PushButton.SheenPeriod, pause = PushButton.SheenPauseSeconds;
                Check("sheen pause in range", pause >= 1.5f && pause <= 2.5f, pause.ToString());
                Check("sheen sweeps then rests", Mathf.Abs(PushButton.SheenU(period * 0.5f) - 0.5f) < 0.01f
                    && PushButton.SheenU(period + pause * 0.5f) == 0f
                    && PushButton.SheenU(period + pause - 0.01f) == 0f, "");
                Check("sheen repeats", Mathf.Abs(PushButton.SheenU(period + pause + period * 0.5f) - 0.5f) < 0.01f, "");
                Check("rest is invisible", PushButton.OvalFade(PushButton.SheenU(period + 0.1f)) == 0f, "");

                // LEVEL idle rim glimmers removed; press burst code intact.
                string wow = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.PressWow.cs");
                int idle = wow.IndexOf("static void DrawLevelIdleLife(");
                int idleEnd = wow.IndexOf("float _levelPressAt", idle);
                string idleBody = idle >= 0 && idleEnd > idle ? wow.Substring(idle, idleEnd - idle) : "";
                Check("no idle comets/twinkles/catch-light", idleBody.IndexOf("OnFlatRing") < 0 && idleBody.IndexOf("DrawAt(") < 0
                    && wow.IndexOf("LevelOrbitSpeed") < 0 && wow.IndexOf("LevelRimAng") < 0, "");
                Check("press burst intact", wow.IndexOf("static void DrawLevelPressOver(") > 0 && wow.IndexOf("DrawLevelPressUnder(") > 0
                    && File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs").IndexOf("SparkleFx.DrawAround(rest, 0.95f, true, 0.12f)") > 0, "");
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                Check("level shimmer has no glints", app.IndexOf("DrawFlowerShimmer(visual, 0f, 1f, false)") > 0, "");

                // Garden banner at the bottom: when it seats, it is under the lowest row and above the
                // restart / hive controls, on iPhone, iPad and SE; when it does not fit it returns false.
                var sizes = new[] { new Vector2(1179f, 2556f), new Vector2(1536f, 2048f), new Vector2(750f, 1334f) };
                string[] stags = { "iphone", "ipad", "se" };
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
                    restart = CornerAnchor.GardenBottomSeat(w, h, safe); // b131: banner seat (Restart itself moved to the top bar)
                    int fit = 0, noFit = 0, bad = 0;
                    for (float rowFrac = 0.55f; rowFrac <= 0.90f; rowFrac += 0.05f)
                        for (float bandH = 90f * s; bandH <= 260f * s; bandH += 40f * s)
                        {
                            float rowBottom = h * rowFrac;
                            bool ok = FlockFiveApp.BottomBannerSeat(bandH, rowBottom, restart, hive, h, safe.yMin, 8f * s, out float y);
                            if (!ok) { noFit++; continue; }
                            fit++;
                            var band = new Rect(0f, y, w, bandH);
                            if (band.yMin < rowBottom || band.Overlaps(restart) || band.Overlaps(hive) || band.yMax > h) bad++;
                        }
                    Check("bottom banner never touches row/restart/hive " + stags[d], bad == 0, "fit " + fit + " nofit " + noFit + " bad " + bad);
                }

                // Coins banner over the Level face: top edge just under LEVEL, so Easy is fully covered.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    var disc = FlockFiveApp.LevelDiscAudit(w, h, safe);
                    float lettersBottom = FlockFiveApp.LevelLettersBottom(disc);
                    float y = FlockFiveApp.CoinsBannerY(w, h, safe, s, 0f, -1f);
                    Check("coins banner top just under LEVEL, over Easy " + stags[d], y >= lettersBottom && y <= lettersBottom + 3f * s && y < disc.yMax - disc.height * 0.2f,
                        "y " + y.ToString("0") + " lettersBottom " + lettersBottom.ToString("0") + " discBottom " + disc.yMax.ToString("0"));
                }

                // Welcome card: a card ending inside the LEVEL word is lengthened to just under it
                // (Easy stays exposed); a card well above or below is untouched.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    var lvl = SplashPress.LayoutLevel(w, h, safe).Rect;
                    var disc = FlockFiveApp.LevelDiscAudit(w, h, safe);
                    float lb = FlockFiveApp.LevelLettersBottom(disc);
                    float stackTop = disc.y + (disc.height - disc.height * 0.56f) * 0.68f;
                    var short1 = new Rect(w * 0.05f, stackTop - 300f * s, w * 0.9f, 300f * s + 6f * s);
                    var grown = FlockFiveApp.CoverLevelWord(short1, lvl, s);
                    var far = new Rect(w * 0.05f, lvl.y - 500f * s, w * 0.9f, 200f * s);
                    var same = FlockFiveApp.CoverLevelWord(far, lvl, s);
                    Check("welcome card covers LEVEL word, not Easy " + stags[d], grown.yMax >= lb && grown.yMax <= lb + 3f * s && same.yMax == far.yMax && grown.yMin == short1.yMin,
                        "grown " + grown.yMax.ToString("0") + " lb " + lb.ToString("0"));
                }

                // Level 1 review: the tutorial pickup target on the top row sits ~3% of screen height under
                // the banner's bottom edge. The tip must stay on that row (drop well under a row pitch,
                // 5.8% of height) and the hand must clear the banner, on every layout.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    float dh = FlockFiveApp.GloveAuditDh(s);
                    var plate = new Rect(0f, h * 0.234f, w, h * 0.12f);
                    var target = new Vector2(w * 0.78f, plate.yMax + h * 0.03f);
                    var aim = FlockFiveApp.GardenAim(target, plate, s, out float deg);
                    float drop = aim.y - target.y;
                    var box = FlockFiveApp.GloveAuditBox(aim, deg, dh, 0f);
                    var box1 = FlockFiveApp.GloveAuditBox(aim, deg, dh, 1f);
                    bool clear = CaptionLayout.GloveClearsBanner(box, plate) && CaptionLayout.GloveClearsBanner(box1, plate);
                    float oldAbove = FlockFiveApp.GloveAuditAbove(s);
                    float oldDrop = Mathf.Max(0f, plate.yMax + CaptionLayout.GloveBannerGap * s + oldAbove - target.y);
                    Check("level1 top row: tip stays on its row, glove clear " + stags[d], drop <= h * 0.02f && clear,
                        "drop " + drop.ToString("0") + " old " + oldDrop.ToString("0") + " row " + (h * 0.058f).ToString("0") + " deg " + deg + " clear " + clear);
                }

                // b98 standard approach: horizontal where it clears the banner, custom only where it fails.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var plate = new Rect(0f, h * 0.234f, w, h * 0.12f);
                    int horiz = 0, custom = 0, bad = 0;
                    for (float y = plate.yMax - 20f * s; y < h * 0.9f; y += h * 0.02f)
                        for (int xi = 0; xi < 6; xi++)
                        {
                            var t = new Vector2(w * (0.10f + 0.80f * xi / 5f), y);
                            if (FlockFiveApp.GardenHorizontalClear(t, plate, s, w))
                            {
                                horiz++;
                                bool mirror = t.x >= w * 0.5f;
                                var box = FlockFiveApp.GloveAuditBox(t, mirror ? 90f : -90f, FlockFiveApp.GloveAuditDh(s), 0f);
                                if (!CaptionLayout.GloveClearsBanner(box, plate)) bad++;
                            }
                            else
                            {
                                custom++;
                                var a2 = FlockFiveApp.GardenAim(t, plate, s, out float dg);
                                var b2 = FlockFiveApp.GloveAuditBox(a2, dg, FlockFiveApp.GloveAuditDh(s), 0f);
                                if (!CaptionLayout.GloveClearsBanner(b2, plate)) bad++;
                            }
                        }
                    Check("garden glove: horizontal standard, custom only on conflict " + stags[d], bad == 0 && horiz > custom, "horiz " + horiz + " custom " + custom + " bad " + bad);
                }

                // Level 1 first pickup, real board: CoachPick takes branch 4 (red tip) to branch 5. Branch 4 is
                // the top-left row, ~3% of height under the banner. Current code must take the horizontal
                // standard approach for it (hand points left, enters from the right), never from below.
                {
                    var l1 = LevelData.Open(0);
                    int from = -1;
                    for (int a1 = 0; a1 < l1.Branches.Count && from < 0; a1++)
                        if (l1.CanPick(a1) && l1.Branches[a1].TipRun() == 1)
                            for (int b1 = 0; b1 < l1.Branches.Count; b1++)
                                if (b1 != a1 && l1.Branches[b1].Birds.Count == 1 && l1.CanMove(a1, b1, out _)) { from = a1; break; }
                    Check("level1 real board first pickup is branch 4", from == 4, "from " + from);
                    for (int d = 0; d < sizes.Length; d++)
                    {
                        float w = sizes[d].x, h = sizes[d].y;
                        DeviceFit.TestWindow = w / h;
                        float s = DeviceFit.Ui(w, h);
                        var plate = new Rect(0f, h * 0.234f, w, h * 0.12f);
                        var target = new Vector2(w * 0.23f, plate.yMax + h * 0.03f);
                        var aim = FlockFiveApp.GardenSidewaysAim(target, plate, s, w, out float deg);
                        var box0 = FlockFiveApp.GloveAuditBox(aim, deg, FlockFiveApp.GloveAuditDh(s), 0f);
                        var box1 = FlockFiveApp.GloveAuditBox(aim, deg, FlockFiveApp.GloveAuditDh(s), 1f);
                        float drop = aim.y - target.y;
                        Check("level1 first glove: sideways from the right, clear, short nudge " + stags[d],
                            deg == -90f && CaptionLayout.GloveClearsBanner(box0, plate) && CaptionLayout.GloveClearsBanner(box1, plate) && drop >= 0f && drop <= h * 0.05f && aim.x == target.x,
                            "deg " + deg + " drop " + drop.ToString("0") + " cap " + (h * 0.05f).ToString("0"));
                    }
                }

                // b100: both Level 1 lessons (and every garden lesson) share one bottom seat, lifted about
                // 4.5% of height, clear of the lowest bird row; none overlaps the glove aim.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    float inset = Mathf.Max(8f, safe.yMin + 4f);
                    float rowBot = h * 0.727f;
                    float ref1 = FlockFiveApp.GardenRefBandH(w, h, safe, s, 0f);
                    float own1 = FlockFiveApp.BannerRect("coach.line", CoachTap.Caption(CoachStep.CoachPickup), w, h, safe, new Rect(0f, 0f, w, 10f), 0f, false, s, out _, false).height;
                    float own2 = FlockFiveApp.BannerRect("coach.line", CaptionLayout.CoachPlaceLine, w, h, safe, new Rect(0f, 0f, w, 10f), 0f, false, s, out _, false).height;
                    bool ok1 = FlockFiveApp.GardenBottomSeat(Mathf.Max(ref1, own1), rowBot, h, inset, 8f * s, out float y1);
                    bool ok2 = FlockFiveApp.GardenBottomSeat(Mathf.Max(ref1, own2), rowBot, h, inset, 8f * s, out float y2);
                    // Every other garden lesson line lands on the same Y as well.
                    string[] others = FlockFiveApp.GardenBottomLines;
                    bool sameAll = true;
                    for (int o = 0; o < others.Length; o++)
                    {
                        float oh = FlockFiveApp.BannerRect("coach.line", others[o], w, h, safe, new Rect(0f, 0f, w, 10f), 0f, false, s, out _, false).height;
                        FlockFiveApp.GardenBottomSeat(Mathf.Max(ref1, oh), rowBot, h, inset, 8f * s, out float yo);
                        if (Mathf.Abs(yo - y1) > 0.01f) sameAll = false;
                    }
                    // Other garden captions (gift Stuck, leaves, pests...) are not on the bottom seat.
                    sameAll = sameAll && !FlockFiveApp.IsGardenBottomLine("Stuck? Tap the gift\nbranch for a bonus.") && !FlockFiveApp.IsGardenBottomLine(CaptionLayout.LeafLine) && FlockFiveApp.IsGardenBottomLine(CaptionLayout.CoachPlaceLine);
                    Check("garden banner: the three lesson lines share the seat " + stags[d], sameAll, "y1 " + y1.ToString("0"));
                    float flush = h - inset - 8f * s - ref1;
                    Check("garden banner: same seat for both Level 1 lessons, lifted " + stags[d],
                        ok1 == ok2 && (!ok1 || (Mathf.Abs(y1 - y2) < 0.01f && y1 <= flush - h * CaptionLayout.GardenBannerLiftFrac + 0.5f && y1 >= rowBot)),
                        "fits " + ok1 + " y1 " + y1.ToString("0") + " y2 " + y2.ToString("0") + " flush " + flush.ToString("0") + " rowBot " + rowBot.ToString("0"));
                }

                // b101: the lesson banner seat is FIXED. It must not depend on the lowest-bird bound (which moves
                // as birds lift and hop) or on where the glove aims; the top fallback is gone.
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    float inset = Mathf.Max(8f, safe.yMin + 4f);
                    float refH = FlockFiveApp.GardenRefBandH(w, h, safe, s, 0f);
                    CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var rs, out var hv);
                    rs = CornerAnchor.GardenBottomSeat(w, h, safe); // b131: banner seat (Restart itself moved to the top bar)
                    float y0 = FlockFiveApp.GardenBannerY(refH, h * 0.5f, rs, hv, h, inset, 8f * s, h * 0.234f);
                    int flips = 0;
                    for (float rb = h * 0.50f; rb <= h * 0.99f; rb += h * 0.01f)
                        if (Mathf.Abs(FlockFiveApp.GardenBannerY(refH, rb, rs, hv, h, inset, 8f * s, h * 0.234f) - y0) > 0.01f) flips++;
                    Check("garden banner seat never flips with the bird bound " + stags[d], flips == 0 && y0 > h * 0.5f, "flips " + flips + " y " + y0.ToString("0"));
                    // The fixed seat never moves with the glove aim either.
                    float ya = FlockFiveApp.GardenFixedSeatY(w, h, safe, s, 0f);
                    float yb = FlockFiveApp.GardenFixedSeatY(w, h, safe, s, 0f);
                    Check("garden banner fixed seat is stable " + stags[d], Mathf.Abs(ya - yb) < 0.001f && Mathf.Abs(ya - y0) < 0.01f, "y " + ya.ToString("0") + " vs " + y0.ToString("0"));
                }

                // Level 1 flow on the real Dawn Garden board: step 1 = branch 0 (RRR) onto branch 5 (lone red),
                // step 2 = branch 4 (red tip) onto branch 5, which completes five reds.
                {
                    var l1 = LevelData.Open(0);
                    bool got = FlockFiveApp.Level1First(l1, i => false, i => false, out int f1, out int t1);
                    Check("level1 step 1 is branch 0 onto branch 5", got && f1 == 0 && t1 == 5, "from " + f1 + " to " + t1);
                    bool moved = l1.TryMove(f1, t1, out int run1);
                    int f2 = -1, t2 = -1;
                    for (int a2 = 0; a2 < l1.Branches.Count && f2 < 0; a2++)
                    {
                        if (!l1.CanPick(a2)) continue;
                        for (int b2 = 0; b2 < l1.Branches.Count; b2++)
                            if (b2 != a2 && !l1.Branches[b2].Empty && l1.CanMove(a2, b2, out _)) { f2 = a2; t2 = b2; break; }
                    }
                    bool moved2 = f2 >= 0 && l1.TryMove(f2, t2, out _);
                    Check("level1 step 2 is branch 4 onto branch 5, step 3 collects the reds", moved && run1 == 3 && f2 == 4 && t2 == 5 && moved2 && l1.FindCollect() == 5,
                        "step2 " + f2 + "->" + t2 + " collect " + l1.FindCollect());
                }

                // Level 1 target (b4's red tip bird, top visible row) on iPhone / tall iPhone / iPad / SE: the
                // garden banner takes its seat (bottom when the lesson allows it), the glove comes in sideways
                // from the right, and the fingertip must land INSIDE the target bird's rect (no nudge off it).
                {
                    var devs = new[] { new Vector2(1179f, 2556f), new Vector2(1290f, 2796f), new Vector2(1536f, 2048f), new Vector2(750f, 1334f) };
                    string[] dtags = { "iphone", "tall-iphone", "ipad", "se" };
                    for (int d = 0; d < devs.Length; d++)
                    {
                        float w = devs[d].x, h = devs[d].y;
                        DeviceFit.TestWindow = w / h;
                        float s = DeviceFit.Ui(w, h);
                        var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                        float bandH = h * 0.12f;
                        float rowBot = h * 0.727f;
                        // Target bird: tip of the top-left visible limb, from the build 98 screenshot (fractions).
                        var target = new Vector2(w * 0.24f, h * 0.277f); // tip bird of branch 0 (RRR), top-left row, behind the old top banner
                        var birdRect = new Rect(target.x - w * 0.05f, target.y - h * 0.026f, w * 0.10f, h * 0.052f);
                        CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
                        restart = CornerAnchor.GardenBottomSeat(w, h, safe); // b131: banner seat (Restart itself moved to the top bar)
                        float by = FlockFiveApp.GardenBannerY(bandH, rowBot, restart, hive, h, safe.yMin, 8f * s, h * 0.234f);
                        var plate = new Rect(0f, by, w, bandH);
                        var aim = FlockFiveApp.GardenSidewaysAim(target, plate, s, w, out float deg);
                        var box = FlockFiveApp.GloveAuditBox(aim, deg, FlockFiveApp.GloveAuditDh(s), 0f);
                        // Step 2 destination: branch 5, the lone red, third limb of the right column (seat 0, right edge).
                        var dest = new Vector2(w * 0.936f, h * 0.39f);
                        var destRect = new Rect(dest.x - w * 0.05f, dest.y - h * 0.026f, w * 0.10f, h * 0.052f);
                        var aim2 = FlockFiveApp.GardenSidewaysAim(dest, plate, s, w, out _);
                        Check("level1 step2 fingertip on branch 5's red bird, side approach " + dtags[d],
                            destRect.Contains(aim2) && !FlockFiveApp.GardenFromRight(dest.x, w) && plate.yMin > dest.y,
                            "aim " + aim2.y.ToString("0") + " dest " + dest.y.ToString("0"));
                        Check("level1 step1 fingertip on branch 0's top red bird, glove clear of banner " + dtags[d],
                            birdRect.Contains(aim) && deg == -90f && CaptionLayout.GloveClearsBanner(box, plate) && plate.yMin > target.y,
                            "banner y " + by.ToString("0") + " target y " + target.y.ToString("0") + " aim y " + aim.y.ToString("0") + " bird " + birdRect.yMin.ToString("0") + ".." + birdRect.yMax.ToString("0"));
                    }
                }

                // Glove entry: start-to-target distance is 67% of the old one, same line (right to left).
                {
                    var aim = new Vector2(300f, 900f);
                    float dh = 220f;
                    var rest = aim + new Vector2(1f, 0f) * 80f;
                    var away = new Vector2(1f, 0f);
                    var oldStart = rest + away * (dh * 0.9f);
                    var ns = FlockFiveApp.GloveEntryPoint(rest, aim, away, dh);
                    float ratio = (ns - aim).magnitude / (oldStart - aim).magnitude;
                    Check("glove entry distance cut by a third", ratio <= 0.67f && ns.x > aim.x && Mathf.Abs(ns.y - aim.y) < 0.01f, "ratio " + ratio.ToString("0.000"));
                }

                // Press burst: glints start on the upper half of the button ring (inverted U), centred.
                {
                    float W = 1179f, H = 2556f;
                    var src = new Rect(W * 0.14f, H * 0.62f, W * 0.72f, W * 0.72f);
                    var buf = new LevelCurtain.Glint[LevelCurtain.Count * 2];
                    int n = LevelCurtain.Fill(0.03f, W, H, src, buf);
                    float ringCy = src.y + src.height * SparkleFx.FlatRingCy;
                    float minX = 1e9f, maxX = -1e9f, maxY = -1e9f;
                    for (int i = 0; i < n; i++) { minX = Mathf.Min(minX, buf[i].X); maxX = Mathf.Max(maxX, buf[i].X); maxY = Mathf.Max(maxY, buf[i].Y); }
                    Check("burst starts on the top curve", n > 0 && maxY < ringCy && minX < src.center.x - src.width * 0.2f && maxX > src.center.x + src.width * 0.2f,
                        "n " + n + " maxY " + maxY.ToString("0") + " ringCy " + ringCy.ToString("0") + " x " + minX.ToString("0") + ".." + maxX.ToString("0"));
                }

                // Plane: 15 s first, 40 s later.
                Check("plane idle 15/40", FlockFiveApp.PlaneIdleFor(false) == 15f && FlockFiveApp.PlaneIdleFor(true) == 40f, "");
            }
            catch (System.Exception ex)
            {
                Check("b95", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }
            string end = fail == 0 ? "BUILD95_OK " + pass : "BUILD95_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
