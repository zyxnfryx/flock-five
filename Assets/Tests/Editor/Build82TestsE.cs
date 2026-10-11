#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82e. Livelier shared season motion, the Animate background
    // checkbox, and a lightning burn of one unused gift. No restore ad.
    static class Build82TestsE
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 82e Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82e] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            bool reduce = BackgroundMotion.ReduceMotion;
            bool hadAnim = PlayerPrefs.HasKey(BackgroundMotion.PrefKey);
            int anim = PlayerPrefs.GetInt(BackgroundMotion.PrefKey, 1);
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int bolt = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            bool force = LightningStrike.Force;
            try
            {
                CheckMotion(Check);
                CheckSettings(Check);
                CheckLightning(Check);
            }
            finally
            {
                BackgroundMotion.ReduceMotion = reduce;
                LightningStrike.Force = force;
                if (hadAnim) PlayerPrefs.SetInt(BackgroundMotion.PrefKey, anim);
                else PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
                if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, bolt);
                else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
                PlayerPrefs.Save();
            }

            if (fail > 0) throw new System.Exception("BUILD82E_FAIL " + fail + " passed " + pass);
            Debug.Log("BUILD82E_OK " + pass);
        }

        static void CheckMotion(System.Action<string, bool, string> Check)
        {
            var scenes = new[]
            {
                GardenScene.Summer, GardenScene.Desert, GardenScene.Fall,
                GardenScene.Winter, GardenScene.Spring
            };
            bool band = true;
            var bandDetail = "";
            for (int i = 0; i < scenes.Length; i++)
            {
                var t = SeasonMotion.For(scenes[i]);
                if (SeasonMotion.InBand(t)) continue;
                band = false;
                bandDetail = scenes[i] + " sway=" + t.Sway.ToString("0.00");
                break;
            }
            Check("season-channels-1.5-2", band, band ? "five scenes" : bandDetail);

            bool counts = true;
            string countDetail = "";
            for (int i = 0; i < scenes.Length; i++)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    bool low = pass == 0;
                    var b = SeasonMotion.Base(scenes[i], low);
                    var n = SeasonMotion.Lively(scenes[i], low);
                    int cap = low ? 16 : 32;
                    bool ok = n.Quads >= b.Quads
                        && n.Snow <= cap && n.Leaf <= cap && n.Petal <= cap
                        && n.Fly <= cap && n.Seed <= cap && n.Wisp <= cap
                        && n.Tumble == b.Tumble
                        && n.Cloud <= (low ? 4 : 6)
                        && n.Glint <= (low ? 4 : 8)
                        && n.Snow >= b.Snow && n.Cloud >= b.Cloud && n.Glint >= b.Glint;
                    if (ok) continue;
                    counts = false;
                    countDetail = scenes[i] + (low ? " low " : " desk ") + n.Quads + "/" + b.Quads;
                    break;
                }
                if (!counts) break;
            }
            var winter = SeasonMotion.Lively(GardenScene.Winter, false);
            var summer = SeasonMotion.Lively(GardenScene.Summer, false);
            Check("lively-counts-capped", counts && winter.Snow == 32 && summer.Fly > 6 && summer.Cloud > 3,
                counts ? "winter snow=" + winter.Snow + " summer fly=" + summer.Fly + " cloud=" + summer.Cloud : countDetail);

            PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
            BackgroundMotion.ReduceMotion = false;
            float leanOn = SeasonMotion.RainLean();
            BackgroundMotion.On = false;
            float leanOff = SeasonMotion.RainLean();
            Check("rain-lean-follows-switch",
                leanOff == 1f && leanOn >= SeasonMotion.LivelyMin && leanOn <= SeasonMotion.LivelyMax,
                "on=" + leanOn.ToString("0.00") + " off=" + leanOff.ToString("0.00"));

            SeasonMotion.WorstMicros(false, false, 8);
            float before = SeasonMotion.WorstMicros(false, false, 48);
            SeasonMotion.WorstMicros(true, false, 8);
            float after = SeasonMotion.WorstMicros(true, false, 48);
            float ratio = before > 0.01f ? after / before : 1f;
            Debug.Log("[build82e] frame-bench before=" + before.ToString("0.0")
                + "us after=" + after.ToString("0.0") + "us ratio=" + ratio.ToString("0.00"));
            Check("frame-step-stays-cheap", after < 2000f && (after < 500f || ratio < 4f),
                "before=" + before.ToString("0.0") + "us after=" + after.ToString("0.0") + "us");

            string sway = Read("Assets/_Project/Scripts/App/FoliageSway.cs");
            string life = Read("Assets/_Project/Scripts/App/GardenLife.cs");
            string shader = Read("Assets/_Project/Art/Resources/Shaders/FoliageSway.shader");
            int taps = 0;
            int at = 0;
            while ((at = shader.IndexOf("tex2D(", at)) >= 0) { taps++; at += 6; }
            Check("motion-uses-shared-tune",
                sway.IndexOf("BackgroundMotion.On") >= 0
                && sway.IndexOf("tune.Gust") >= 0
                && sway.IndexOf("tune.WaveHz") >= 0
                && life.IndexOf("BackgroundMotion.On") >= 0
                && life.IndexOf("HoldStill") >= 0
                && life.IndexOf("Cloud") >= 0
                && shader.IndexOf("_Wind.y < 0.05") >= 0
                && taps == 1,
                "taps=" + taps);

            string[] play =
            {
                "Assets/_Project/Scripts/View/BirdIdle.cs",
                "Assets/_Project/Scripts/View/BeeSwarm.cs",
                "Assets/_Project/Scripts/View/FeederView.cs",
                "Assets/_Project/Scripts/View/HawkView.cs",
                "Assets/_Project/Scripts/View/SparrowView.cs",
                "Assets/_Project/Scripts/View/BranchView.cs"
            };
            string leaked = null;
            for (int i = 0; i < play.Length; i++)
                if (Read(play[i]).IndexOf("BackgroundMotion") >= 0) leaked = play[i];
            Check("gameplay-anims-ignore-switch", leaked == null, leaked ?? "birds limbs bees feeders pests");
        }

        static void CheckSettings(System.Action<string, bool, string> Check)
        {
            var inner = new Rect(24f, 80f, 360f, 500f);
            var rows = SettingsSheet.Place(inner, 1f);
            bool under = rows.AnimateLabel.yMin >= rows.Label.yMax - 0.5f
                && rows.AnimateToggle.yMin >= rows.Toggle.yMax - 0.5f
                && !rows.AnimateLabel.Overlaps(rows.Label)
                && !rows.AnimateLabel.Overlaps(rows.Toggle)
                && !rows.AnimateToggle.Overlaps(rows.Toggle)
                && rows.AnimateToggle.width >= 44f
                && Mathf.Abs(rows.AnimateToggle.width - rows.Toggle.width) < 0.5f
                && rows.AnimateLabel.xMin == rows.Label.xMin;
            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            var frame = SettingsSheet.Layout(W, H, safe, 220f);
            bool inside = frame.Body.AnimateLabel.yMax <= frame.Content.yMax + 0.5f
                && frame.Body.AnimateToggle.xMax <= frame.Content.xMax + 0.5f
                && frame.Card.yMax - frame.Content.yMax > 8f;
            Check("animate-row-under-do-not-sell", under && inside && SettingsSheet.AnimateLabel == "Animate background",
                "w=" + rows.AnimateToggle.width.ToString("0.0") + " inside=" + inside);

            string ui = Read("Assets/_Project/Scripts/App/FlockFiveApp.Settings.cs");
            int sell = ui.IndexOf("DrawCheckbox(rows.Toggle");
            int anim = ui.IndexOf("DrawCheckbox(rows.AnimateToggle");
            int read = ui.IndexOf("bool animOn = BackgroundMotion.On");
            int assign = ui.IndexOf("BackgroundMotion.On = !animOn");
            Check("animate-checkbox-same-style",
                sell >= 0 && anim > sell && read > sell && assign > anim,
                "sell=" + sell + " anim=" + anim);

            PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
            BackgroundMotion.ReduceMotion = false;
            bool on = BackgroundMotion.On;
            BackgroundMotion.ReduceMotion = true;
            bool off = !BackgroundMotion.On;
            BackgroundMotion.On = false;
            BackgroundMotion.ReduceMotion = false;
            bool prefOff = !BackgroundMotion.On;
            BackgroundMotion.On = true;
            BackgroundMotion.ReduceMotion = true;
            bool prefOn = BackgroundMotion.On;
            Check("animate-default-and-pref", on && off && prefOff && prefOn,
                "defaultOn=" + on + " reduceOff=" + off + " pref0=" + prefOff + " pref1=" + prefOn);
        }

        static void CheckLightning(System.Action<string, bool, string> Check)
        {
            PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
            Check("lightning-default-off",
                !LightningStrike.Enabled && !LightningStrike.RemoteDefault
                && LightningStrike.PrefKey == "flockfive.lightning"
                && Mathf.Abs(LightningStrike.Chance - 0.45f) < 0.0001f
                && LightningStrike.MaxPerLevel == 1,
                "enabled=" + LightningStrike.Enabled);

            var open = Gift();
            var claimed = new bool[2];
            var burned = new bool[2];
            int hit = LightningStrike.Target(open, claimed, burned);
            Check("targets-unused-gift", hit == 1 && open.Branches[hit].AdLocked && open.Branches[hit].Count == 0,
                "index=" + hit);

            var normal = new Board();
            normal.Branches.Add(new BranchState());
            Check("skips-normal", LightningStrike.Target(normal, claimed, burned) < 0, "normal");

            var occupied = Gift();
            occupied.Branches[1].Birds.Add(new Bird(BirdColor.Ruby));
            Check("skips-occupied", LightningStrike.Target(occupied, claimed, burned) < 0, "occupied");

            var broken = Gift();
            broken.Branches[1].Broken = true;
            Check("skips-broken", LightningStrike.Target(broken, claimed, burned) < 0, "broken");

            var already = Gift();
            already.Branches[1].Burned = true;
            Check("skips-burned", LightningStrike.Target(already, claimed, burned) < 0, "burned");

            var took = new bool[] { true, false };
            Check("skips-claimed", LightningStrike.Target(Gift(), took, burned) < 0, "claimed");

            LightningStrike.Enabled = true;
            var win = EmptyGift();
            int struck = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.44f, false);
            int miss = LightningStrike.TryPick(win, claimed, burned, true, 0, 0.45f, false);
            int dry = LightningStrike.TryPick(win, claimed, burned, false, 0, 0.1f, false);
            int capped = LightningStrike.TryPick(win, claimed, burned, true, 1, 0.1f, true);
            int forced = LightningStrike.TryPick(win, claimed, burned, false, 0, 0.9f, true);
            Check("chance-storm-cap-force",
                LightningStrike.RollHits(0.44f) && !LightningStrike.RollHits(0.45f)
                && struck == 0 && miss < 0 && dry < 0 && capped < 0 && forced == 0,
                "struck=" + struck + " miss=" + miss + " dry=" + dry + " cap=" + capped + " force=" + forced);

            var stuck = new Board();
            var limb = new BranchState();
            limb.Birds.Add(new Bird(BirdColor.Ruby));
            stuck.Branches.Add(limb);
            stuck.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            bool keeps = LightningStrike.KeepsSolvable(EmptyGift(), 0);
            bool bricks = !LightningStrike.KeepsSolvable(stuck, 1);
            int noForce = LightningStrike.TryPick(stuck, claimed, burned, true, 0, 0f, true);
            Check("solvability-gates-strike", keeps && bricks && noForce < 0,
                "empty=" + keeps + " stuck=" + bricks + " force=" + noForce);

            var offer = Gift();
            var flags = new bool[2];
            LightningStrike.Burn(offer.Branches[1]);
            int claim = BonusBranches.Claim(offer, flags, 1);
            var copy = offer.Clone();
            Check("burn-does-not-claim",
                claim < 0 && !flags[0] && offer.Branches[1].Burned && offer.Branches[1].AdLocked
                && offer.Branches[1].Broken && copy.Branches[1].Burned,
                "claim=" + claim + " locked=" + offer.Branches[1].AdLocked);

            var heal = new Board();
            var gone = new BranchState { IsBonus = true, AdLocked = true, Broken = true, Burned = true };
            var snap = new BranchState { IsBonus = true, Broken = true };
            heal.Branches.Add(gone);
            heal.Branches.Add(snap);
            BoardValidator.Restore(heal, new int[Palette.Max]);
            var reopen = new bool[] { true };
            var burnedBoard = new Board();
            var burnedGift = new BranchState { IsBonus = true, AdLocked = true, Burned = true, Broken = true };
            burnedBoard.Branches.Add(burnedGift);
            BonusBranches.ApplyClaims(burnedBoard, reopen);
            Check("burn-stays-broken",
                gone.Broken && gone.Burned && !snap.Broken
                && burnedGift.AdLocked && burnedGift.Broken && burnedGift.Burned,
                "healBroken=" + gone.Broken + " snapBroken=" + snap.Broken + " locked=" + burnedGift.AdLocked);

            string strike = Read("Assets/_Project/Scripts/App/LightningStrike.cs");
            string fx = Read("Assets/_Project/Scripts/App/StrikeFx.cs");
            string tick = Read("Assets/_Project/Scripts/App/FlockFiveApp.Lightning.cs");
            string storm = Read("Assets/_Project/Scripts/App/GardenStorm.cs");
            string branch = Read("Assets/_Project/Scripts/View/BranchView.cs");
            string pest = Read("Assets/_Project/Scripts/Puzzle/PestPark.cs");
            bool noRestore = strike.IndexOf("branch_restore") < 0
                && strike.IndexOf("void Restore") < 0
                && fx.IndexOf("branch_restore") < 0
                && tick.IndexOf("branch_restore") < 0
                && tick.IndexOf("Restore branch") < 0;
            bool wow = storm.IndexOf("CamShake.Bolt") >= 0
                && storm.IndexOf("StrikeAt") >= 0
                && storm.IndexOf("_strikeDark") >= 0
                && branch.IndexOf("BeginScorch") >= 0
                && branch.IndexOf("StrikeFx.Play") >= 0
                && branch.IndexOf("Sfx.Crackle") >= 0
                && branch.IndexOf("BreakAway") >= 0
                && fx.IndexOf("Kind == 1") >= 0
                && fx.IndexOf("Kind == 2") >= 0
                && fx.IndexOf("Kind == 3") >= 0;
            bool once = tick.IndexOf("_lightningRolled = false") >= 0
                && tick.IndexOf("LightningStrike.TryPick") >= 0
                && pest.IndexOf("!src.Burned") >= 0;
            Check("no-restore-ad-and-wow", noRestore && wow && once, "restore=" + noRestore + " wow=" + wow + " once=" + once);
        }

        static Board Gift()
        {
            var b = new Board();
            b.Branches.Add(new BranchState());
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            return b;
        }

        static Board EmptyGift()
        {
            var b = new Board();
            b.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            return b;
        }

        static string Read(string rel)
        {
            string path = Path.Combine(Application.dataPath, "..", rel);
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }
    }
}
#endif
