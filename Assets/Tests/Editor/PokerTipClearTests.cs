#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // The home coins tip ("You earned coins! Tap poker to bet them.") is a full-width
    // banner. The poker glove crosses that width, so the banner sits under the whole
    // path — perch, arc, and the landed hand — and is not lifted back onto the finger
    // when the LEVEL disc is close. iPad column first, then the phone columns.
    public static class PokerTipClearTests
    {
        const string Out = "/tmp/flock-five-poker-tip-clear-tests.txt";
        const string Line = "You earned coins!\nTap poker to bet them.";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[pokertip] " + line);
            }
            float? keep = DeviceFit.TestWindow;
            try
            {
                Check("gap is the shared caption margin",
                    Mathf.Abs(FlockFiveApp.PokerTipClearGap(2f) - 24f) < 0.01f, "");

                // A safe bottom that cannot hold the band must not pull it up onto the path.
                float pinned = FlockFiveApp.PokerTipBandY(500f, 180f, 40f, 520f, 12f);
                Check("short glass keeps the tip under the path", pinned >= 512f - 0.01f, "y " + pinned);
                float roomy = FlockFiveApp.PokerTipBandY(500f, 80f, 40f, 900f, 12f);
                Check("roomy glass seats the tip on the gap", Mathf.Abs(roomy - 512f) < 0.01f, "y " + roomy);
                float underLogo = FlockFiveApp.PokerTipBandY(40f, 30f, 100f, 900f, 12f);
                Check("logo floor still stays under the path", underLogo >= 52f - 0.01f, "y " + underLogo);

                // Coin row just above the fingertip: the perch is pushed below the aim,
                // which is the pose that used to cross the banner.
                float forcedS = 2f;
                float forcedAim = 500f;
                float forcedRow = forcedAim - 10f;
                float forcedPerch = FlockFiveApp.PokerGlovePerchY(forcedAim, forcedRow, forcedS);
                Check("close coin row drops the perch below the fingertip", forcedPerch > forcedAim + 4f,
                    "perch " + forcedPerch.ToString("0.0") + " aim " + forcedAim.ToString("0.0"));
                FlockFiveApp.PokerGlovePathExtent(forcedAim, forcedRow, forcedS, out _, out float forcedBottom);
                float forcedGap = FlockFiveApp.PokerTipClearGap(forcedS);
                float forcedY = FlockFiveApp.PokerTipBandY(forcedBottom, 160f, 0f, forcedBottom + forcedGap + 8f, forcedGap);
                var forcedBand = new Rect(0f, forcedY, 800f, 160f);
                var forcedYs = new float[5];
                int forcedN = FlockFiveApp.PokerGlovePathPivots(forcedAim, forcedRow, forcedS, forcedYs);
                int forcedHits = 0;
                float forcedLow = forcedAim;
                for (int i = 0; i < forcedN; i++)
                {
                    if (forcedYs[i] > forcedLow) forcedLow = forcedYs[i];
                    var hand = FlockFiveApp.PokerDrawnHand(new Vector2(400f, forcedYs[i]), forcedS);
                    if (forcedBand.Overlaps(hand) || hand.yMax > forcedBand.y - forcedGap + 0.75f) forcedHits++;
                }
                Check("forced path stays clear of the tip", forcedN >= 3 && forcedHits == 0 && forcedLow > forcedAim,
                    "hits " + forcedHits + " low " + forcedLow.ToString("0") + " bandY " + forcedY.ToString("0"));

                var devices = new[]
                {
                    new Vector2(820f, 1180f),
                    new Vector2(402f, 874f),
                    new Vector2(412f, 915f),
                    new Vector2(1179f, 2556f),
                    new Vector2(1080f, 2400f),
                };
                string[] tags = { "ipad", "402", "412", "iphone", "android" };
                bool sawLowPerch = false;
                for (int d = 0; d < devices.Length; d++)
                {
                    float w = devices[d].x, h = devices[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    float safeTop = h - safe.yMax, safeBottom = h - safe.yMin;
                    float minTop = Mathf.Max(safeTop, FlockFiveApp.HomeBannerTop(h, safe, s));
                    var measured = FlockFiveApp.BannerRect("home.poker", Line, w, h, safe, new Rect(w * 0.2f, h * 0.4f, w * 0.5f, 80f), 0f, false, s, out _);
                    float bandH = measured.height;
                    for (int hive = 0; hive < 2; hive++)
                    {
                        RestedRails(w, h, safe, hive == 1, out var pig, out var poker);
                        float aimX = poker.x + poker.width * FlockFiveApp.PokerGloveU;
                        float aimY = poker.y + poker.height * FlockFiveApp.PokerGloveV;
                        string tag = tags[d] + (hive == 1 ? " hive" : " nohive");
                        FlockFiveApp.PokerGlovePathExtent(aimY, pig.yMax, s, out _, out float pathBottom);
                        float floor = pathBottom > poker.yMax ? pathBottom : poker.yMax;
                        float gap = FlockFiveApp.PokerTipClearGap(s);
                        float y = FlockFiveApp.PokerTipBandY(floor, bandH, minTop, safeBottom, gap);
                        var band = new Rect(0f, y, w, bandH);
                        float perch = FlockFiveApp.PokerGlovePerchY(aimY, pig.yMax, s);
                        if (perch > aimY + 4f) sawLowPerch = true;
                        var ys = new float[5];
                        int n = FlockFiveApp.PokerGlovePathPivots(aimY, pig.yMax, s, ys);
                        int hits = 0;
                        float worst = 0f;
                        for (int i = 0; i < n; i++)
                        {
                            var hand = FlockFiveApp.PokerDrawnHand(new Vector2(aimX, ys[i]), s);
                            float over = hand.yMax - (band.y - gap);
                            if (over > worst) worst = over;
                            if (band.Overlaps(hand) || over > 0.75f) hits++;
                        }
                        bool under = band.y >= floor + gap - 0.6f;
                        bool offButton = !band.Overlaps(poker);
                        bool onGlass = band.y >= -1f && band.y < h;
                        Check("tip under the glove path " + tag, n >= 3 && hits == 0 && under && offButton && onGlass && bandH > 8f,
                            "y " + y.ToString("0") + " h " + bandH.ToString("0") + " floor " + floor.ToString("0")
                            + " perch " + perch.ToString("0") + " aim " + aimY.ToString("0") + " hits " + hits
                            + " worst " + worst.ToString("0.0") + " poker " + poker.yMax.ToString("0"));
                    }
                }
                Check("a phone perch drops below the fingertip", sawLowPerch, "");

                string coach = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Coach.cs"));
                int coins = coach.IndexOf("Rect PokerCoinsBand(");
                int next = coach.IndexOf("public static float CoinsBannerY", coins);
                string seat = coins >= 0 && next > coins ? coach.Substring(coins, next - coins) : "";
                int early = seat.IndexOf("return band;");
                int lift = seat.IndexOf("LiftOffMainButtons");
                Check("coins band uses the path and does not lift onto it",
                    seat.Contains("PokerGlovePathBottom") && seat.Contains("PokerTipBandY")
                    && early > 0 && lift > early,
                    "early " + early + " lift " + lift);
                int live = coach.IndexOf("if (_pokerIntroLive)");
                int daily = coach.IndexOf("if (_dailyIntroLive)", live);
                string body = live >= 0 && daily > live ? coach.Substring(live, daily - live) : "";
                Check("poker step notes the banner before the glove",
                    body.IndexOf("NoteTutorPlate(PokerIntroTipBand(") > 0
                    && body.IndexOf("PokerGloveFromLeft") > body.IndexOf("NoteTutorPlate(PokerIntroTipBand("),
                    "");
            }
            catch (System.Exception ex)
            {
                Check("poker-tip-threw", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
                sb.AppendLine("TOTAL pass " + pass + " fail " + fail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[pokertip] TOTAL pass " + pass + " fail " + fail);
            }
            if (fail > 0) throw new System.Exception("poker tip clear " + fail + " failed, " + pass + " passed");
        }

        // Rested right rail: pig, then hive when it is up, then poker. Same packing as ApplySplashRails.
        static void RestedRails(float w, float h, Rect safe, bool hive, out Rect pig, out Rect poker)
        {
            float size = FlockFiveApp.SplashRailSize(w);
            float railGap = Mathf.Max(24f, size * 0.30f);
            float anchorY = h * 0.335f;
            float x = Mathf.Min(w, safe.xMax) - 20f - size;
            float top = anchorY - size - railGap;
            pig = new Rect(x, top, size, size);
            float y = top + size + railGap;
            if (hive) y += size + railGap;
            poker = new Rect(x, y, size, size);
        }
    }
}
#endif
