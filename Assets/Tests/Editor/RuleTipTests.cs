#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 4: first-time rule captions for crown/bow, feeder turn and sleep.
    static class RuleTipTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[rule-tips] FAIL " + what); } }
            var none = new RuleTips.Seen();
            Check(RuleTips.Due(none, new RuleTips.Board()) == RuleTip.None, "tip with nothing on board");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true }) == RuleTip.Kit, "kit tip");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true, AnySleeping = true }) == RuleTip.Sleep, "sleep first");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true, AnySleeping = true, FeederTurned = true }) == RuleTip.Feeder, "feeder first");
            var all = new RuleTips.Seen { Kit = true, Feeder = true, Sleep = true };
            Check(RuleTips.Due(all, new RuleTips.Board { HasKit = true, AnySleeping = true, FeederTurned = true }) == RuleTip.None, "seen tip repeats");
            Check(RuleTips.Turned(new BirdColor?[] { BirdColor.Ruby, null }, new BirdColor?[] { BirdColor.Teal, null }), "turn missed");
            Check(!RuleTips.Turned(new BirdColor?[] { null, null }, new BirdColor?[] { BirdColor.Teal, null }), "fill read as turn");
            foreach (var t in new[] { RuleTip.Kit, RuleTip.Feeder, RuleTip.Sleep })
            {
                var line = RuleTips.Line(t);
                Check(!string.IsNullOrEmpty(line) && line.Length <= 70, t + " line too long");
                Check(CoachTap.Kind(RuleTips.Step(t)) == CoachTapKind.Informational, t + " not any-tap");
                Check(CoachTap.Caption(RuleTips.Step(t)) == line, t + " caption mismatch");
                Check(RuleTips.Key(t).StartsWith("flockfive.coach.rule."), t + " key");
            }
            Check(RuleTips.HoldMax > 0f, "no watchdog");
            Check(RuleTips.IsLine(RuleTips.KitLine) && !RuleTips.IsLine("Tap a branch."), "rule line detect");
            Check(RuleTips.FeederReady(true, false, false, false), "feeder ready when landed");
            Check(!RuleTips.FeederReady(true, false, true, false), "feeder waits on another tip");
            Check(!RuleTips.FeederReady(true, true, false, false), "feeder seen stays down");
            Check(!RuleTips.FeederReady(false, false, false, false), "feeder without a turn");
            var kitRows = RuleTips.KitLine.Split('\n');
            Check(kitRows.Length == 3 && kitRows[1] == "bows with bows!", "bows stay one row");
            var sleepRows = RuleTips.SleepLine.Split('\n');
            Check(sleepRows.Length == 3 && sleepRows[2] == "wants their colour.", "sleep rows");
            // 402, 412, and the iPad play column (820x1180 pillars to 9:16).
            var faces = new[]
            {
                new Vector2(402f, 874f),
                new Vector2(412f, 915f),
                new Vector2(664f, 1180f),
                new Vector2(820f, 1180f),
            };
            var style = FlockFiveApp.CoachCaptionStyle();
            float? keepWindow = DeviceFit.TestWindow;
            try
            {
                foreach (var face in faces)
                {
                    DeviceFit.TestWindow = face.x / face.y;
                    int banner = CaptionLayout.BannerFont(face.x, face.y);
                    float col = CaptionLayout.RuleTextWidth(face.x);
                    foreach (RuleTip tip in new[] { RuleTip.Kit, RuleTip.Feeder, RuleTip.Sleep })
                    {
                        string line = RuleTips.Line(tip);
                        var kept = CaptionLayout.FitKept(style, line, col, banner, CaptionLayout.RuleFontFloor(banner));
                        bool whole = kept.Shown == line && !kept.Overflow;
                        Check(whole, face.x + "x" + face.y + " " + tip + " reflow [" + (kept.Shown ?? "").Replace("\n", " / ") + "] font " + kept.Font + "/" + banner);
                    }
                }
            }
            finally { DeviceFit.TestWindow = keepWindow; }
            Check(Mathf.Abs(CaptionStyles.Ink("coach.rule") - 1f) < 0.001f, "rule plate solid");
            Debug.Log("[rule-tips] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
