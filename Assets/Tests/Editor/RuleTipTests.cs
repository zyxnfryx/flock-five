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
            Debug.Log("[rule-tips] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif
