#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Fire odds climb until a burn, then reset. Gifts go first. A normal limb
    // burns only when the puzzle stays winnable, and only if no gift is waiting.
    public static class LightningFireTests
    {
        const string Out = "/tmp/flock-five-lightning-fire-tests.txt";

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
                Debug.Log("[lightning-fire] " + line);
            }

            bool steps = true;
            for (int i = 0; i < 19; i++)
            {
                float expect = Mathf.Min(1f, 0.10f + 0.05f * i);
                if (Mathf.Abs(LightningFire.Chance(i) - expect) > 0.0001f) steps = false;
            }
            bool neg = Mathf.Abs(LightningFire.Chance(-3) - 0.10f) < 0.0001f;
            bool cap = Mathf.Abs(LightningFire.Chance(40) - 1f) < 0.0001f;
            Check("chance-steps", steps && neg && cap,
                "m0=" + LightningFire.Chance(0).ToString("0.00")
                + " m1=" + LightningFire.Chance(1).ToString("0.00")
                + " m2=" + LightningFire.Chance(2).ToString("0.00")
                + " m18=" + LightningFire.Chance(18).ToString("0.00"));

            bool edges = LightningFire.RollHits(0, 0.099f) && !LightningFire.RollHits(0, 0.10f)
                && LightningFire.RollHits(1, 0.149f) && !LightningFire.RollHits(1, 0.15f)
                && LightningFire.RollHits(2, 0.199f) && !LightningFire.RollHits(2, 0.20f)
                && LightningFire.RollHits(18, 0.999f) && !LightningFire.RollHits(18, 1f);
            Check("roll-boundaries", edges, "10 then 15 then 20, cap at 100");

            var spare = Spare();
            int m = 0;
            var missA = LightningFire.Resolve(spare, null, null, m, 0.10f, false);
            m = missA.Misses;
            var missB = LightningFire.Resolve(spare, null, null, m, 0.15f, false);
            m = missB.Misses;
            var missC = LightningFire.Resolve(spare, null, null, m, 0.20f, false);
            m = missC.Misses;
            bool climbed = !missA.Fired && !missB.Fired && !missC.Fired
                && missA.Misses == 1 && missB.Misses == 2 && missC.Misses == 3
                && Mathf.Abs(LightningFire.Chance(m) - 0.25f) < 0.0001f;
            var hit = LightningFire.Resolve(spare, null, null, m, 0f, false);
            var again = LightningFire.Resolve(spare, null, null, hit.Misses, 0.10f, false);
            Check("miss-climbs-fire-resets",
                climbed && hit.Fired && hit.Misses == 0 && hit.Index == 1
                && !again.Fired && again.Misses == 1,
                "streak=" + m + " hit=" + hit.Index + " after=" + again.Misses);

            var lone = new Board();
            var bird = new BranchState();
            bird.Birds.Add(new Bird(BirdColor.Ruby));
            lone.Branches.Add(bird);
            var kept = LightningFire.Resolve(lone, null, null, 4, 0f, true);
            var forced = LightningFire.Resolve(spare, null, null, 6, 0.99f, true);
            Check("no-limb-keeps-climbing",
                !kept.Fired && kept.Index < 0 && kept.Misses == 5
                && forced.Fired && forced.Misses == 0 && forced.Index == 1,
                "stuck=" + kept.Misses + " force=" + forced.Index);

            var both = Spare();
            both.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            int ad = LightningFire.Target(both, new bool[2], new bool[2]);
            Check("ad-branch-first", ad == 2 && both.Branches[ad].IsBonus,
                "index=" + ad);

            int normal = LightningFire.Target(Spare(), new bool[2], new bool[2]);
            bool spareWins = LightningFire.KeepsSolvable(Spare(), 1);
            Check("normal-when-solvable", normal == 1 && spareWins, "index=" + normal);

            var blocked = NeedsEmpty();
            bool loaded = GardenSolve.Look(blocked, LightningFire.SolveCap) == GardenSolve.Outlook.Winnable;
            int no = LightningFire.Target(blocked, null, null);
            bool bricks = !LightningFire.KeepsSolvable(blocked, 2);
            Check("normal-only-when-solvable", loaded && bricks && no < 0,
                "winnable=" + loaded + " bricks=" + bricks + " target=" + no);

            var claimed = Spare();
            claimed.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            int fell = LightningFire.Target(claimed, new[] { true, false }, new bool[2]);
            var burned = Spare();
            burned.Branches.Add(new BranchState { IsBonus = true, AdLocked = true, Burned = true, Broken = true });
            int skipBurn = LightningFire.Target(burned, new bool[2], new[] { true, false });
            Check("skip-claimed-and-burned", fell == 1 && skipBurn == 1,
                "claimed=" + fell + " burned=" + skipBurn);

            // A gift with birds is not an offer. A complete set on it still
            // collects, so the spare empty perch can burn. A trapped single
            // bird means the puzzle is not winnable, so the bolt stays dry.
            var seated = new Board();
            seated.Live = new BirdColor?[] { BirdColor.Ruby, BirdColor.Gold };
            var rubies = new BranchState();
            for (int i = 0; i < BranchState.Cap; i++) rubies.Birds.Add(new Bird(BirdColor.Ruby));
            var gift = new BranchState { IsBonus = true, AdLocked = true };
            for (int i = 0; i < BranchState.Cap; i++) gift.Birds.Add(new Bird(BirdColor.Gold));
            seated.Branches.Add(rubies);
            seated.Branches.Add(new BranchState());
            seated.Branches.Add(gift);
            int skipOcc = LightningFire.Target(seated, new bool[2], new bool[2]);
            var trapped = Spare();
            var stuckGift = new BranchState { IsBonus = true, AdLocked = true };
            stuckGift.Birds.Add(new Bird(BirdColor.Gold));
            trapped.Branches.Add(stuckGift);
            int noOcc = LightningFire.Target(trapped, new bool[2], new bool[2]);
            Check("occupied-gift-is-not-the-offer",
                skipOcc == 1 && noOcc < 0 && noOcc != 2,
                "seated=" + skipOcc + " trapped=" + noOcc);

            var stuck = new Board();
            var one = new BranchState();
            one.Birds.Add(new Bird(BirdColor.Ruby));
            stuck.Branches.Add(one);
            stuck.Branches.Add(new BranchState());
            stuck.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            int none = LightningFire.Target(stuck, new bool[2], new bool[2]);
            Check("unsolvable-strikes-with-no-fire", none < 0, "target=" + none);

            string root = Application.dataPath;
            string fire = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/LightningFire.cs"));
            string tick = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Lightning.cs"));
            string storm = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/GardenStorm.cs"));
            string branch = File.ReadAllText(Path.Combine(root, "_Project/Scripts/View/BranchView.cs"));
            string fx = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/StrikeFx.cs"));
            int offer = tick.IndexOf("bool OfferFire");
            int apply = tick.IndexOf("void ApplyFire", offer < 0 ? 0 : offer);
            string body = offer >= 0 && apply > offer ? tick.Substring(offer, apply - offer) : "";
            bool always = fire.IndexOf("PlayerPrefs") < 0 && fire.IndexOf("PrefKey") < 0
                && fire.IndexOf("Enabled") < 0
                && body.IndexOf("LightningStrike.Enabled") < 0
                && body.IndexOf("LightningFire.Resolve") >= 0
                && storm.IndexOf("FireAim") >= 0;
            bool show = storm.IndexOf("_strikeDark") >= 0
                && storm.IndexOf("CamShake.Bolt") >= 0
                && storm.IndexOf("SpawnBolt") >= 0
                && branch.IndexOf("Sfx.Crackle") >= 0
                && branch.IndexOf("StrikeFx.Play") >= 0
                && branch.IndexOf("BeginScorch") >= 0
                && fx.IndexOf("Kind == 1") >= 0
                && fx.IndexOf("Kind == 2") >= 0
                && fx.IndexOf("Kind == 3") >= 0
                && fx.IndexOf("Kind == 4") >= 0;
            Check("always-on-showpiece", always && show,
                "always=" + always + " show=" + show);

            string lineEnd = fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass;
            sb.AppendLine(lineEnd);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[lightning-fire] " + lineEnd);
            if (fail > 0) throw new System.Exception("lightning-fire " + fail + " failed");
        }

        // Five rubies already matched, plus an empty perch the puzzle does not need.
        static Board Spare()
        {
            var b = new Board();
            b.Live = new BirdColor?[] { BirdColor.Ruby, null };
            var full = new BranchState();
            for (int i = 0; i < BranchState.Cap; i++)
                full.Birds.Add(new Bird(BirdColor.Ruby));
            b.Branches.Add(full);
            b.Branches.Add(new BranchState());
            return b;
        }

        // The empty perch is the only way to unbury the two fives.
        static Board NeedsEmpty()
        {
            var b = new Board();
            b.Live = new BirdColor?[] { BirdColor.Ruby, BirdColor.Gold };
            var ruby = new BranchState();
            for (int i = 0; i < 4; i++) ruby.Birds.Add(new Bird(BirdColor.Ruby));
            ruby.Birds.Add(new Bird(BirdColor.Gold));
            var gold = new BranchState();
            for (int i = 0; i < 4; i++) gold.Birds.Add(new Bird(BirdColor.Gold));
            gold.Birds.Add(new Bird(BirdColor.Ruby));
            b.Branches.Add(ruby);
            b.Branches.Add(gold);
            b.Branches.Add(new BranchState());
            return b;
        }
    }
}
#endif
