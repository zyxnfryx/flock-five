#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 4: first-time rule captions for crown/bow, feeder turn and sleep.
    // The sleep caption waits until a bird has drawn Zzz. Board sleep at the
    // commit is still the hop, and the caption would freeze that hop.
    static class RuleTipTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[rule-tips] FAIL " + what); } }
            var none = new RuleTips.Seen();
            Check(RuleTips.Due(none, new RuleTips.Board()) == RuleTip.None, "tip with nothing on board");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true }) == RuleTip.Kit, "kit tip");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true, AnySleeping = true }) == RuleTip.None, "sleep waits until the nap pose");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true, AnySleeping = true, NapBegun = true }) == RuleTip.Sleep, "sleep first");
            Check(RuleTips.Due(none, new RuleTips.Board { HasKit = true, AnySleeping = true, FeederTurned = true }) == RuleTip.Feeder, "feeder before the nap pose");
            var sleepSeen = new RuleTips.Seen { Sleep = true };
            Check(RuleTips.Due(sleepSeen, new RuleTips.Board { HasKit = true, AnySleeping = true }) == RuleTip.Kit, "seen nap does not hold the kit");
            var all = new RuleTips.Seen { Kit = true, Feeder = true, Sleep = true };
            Check(RuleTips.Due(all, new RuleTips.Board { HasKit = true, AnySleeping = true, NapBegun = true, FeederTurned = true }) == RuleTip.None, "seen tip repeats");
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
            Check(sleepRows.Length == 3 && sleepRows[2] == "wants their color.", "sleep rows");
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
            Check(!TapCue.ForCaption(RuleTips.KitLine) && !TapCue.ForCaption(CaptionLayout.BeePestLine) && !TapCue.ForCaption(CaptionLayout.FireLine), "no fingertip tick");
            Check(!RuleTips.SleepReady(true, false, false, false, false), "sleep waits for the nap pose");
            Check(RuleTips.SleepReady(true, true, false, false, false), "sleep ready on first nap");
            Check(!RuleTips.SleepReady(true, true, false, true, false), "sleep waits on another tip");
            Check(!RuleTips.SleepReady(true, true, true, false, false), "sleep seen stays down");
            Check(!RuleTips.SleepReady(false, true, false, false, false), "sleep without a nap");
            Check(RuleTips.NapWait > 0f, "nap wait");
            NapFlag();
            NapWiring();
            var band = new Rect(0f, 1000f, 820f, 180f);
            var safe = new Rect(0f, 34f, 820f, 1146f);
            FlockFiveApp.ClampGardenBand(ref band, 820f, 1180f, safe);
            Check(band.yMax <= 1180f - 34f - 4f + 0.5f, "bee plate inside safe " + band.yMax);
            Debug.Log("[rule-tips] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));

            void NapFlag()
            {
                var go = new GameObject("nap-tip");
                try
                {
                    var idle = go.AddComponent<BirdIdle>();
                    idle.Sleeping = true;
                    idle.NoteNapZzz();
                    Check(idle.NapBegun, "zzz marks the nap");
                    idle.Sleeping = false;
                    Check(!idle.NapBegun, "waking clears the nap");
                    idle.NoteNapZzz();
                    Check(!idle.NapBegun, "zzz while awake does not count");
                    idle.Sleeping = true;
                    Check(!idle.NapBegun, "a new nap starts before zzz");
                }
                finally { Object.DestroyImmediate(go); }
            }

            void NapWiring()
            {
                string root = Application.dataPath;
                string idle = File.ReadAllText(Path.Combine(root, "_Project/Scripts/View/BirdIdle.cs"));
                string rules = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.RuleTips.cs"));
                string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
                int zzz = idle.IndexOf("PlaceZzz(snore);");
                int note = zzz < 0 ? -1 : idle.IndexOf("NoteNapZzz();", zzz);
                Check(zzz >= 0 && note > zzz && note - zzz < 80, "first zzz notes the nap");
                int at = app.IndexOf("bool napTip = kicked == 0 && _board.IsSleeping(to)");
                string block = at < 0 ? "" : app.Substring(at, System.Math.Min(480, app.Length - at));
                Check(block.Contains("StartCoroutine(RaiseSleepTipWhenNapping(to, gen))")
                    && block.Contains("else if (kicked == 0)")
                    && !block.Contains("RaiseRuleSleepTip()"), "move waits for the nap pose");
                Check(rules.Contains("if (!b.NapBegun && !_devNapHold && BranchNapBegun(i)) b.NapBegun = true"), "rule board reads the nap pose");
                Check(rules.Contains("SleepReady(sleeping, napBegun,"), "raise gates on the nap pose");
                Check(rules.Contains("while (gen == _motionGen && !BranchNapBegun(branch)"), "waiter yields until zzz");
            }
        }
    }
}
#endif
