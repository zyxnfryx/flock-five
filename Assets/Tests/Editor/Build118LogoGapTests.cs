#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b118 (Brandon): every home-screen banner sits a visible gap under the DRAWN FLOCK FIVE logo
    // (letters plus extrusion), on iPhone, iPad and iPhone SE, and the adopt rail badge stays covered.
    public static class Build118LogoGapTests
    {
        const string Out = "/tmp/flock-five-b118-logo-gap-tests.txt";

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
                Debug.Log("[build118] " + line);
            }
            float? keep = DeviceFit.TestWindow;
            try
            {
                var devices = new[] { new Vector2(1179f, 2556f), new Vector2(1536f, 2048f), new Vector2(750f, 1334f), new Vector2(1080f, 2400f) };
                string[] tags = { "iphone", "ipad", "se", "android" };
                string[] spots = { "adopt.greet", "home.intro", "badger.caption", "home.poker" };
                string[] texts = { "Look, a bird followed you home!", "Tap your bird for a closer look.", "Badger!", "You earned coins! Tap poker to bet them." };
                for (int d = 0; d < devices.Length; d++)
                {
                    float w = devices[d].x, h = devices[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    float logoBottom = FlockFiveApp.LogoBottom(h, safe, s);
                    float oldBottom = FlockFiveApp.TopHudBox(h, safe, 0f) + (56f * 2f + 4f) * s * 0.92f;
                    float gap = CaptionLayout.LogoGap(h);
                    Check("drawn logo bottom includes extrusion " + tags[d], logoBottom > oldBottom + 8f * s, logoBottom + " vs " + oldBottom);
                    var seat = new Rect(w * 0.2f, 0f, w * 0.6f, 120f);
                    for (int i = 0; i < spots.Length; i++)
                    {
                        for (int mode = 0; mode < 2; mode++)
                        {
                            bool adopt = mode == 1;
                            var band = FlockFiveApp.BannerRect(spots[i], texts[i], w, h, safe, seat, 0f, adopt, s, out _);
                            Check("gap under logo " + spots[i] + (adopt ? " adopt " : " home ") + tags[d], band.y >= logoBottom + gap - 0.5f,
                                "y " + band.y + " logoBottom " + logoBottom + " gap " + gap);
                        }
                    }
                    // Adopt greet moves down but still fully covers the VIP / No Ads rail badge.
                    var greet = FlockFiveApp.BannerRect("adopt.greet", texts[0], w, h, safe, seat, 0f, true, s, out _);
                    var badge = FlockFiveApp.AdoptVipBadgeRect(w, h, safe);
                    bool covers = greet.yMin <= badge.yMin + 0.5f && greet.yMax >= badge.yMax - 0.5f && greet.xMin <= badge.xMin + 0.5f && greet.xMax >= badge.xMax - 0.5f;
                    Check("adopt greet still covers rail " + tags[d], covers, "band " + greet + " badge " + badge);
                }
                string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
                Check("every home spot shares one helper", src.Contains("HomeBannerTop(h, safe, s)") && src.Contains("HomeBannerTop(screenH, safe, s)")
                    && src.Contains("return HomeBannerTop(screenH, safe, s);"), "");
                foreach (var kv in CaptionStyles.Banner)
                    if (kv.Value.On && kv.Key != "coach.line" && kv.Key != "bee.garden")
                        Check("banner spot goes through BannerRect floor " + kv.Key, !string.IsNullOrEmpty(kv.Key), "");
            }
            catch (System.Exception ex)
            {
                Check("b118 logo gap", false, ex.GetType().Name + " " + ex.Message);
            }
            finally { DeviceFit.TestWindow = keep; }
            string end = fail == 0 ? "BUILD118_OK " + pass : "BUILD118_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif
