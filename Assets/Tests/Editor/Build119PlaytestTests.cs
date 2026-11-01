#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b119 playtest fixes (Brandon, 2026-11-01): VIP store note, multiplier poke, lightning bolt/ignite order,
    // badger win lines, the power-ready pill, the cancel/tie pacing, the USED power look, and the frozen shiver.
    public static class Build119PlaytestTests
    {
        const string Out = "/tmp/flock-five-b119-playtest-tests.txt";

        [MenuItem("Flock Five/Build 119 Playtest Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                Debug.Log("[build119-playtest] " + line);
            }

            string Src(string p) => File.Exists(p) ? File.ReadAllText(p) : "";
            const string App = "Assets/_Project/Scripts/App/";

            // (1) VIP: no timeout note; a grant clears the note.
            Check("vip-timeout-silent", PurchasePending.NoteFor(PurchasePending.Outcome.TimedOut, PurchasePending.Kind.Buy) == null
                && PurchasePending.NoteFor(PurchasePending.Outcome.TimedOut, PurchasePending.Kind.Restore) == null, "");
            Check("vip-no-didnt-answer-text", Src(App + "PurchasePending.cs").IndexOf("didn't answer") < 0, "");
            Check("vip-grant-clears-note", Src(App + "NoAds.cs").IndexOf("NoteRestore(null)") > 0, "");

            // (2) Multiplier stamp: no poke pulse, the tap is still eaten.
            string app = Src(App + "FlockFiveApp.cs");
            Check("stamp-no-pulse", app.IndexOf("_stampPulse") < 0, "");
            Check("stamp-still-eats-tap", app.IndexOf("if (GardenStampHit(screen)) return;") > 0, "");

            // (3) Lightning: bolt first, ignite later, bigger aimed bolt with an impact flash, aimed at the wood.
            string tick = Src(App + "FlockFiveApp.Lightning.cs");
            int strike = tick.IndexOf("GardenStorm.StrikeAt(aim)");
            int ignite = tick.IndexOf("StartCoroutine(IgniteAfterBolt(");
            Check("bolt-before-ignite", strike > 0 && ignite > strike && tick.IndexOf("view.BeginScorch(_garden.Root);") < 0, "");
            Check("ignite-delay", Mathf.Abs(GardenStorm.IgniteDelay - 0.15f) < 0.001f, "" + GardenStorm.IgniteDelay);
            Check("aimed-bolt-life", Mathf.Abs(GardenStorm.BoltLifeFor(true, true) - 0.7f) < 0.001f
                && Mathf.Abs(GardenStorm.BoltLifeFor(true, false) - 0.46f) < 0.001f && Mathf.Abs(GardenStorm.BoltLifeFor(false, false) - 0.28f) < 0.001f, "");
            Check("aimed-bolt-thicker", GardenStorm.BoltThickFor(true, true) > GardenStorm.BoltThickFor(true, false), "");
            Check("impact-flash-curve", GardenStorm.ImpactAlpha(0f) > 0.99f && GardenStorm.ImpactAlpha(0.2f) > 0f
                && GardenStorm.ImpactAlpha(GardenStorm.ImpactSeconds) == 0f && GardenStorm.ImpactAlpha(-0.1f) == 0f, "");
            Check("impact-flash-built", Src(App + "GardenStorm.cs").IndexOf("StartImpact(x1, y1)") > 0, "");
            var wood = new Bounds(new Vector3(2f, 1f, 0f), new Vector3(3f, 0.4f, 0f));
            var aimP = LightningStrike.AimPoint(new Vector3(0.5f, 1f, 5f), true, wood);
            Check("aim-wood-centre", Mathf.Abs(aimP.x - 2f) < 0.001f && Mathf.Abs(aimP.y - 1f) < 0.001f && Mathf.Abs(aimP.z - 5f) < 0.001f, aimP.ToString());
            Check("aim-no-wood-pivot", LightningStrike.AimPoint(new Vector3(0.5f, 1f, 5f), false, default) == new Vector3(0.5f, 1f, 5f), "");
            Check("ignite-well-inside-bolt", GardenStorm.IgniteDelay < GardenStorm.AimedBoltLife && GardenStorm.IgniteDelay < BranchView.ScorchSeconds, "");

            // (4) Win lines: pool, no back-to-back repeat, signature at most once per fight and never early.
            Check("win-pool", BadgerCopy.WinLines.Length >= 4 && System.Array.IndexOf(BadgerCopy.WinLines, BadgerCopy.DontCare) < 0, "");
            var won = new BadgerRound { PlayerGained = 3 };
            int sig = 0, repeats = 0, early = 0; string last = null;
            for (int seed = 1; seed <= 60; seed++)
            {
                var rng = new System.Random(seed);
                var pool = new BadgerWinLines(rng.Next);
                int fightSig = 0;
                last = null;
                for (int k = 0; k < 12; k++)
                {
                    string ln = BadgerCopy.VerdictLine(won, pool);
                    if (ln == BadgerCopy.DontCare) { fightSig++; if (k < BadgerCopy.SignatureMinWins) early++; }
                    if (ln == last) repeats++;
                    last = ln;
                }
                if (fightSig > 1) sig += 100;
                sig += fightSig;
            }
            Check("win-no-repeat", repeats == 0, "repeats " + repeats);
            Check("signature-once-per-fight", sig < 100 && sig > 0, "total " + sig);
            Check("signature-not-early", early == 0, "early " + early);
            var zero = new BadgerWinLines(n => 0);
            Check("signature-earliest-third-win", zero.Next() != BadgerCopy.DontCare && zero.Next() != BadgerCopy.DontCare && zero.Next() == BadgerCopy.DontCare
                && zero.Next() != BadgerCopy.DontCare && zero.SignatureUsed, "");
            Check("verdict-null-pool", BadgerCopy.VerdictLine(won) == BadgerCopy.WinLines[0], "");
            var app4 = Src(App + "FlockFiveApp.Badger.cs");
            Check("fight-gets-fresh-pool", app4.IndexOf("_bgWinLines = new BadgerWinLines(") > 0 && app4.IndexOf("BadgerCopy.VerdictLine(r, _bgWinLines)") > 0, "");

            // (6) Ready pill: the pick band, two rows, fades on its own, and never a hit rect.
            string ready = BadgerCopy.ReadyLine("x2", false);
            Check("ready-in-pick-band", FlockFiveApp.BadgerCaptionSpot(ready) == "badger.pick"
                && FlockFiveApp.BadgerCaptionSpot(BadgerCopy.ReadyLine("Hot Sauce", true)) == "badger.pick", ready.Replace("\n", "/"));
            Check("ready-two-rows", ready.Split('\n').Length <= CaptionStyles.MaxLines("badger.pick"), "");
            Check("ready-fades", BadgerCopy.ReadyAlpha(0f) == 1f && BadgerCopy.ReadyAlpha(1.4f) == 1f
                && BadgerCopy.ReadyAlpha(1.6f) < 1f && BadgerCopy.ReadyAlpha(BadgerCopy.ReadyHold + BadgerCopy.ReadyFade) == 0f, "");
            Check("not-ready-elsewhere", !BadgerCopy.IsReadyLine(BadgerCopy.YourPick) && !BadgerCopy.IsReadyLine(BadgerCopy.CoinsShort), "");
            foreach (var sz in new[] { new Vector2(375, 667), new Vector2(390, 844), new Vector2(1290, 2796) })
            {
                var plate = FlockFiveApp.BadgerPickPlateAt(sz.x, sz.y, new Rect(0, 0, sz.x, sz.y), ready, out var boss, out var arena, out var you, out var power,
                    out bool fits, out string detail);
                Check("ready-pill-clear-" + sz.x, fits && !plate.Overlaps(you) && !plate.Overlaps(arena) && !plate.Overlaps(power), detail);
            }

            // (7) Cancel pacing.
            Check("cancel-beats", Mathf.Abs(BadgerCancelBeat.ShatterSeconds - 0.6f) < 0.001f && Mathf.Abs(BadgerCancelBeat.PauseSeconds - 0.4f) < 0.001f
                && BadgerCancelBeat.TieHoldSeconds >= 1.8f && Mathf.Abs(BadgerCancelBeat.TapLockSeconds - 0.8f) < 0.001f, "");
            Check("cancel-tap-lock", !BadgerCancelBeat.TapSkips(0.79f) && BadgerCancelBeat.TapSkips(0.8f), "");
            Check("cancel-verdict-hold", BadgerCancelBeat.VerdictHold(true, 1.45f) >= 1.8f && Mathf.Abs(BadgerCancelBeat.VerdictHold(false, 1.45f) - 1.45f) < 0.001f, "");
            var cancelTie = new BadgerRound { DontCare = true, Destroyed = BadgerPower.X2, PlayerFinal = 5, BossFinal = 5 };
            string tieLine = BadgerCopy.VerdictLine(cancelTie);
            Check("cancel-tie-line", tieLine.StartsWith("5 vs 5") && tieLine.Contains("tie"), tieLine.Replace("\n", "/"));
            var cancelLose = new BadgerRound { DontCare = true, Destroyed = BadgerPower.X2, PlayerFinal = 3, BossFinal = 5, BossGained = 5 };
            Check("cancel-lose-line", BadgerCopy.VerdictLine(cancelLose) == BadgerCopy.DontCareWasted, "");
            Check("cancel-stage-wired", app4.IndexOf("BadgerCancelBeat.Done(_bgCancelT)") > 0 && app4.IndexOf("DrawBadgerShatter(rr") > 0, "");
            Check("cancel-under-heal", BadgerCancelBeat.Duration < TutorialHeal.MaxGateSeconds, "");

            // (8) USED look.
            Check("used-look", BadgerUsedLook.PlateAlpha <= 0.5f && BadgerUsedLook.PlateAlpha < 0.85f, "");
            Check("used-wired", app4.IndexOf("bool spent = used && !armedNow && !frozenOut;") > 0 && app4.IndexOf("BadgerPowerSpentColors(") > 0
                && app4.IndexOf("DrawBadgerUsedSlash(") > 0, "");

            // (9) Frozen badger: a deliberate periodic shiver, still between bursts.
            int still = 0, moving = 0;
            for (int i = 0; i < 400; i++)
            {
                float v = SparkleFx.IceShiver(i * 0.01f, 2f);
                if (Mathf.Abs(v) < 0.0001f) still++; else moving++;
            }
            Check("ice-shiver-bursts", still > moving * 2 && moving > 0, "still " + still + " moving " + moving);
            Check("ice-shiver-wired", app4.IndexOf("SparkleFx.IceShiver(now, 2.2f * s)") > 0 && app4.IndexOf("SparkleFx.Shiver(now, 2.2f * s)") < 0, "");

            // b119 follow-up (Brandon's picks): frozen copy matches the rule, flame tongues, Spring A art.
            Check("frozen-copy-rule", BadgerCopy.FrozenSolid.Contains("scores 0") && BadgerCopy.BreakFree.Contains("scores 0")
                && !BadgerCopy.FrozenSolid.Contains("can't move") && !BadgerCopy.BreakFree.Contains("spent his turn"), BadgerCopy.FrozenSolid + " | " + BadgerCopy.BreakFree.Replace("\n", "/"));
            // Fit: both lines are in Build110BadgerDialogTests' bottom-band list (small to large screens).
            var tongue = SpriteCatalog.FlameTongue;
            Check("flame-tongue-sprite", tongue != null && tongue.texture.height > tongue.texture.width && tongue.pivot.y < 0.5f
                && Mathf.Abs(tongue.pivot.x - tongue.rect.width * 0.5f) < 0.6f, tongue == null ? "null" : tongue.pivot.ToString());
            if (tongue != null)
            {
                var tx = tongue.texture;
                Color core = tx.GetPixel(tx.width / 2, tx.height / 5), tip = tx.GetPixel(tx.width / 2, tx.height - 2), edge = tx.GetPixel(1, tx.height / 5);
                Check("flame-tongue-colours", core.a > 0.9f && core.g > 0.7f && edge.a < 0.2f && tip.a < 0.5f, core + " " + tip + " " + edge);
            }
            string branch = Src("Assets/_Project/Scripts/View/BranchView.cs");
            string build = branch.Substring(System.Math.Max(0, branch.IndexOf("void BuildFlames(")), 600);
            Check("flames-use-tongues", build.Contains("SpriteCatalog.FlameTongue") && !build.Contains("SpriteCatalog.Glow"), "");
            var s0 = BranchView.FlameTongueScale(7, 14, 0f);
            var s1 = BranchView.FlameTongueScale(7, 14, 0.6f);
            var sEdge = BranchView.FlameTongueScale(0, 14, 0.6f);
            var sEnd = BranchView.FlameTongueScale(7, 14, BranchView.ScorchSeconds);
            Check("flame-tongue-grow-fade", s0.y == 0f && s1.y > 0.4f && sEdge.y < s1.y && sEnd.y == 0f, s1 + " " + sEdge);
            // v2: dense wall (overlapping tongues), warm base glow, frozen verdicts in the clear band under the power row.
            var sw0 = BranchView.FlameTongueScale(10, 24, 0.6f);
            float tongueW = tongue != null ? tongue.bounds.size.x * sw0.x : 0f;
            Check("flames-dense-wall", branch.Contains("FlameN = 24") && tongueW > 2.0f / 24f * 2.5f, "tongue w " + tongueW);
            Check("flames-base-glow", branch.Contains("BuildFlameGlow()") && branch.Contains("SpriteCatalog.Glow, transform.position, 1f, 15"), "");
            Check("frozen-verdict-in-band", FlockFiveApp.BadgerFrozenVerdict(BadgerCopy.FrozenSolid) && FlockFiveApp.BadgerFrozenVerdict(BadgerCopy.BreakFree)
                && !FlockFiveApp.BadgerFrozenVerdict(BadgerCopy.YourPick), "");
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Resources/Sprites/bg_spring.png");
            var bgImp = AssetImporter.GetAtPath("Assets/_Project/Art/Resources/Sprites/bg_spring.png") as TextureImporter;
            int bw = 0, bh = 0;
            if (bgImp != null) bgImp.GetSourceTextureWidthAndHeight(out bw, out bh);
            Check("spring-a-size", bw == 1080 && bh == 1920, bw + "x" + bh);
            var mask = Resources.Load<Texture2D>("SwayMasks/bg_spring_sway");
            Check("spring-sway-mask", mask != null, "");

            sb.AppendLine("pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build119-playtest] pass " + pass + " fail " + fail);
        }
    }
}
#endif
