#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // The adopt greet band moves down over the home VIP badge. Other caption spots stay put.
    public static class Build88BannerTests
    {
        const string Out = "/tmp/flock-five-b88-banner-tests.txt";
        const string Line = "Look, a bird followed you home!";

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
                Debug.Log("[build88banner] " + line);
            }

            float? keepWindow = DeviceFit.TestWindow;
            try
            {
                bool onlyGreet = CaptionStyles.Drop("adopt.greet") > 0f;
                int moved = 0;
                foreach (var kv in CaptionStyles.Banner)
                {
                    if (kv.Key == "adopt.greet") continue;
                    if (kv.Value.Drop != 0f) { onlyGreet = false; moved++; }
                }
                Check("drop-only-adopt-greet",
                    onlyGreet && moved == 0 && CaptionStyles.IsBanner("adopt.greet")
                    && CaptionStyles.IsBanner("home.intro") && CaptionStyles.IsBanner("coach.line")
                    && !CaptionStyles.IsBanner("home.daily"),
                    "greet " + CaptionStyles.Drop("adopt.greet") + " others " + moved);

                int solid = 0;
                foreach (var kv in CaptionStyles.Banner)
                {
                    if (kv.Key == "adopt.greet") continue;
                    if (kv.Value.Ink > 0f) solid++;
                }
                Check("ink-only-adopt-greet",
                    CaptionStyles.Ink("adopt.greet") >= 0.99f && solid == 0
                    && Mathf.Abs(CaptionStyles.Ink("home.intro") - CaptionStyles.BannerAlpha) < 0.001f
                    && Mathf.Abs(CaptionStyles.Ink("coach.line") - CaptionStyles.BannerAlpha) < 0.001f
                    && Mathf.Abs(CaptionStyles.Ink("badger.caption") - CaptionStyles.BannerAlpha) < 0.001f,
                    "greet " + CaptionStyles.Ink("adopt.greet") + " shared " + CaptionStyles.BannerAlpha + " overrides " + solid);

                var phone = new Rect(0f, 102f, 1179f, 2277f);
                var pad = new Rect(0f, 20f, 1536f, 2008f);
                Face(Check, "iphone", 1179f, 2556f, phone);
                Face(Check, "ipad", 1536f, 2048f, pad);
            }
            catch (System.Exception ex)
            {
                Check("banner", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keepWindow;
            }

            string end = fail == 0 ? "BUILD88BANNER_OK " + pass : "BUILD88BANNER_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88banner] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Face(System.Action<string, bool, string> check, string tag, float w, float h, Rect safe)
        {
            DeviceFit.TestWindow = w / h;
            float s = DeviceFit.Ui(w, h);
            var seat = new Rect(w * 0.2f, h * 0.42f, w * 0.6f, 120f);
            var badge = FlockFiveApp.AdoptVipBadgeRect(w, h, safe);
            var greet = FlockFiveApp.BannerRect("adopt.greet", Line, w, h, safe, seat, 0f, true, s, out _);
            var intro = FlockFiveApp.BannerRect("home.intro", Line, w, h, safe, seat, 0f, true, s, out _);
            var coach = FlockFiveApp.BannerRect("coach.line", Line, w, h, safe, seat, 0f, true, s, out _);
            var badger = FlockFiveApp.BannerRect("badger.caption", Line, w, h, safe, seat, 0f, true, s, out _);
            bool same = Near(intro, coach) && Near(intro, badger);
            bool greetCovers = Covers(greet, badge);
            bool othersClear = !Covers(intro, badge) && greet.y > intro.y + 4f;
            check(tag + "-banner-covers-badge",
                greetCovers && badge.width > 8f && greet.width >= w - 0.5f,
                "band " + greet + " badge " + badge);
            check(tag + "-other-banners-stay",
                same && othersClear,
                "intro " + intro + " coach " + coach + " badger " + badger);
        }

        static bool Covers(Rect band, Rect badge)
        {
            return band.xMin <= badge.xMin + 0.5f
                && band.yMin <= badge.yMin + 0.5f
                && band.xMax >= badge.xMax - 0.5f
                && band.yMax >= badge.yMax - 0.5f;
        }

        static bool Near(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.05f && Mathf.Abs(a.y - b.y) < 0.05f
                && Mathf.Abs(a.width - b.width) < 0.05f && Mathf.Abs(a.height - b.height) < 0.05f;
        }
    }
}
#endif
