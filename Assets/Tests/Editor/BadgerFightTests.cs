#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Rules for the honey badger contest. Menu, or drop a file on /tmp/flock-five-badger-tests.
    [InitializeOnLoad]
    static class BadgerFightTests
    {
        const string Cmd = "/tmp/flock-five-badger-tests";
        const string Out = "/tmp/flock-five-badger-tests.txt";

        static BadgerFightTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Badger Fight Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[badger] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            Purse.Boot();
            int keep = Purse.Coins;
            try
            {
                CheckDue(Check);
                CheckTargets(Check);
                CheckPrice(Check);
                CheckMix(Check);
                CheckHoney(Check);
                CheckCompare(Check);
                CheckTie(Check);
                CheckBlock(Check);
                CheckMultiplier(Check);
                CheckEmpty(Check);
                CheckExhaust(Check);
                CheckTargetWin(Check);
                CheckTieFight(Check);
                CheckPurse(Check);
                CheckSeed(Check);
            }
            finally
            {
                SetCoins(keep);
            }

            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }

        static void CheckDue(System.Action<string, bool, string> Check)
        {
            bool yes = BadgerSchedule.Due(15) && BadgerSchedule.Appearance(15) == 1
                && BadgerSchedule.Due(20) && BadgerSchedule.Appearance(20) == 2
                && BadgerSchedule.Due(25) && BadgerSchedule.Appearance(25) == 3;
            bool no = !BadgerSchedule.Due(16) && !BadgerSchedule.Due(19) && !BadgerSchedule.Due(10);
            Check("due", yes && no, "15/20/25 due (visits 1/2/3); 16/19/10 are not");
        }

        static void CheckTargets(System.Action<string, bool, string> Check)
        {
            bool player = BadgerSchedule.PlayerTarget(1) == 10
                && BadgerSchedule.PlayerTarget(2) == 11
                && BadgerSchedule.PlayerTarget(8) == 17
                && BadgerSchedule.PlayerTarget(9) == 18
                && BadgerSchedule.PlayerTarget(10) == 18
                && BadgerSchedule.PlayerTarget(100) == 18;
            bool badger = BadgerSchedule.BadgerTarget(1) == 18
                && BadgerSchedule.BadgerTarget(9) == 18
                && BadgerSchedule.BadgerTarget(100) == 18;
            Check("targets", player && badger, "player 10+(n-1) cap 18; badger 18");
        }

        static void CheckPrice(System.Action<string, bool, string> Check)
        {
            bool bases = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 1) == 100
                && BadgerSchedule.PowerUpPrice(BadgerPower.X3, 1) == 200
                && BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 1) == 300
                && BadgerSchedule.PowerUpPrice(BadgerPower.Pepper, 1) == 300;
            bool step = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 2) == 150
                && BadgerSchedule.PowerUpPrice(BadgerPower.X3, 2) == 250
                && BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 2) == 350
                && BadgerSchedule.PowerUpPrice(BadgerPower.Pepper, 3) == 400;
            bool pair = BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 6)
                == BadgerSchedule.PowerUpPrice(BadgerPower.Pepper, 6);
            int far = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 100);
            int near = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 99);
            bool open = far == 100 + 50 * 99 && far - near == 50
                && BadgerSchedule.PowerUpPrice(BadgerPower.Pepper, 40) == 300 + 50 * 39
                && BadgerSchedule.PowerUpPrice(BadgerPower.None, 5) == 0;
            Check("price", bases && step && pair && open, "base + 50 per visit, no cap, sauce == pepper");
        }

        static void CheckMix(System.Action<string, bool, string> Check)
        {
            bool shape = true;
            bool climb = true;
            bool stable = true;
            string why = "";
            int prevSum = -1;
            int prevFive = -1;
            for (int visit = 1; visit <= 7; visit++)
            {
                var first = BadgerSchedule.BossTileMix(visit, 800 + visit);
                var second = BadgerSchedule.BossTileMix(visit, 800 + visit);
                if (!Same(first, second)) stable = false;
                string bad;
                if (!Shape(first, out bad))
                {
                    shape = false;
                    why = "visit " + visit + " " + bad;
                }
                if (!Rises(first)) climb = false;
                int sum = Sum(first);
                int fives = Count(first, 5);
                if (visit > 1 && (sum <= prevSum || fives <= prevFive)) climb = false;
                prevSum = sum;
                prevFive = fives;
            }

            var hot = BadgerSchedule.BossTileMix(7, 3);
            var later = BadgerSchedule.BossTileMix(50, 4);
            var again = BadgerSchedule.BossTileMix(50, 4);
            bool stayed = Sum(hot) == Sum(later)
                && Count(hot, 5) == Count(later, 5)
                && Rises(later)
                && Shape(later, out _)
                && Same(later, again)
                && Count(later, 1) == 2
                && later.Length == BadgerSchedule.Tiles;
            var bagA = BadgerSchedule.BossTileMix(4, 1);
            var bagB = BadgerSchedule.BossTileMix(4, 2);
            bool bag = true;
            for (int face = 1; face <= 5; face++)
                if (Count(bagA, face) != Count(bagB, face)) bag = false;
            var early = BadgerSchedule.BossTileMix(1, 5);
            bool mixed = Count(early, 2) > 0 && Count(early, 3) > Count(early, 2)
                && Count(early, 4) > Count(early, 3) && Count(early, 5) > Count(early, 4);

            Check("mix-shape", shape, why.Length == 0 ? "16 tiles, exactly two 1s, values 1-5" : why);
            Check("mix-up", climb && mixed, "each visit through 7 is hotter and still rises");
            Check("mix-stay", stayed && bag && stable, "visit 50 stays on the hot row; seed is stable");
        }

        static void CheckHoney(System.Action<string, bool, string> Check)
        {
            bool faces = Hive.HoneyOfFinish(BeeFinish.Normal) == 2
                && Hive.HoneyOfFinish(BeeFinish.Holo) == 3
                && Hive.HoneyOfFinish(BeeFinish.InverseRainbow) == 5;
            var owned = new[]
            {
                Hive.HoneyOfFinish(BeeFinish.Normal),
                Hive.HoneyOfFinish(BeeFinish.InverseRainbow),
                Hive.HoneyOfFinish(BeeFinish.Holo),
            };
            var row = BadgerSchedule.ArrangeLoadout(owned, 5);
            bool order = row.Length == 5 && row[0] == 5 && row[1] == 3 && row[2] == 2
                && row[3] == 1 && row[4] == 1;
            var empty = BadgerSchedule.ArrangeLoadout(null, 4);
            bool pads = empty.Length == 4 && empty[0] == 1 && empty[1] == 1 && empty[2] == 1 && empty[3] == 1;
            var many = new int[20];
            for (int i = 0; i < many.Length; i++) many[i] = 2;
            many[4] = 5;
            many[9] = 3;
            var top = BadgerSchedule.ArrangeLoadout(many, BadgerSchedule.Tiles);
            bool trimmed = top.Length == 16 && top[0] == 5 && top[1] == 3 && top[2] == 2 && top[15] == 2;

            var live = BadgerSchedule.PlayerLoadout();
            bool sized = live != null && live.Length == 16;
            bool sorted = true;
            bool legal = true;
            bool suffix = true;
            bool seenPad = false;
            if (live != null)
            {
                for (int slot = 0; slot < live.Length; slot++)
                {
                    int honey = live[slot];
                    if (honey != 1 && honey != 2 && honey != 3 && honey != 5) legal = false;
                    if (slot > 0 && honey > live[slot - 1]) sorted = false;
                    if (honey == 1) seenPad = true;
                    else if (seenPad) suffix = false;
                }
            }

            Check("honey-finish", faces, "regular 2, foil 3, Inverse Rainbow 5");
            Check("loadout", order && pads && trimmed, "best honey first, yard 1 pads, 16 best kept");
            Check("loadout-album", sized && sorted && legal && suffix, "album tiles are 16, best first, 1s at the end");
        }

        static void CheckCompare(System.Action<string, bool, string> Check)
        {
            int playerFinal;
            int bossFinal;
            int playerGain;
            int bossGain;
            BadgerFight.ApplyRound(4, 2, BadgerPower.None, out playerFinal, out bossFinal, out playerGain, out bossGain);
            bool math = playerFinal == 4 && bossFinal == 2 && playerGain == 4 && bossGain == 0;

            var fight = new BadgerFight(1, 6, Fill(16, 4), Fill(16, 2));
            bool early = !fight.PlayerPick(0) && fight.PlayerOpen(0) && fight.PlayerLeft == 16;
            BadgerRound round;
            bool played = Play(fight, 4, BadgerPower.None, out round);
            BadgerRound extra;
            bool again = !fight.Resolve(out extra);
            bool scored = played && early && again
                && round.PlayerGained == 4 && round.BossGained == 0
                && fight.PlayerScore == 4 && fight.BossScore == 0
                && !fight.BossOpen(round.BossIndex) && !fight.PlayerOpen(round.PlayerIndex);
            Check("compare", math && scored, "higher honey scores that honey");
        }

        static void CheckTie(System.Action<string, bool, string> Check)
        {
            int playerFinal;
            int bossFinal;
            int playerGain;
            int bossGain;
            BadgerFight.ApplyRound(3, 3, BadgerPower.None, out playerFinal, out bossFinal, out playerGain, out bossGain);
            bool math = playerFinal == 3 && bossFinal == 3 && playerGain == 0 && bossGain == 0;

            var fight = new BadgerFight(1, 7, Fill(16, 3), Fill(16, 3));
            BadgerRound round;
            bool played = Play(fight, 3, BadgerPower.None, out round);
            bool quiet = played && math && round.PlayerGained == 0 && round.BossGained == 0
                && fight.PlayerScore == 0 && fight.BossScore == 0
                && fight.Result == BadgerResult.Playing
                && !fight.BossOpen(round.BossIndex) && !fight.PlayerOpen(round.PlayerIndex);
            Check("tie", quiet, "equal finals score nothing");
        }

        static void CheckBlock(System.Action<string, bool, string> Check)
        {
            int sauceFinal;
            int sauceBoss;
            int sauceGain;
            int sauceBossGain;
            int pepperFinal;
            int pepperBoss;
            int pepperGain;
            int pepperBossGain;
            BadgerFight.ApplyRound(2, 5, BadgerPower.HotSauce, out sauceFinal, out sauceBoss, out sauceGain, out sauceBossGain);
            BadgerFight.ApplyRound(2, 5, BadgerPower.Pepper, out pepperFinal, out pepperBoss, out pepperGain, out pepperBossGain);
            bool same = sauceFinal == pepperFinal && sauceBoss == pepperBoss
                && sauceGain == pepperGain && sauceBossGain == pepperBossGain
                && sauceFinal == 2 && sauceBoss == 0 && sauceGain == 2 && sauceBossGain == 0;
            int plainFinal;
            int plainBoss;
            int plainGain;
            int plainBossGain;
            BadgerFight.ApplyRound(2, 5, BadgerPower.None, out plainFinal, out plainBoss, out plainGain, out plainBossGain);
            bool unblocked = plainGain == 0 && plainBossGain == 5 && plainBoss == 5;

            SetCoins(5000);
            int before = Purse.Coins;
            var sauce = new BadgerFight(1, 21, Fill(16, 2), Fill(16, 5));
            int bossIx = sauce.BossPick();
            bool picked = bossIx >= 0 && sauce.PlayerPick(0);
            bool bought = sauce.TryPower(BadgerPower.HotSauce);
            int price = BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 1);
            BadgerRound sauceRound = default;
            bool resolved = bought && sauce.Resolve(out sauceRound);
            bool effect = picked && resolved && Purse.Coins == before - price
                && sauceRound.BossFinal == 0 && sauceRound.BossGained == 0 && sauceRound.PlayerGained == 2
                && sauce.PlayerScore == 2 && sauce.BossScore == 0
                && !sauce.BossOpen(bossIx) && !sauce.PlayerOpen(0)
                && !sauce.PowerReady(BadgerPower.HotSauce);
            bool second = sauce.BossPick() >= 0 && sauce.PlayerPick(1)
                && !sauce.TryPower(BadgerPower.HotSauce)
                && Purse.Coins == before - price;

            BadgerRound pepperRound;
            var pepper = new BadgerFight(1, 22, Fill(16, 2), Fill(16, 5));
            bool pepperOk = Play(pepper, 2, BadgerPower.Pepper, out pepperRound)
                && pepperRound.BossFinal == 0 && pepperRound.BossGained == 0 && pepperRound.PlayerGained == 2
                && pepper.BossScore == 0
                && !pepper.BossOpen(pepperRound.BossIndex) && !pepper.PlayerOpen(pepperRound.PlayerIndex);

            Check("block-sauce", same && unblocked && effect && second, "sauce knocks badger honey to 0; one use");
            Check("block-pepper", pepperOk && same, "pepper matches sauce");
        }

        static void CheckMultiplier(System.Action<string, bool, string> Check)
        {
            int flipFinal;
            int flipBoss;
            int flipGain;
            int flipBossGain;
            BadgerFight.ApplyRound(2, 5, BadgerPower.X3, out flipFinal, out flipBoss, out flipGain, out flipBossGain);
            bool flip = flipFinal == 6 && flipBoss == 5 && flipGain == 6 && flipBossGain == 0;

            int tieFinal;
            int tieBoss;
            int tieGain;
            int tieBossGain;
            BadgerFight.ApplyRound(2, 4, BadgerPower.X2, out tieFinal, out tieBoss, out tieGain, out tieBossGain);
            bool tied = tieFinal == 4 && tieBoss == 4 && tieGain == 0 && tieBossGain == 0;

            int loseFinal;
            int loseBoss;
            int loseGain;
            int loseBossGain;
            BadgerFight.ApplyRound(2, 5, BadgerPower.X2, out loseFinal, out loseBoss, out loseGain, out loseBossGain);
            bool lose = loseFinal == 4 && loseBoss == 5 && loseGain == 0 && loseBossGain == 5;

            SetCoins(1000);
            var fight = new BadgerFight(1, 30, Fill(16, 2), Fill(16, 5));
            int start = Purse.Coins;
            bool tooSoon = !fight.TryPower(BadgerPower.X3) && Purse.Coins == start && fight.PowerReady(BadgerPower.X3);
            BadgerRound boosted;
            bool first = Play(fight, 2, BadgerPower.X3, out boosted);
            int after = Purse.Coins;
            bool priced = tooSoon && first && after == start - BadgerSchedule.PowerUpPrice(BadgerPower.X3, 1)
                && boosted.PlayerFinal == 6 && boosted.PlayerGained == 6 && boosted.BossGained == 0;

            bool opened = fight.BossPick() >= 0 && fight.PlayerPick(1);
            bool spent = opened && !fight.TryPower(BadgerPower.X3) && !fight.PowerReady(BadgerPower.X3)
                && Purse.Coins == after;
            BadgerRound plain = default;
            bool raw = spent && fight.Resolve(out plain);
            bool once = priced && raw && plain.PlayerFinal == 2 && plain.BossGained == 5
                && fight.PlayerScore == 6 && fight.BossScore == 5;

            Check("multiplier", flip && tied && lose && once, "x3 scores 6, x2 can tie or lose, does not stick");
        }

        static void CheckEmpty(System.Action<string, bool, string> Check)
        {
            var dry = new BadgerFight(1, 1, new int[0], new int[0]);
            bool gone = dry.Result == BadgerResult.BadgerWon
                && dry.PlayerScore == 0 && dry.BossScore == 0
                && dry.PlayerLeft == 0 && dry.BossLeft == 0
                && dry.BossPick() < 0;
            Check("empty-grid", gone, "no tiles, 0-0, badger wins");
        }

        static void CheckExhaust(System.Action<string, bool, string> Check)
        {
            var lead = new BadgerFight(1, 12, TwoHigh(4, 3), Fill(16, 3));
            int fours = 2;
            int leadRounds = 0;
            bool leadOk = true;
            while (lead.Result == BadgerResult.Playing && leadRounds < 20)
            {
                leadRounds++;
                int want = fours > 0 ? 4 : 3;
                if (fours > 0) fours--;
                BadgerRound ignored;
                if (!Play(lead, want, BadgerPower.None, out ignored)) leadOk = false;
            }
            bool playerWins = leadOk && leadRounds == 16
                && lead.PlayerScore == 8 && lead.BossScore == 0
                && lead.Result == BadgerResult.PlayerWon
                && lead.PlayerLeft == 0 && lead.BossLeft == 0;

            var trail = new BadgerFight(1, 13, Fill(16, 3), TwoHigh(4, 3));
            int trailRounds = 0;
            bool trailOk = true;
            while (trail.Result == BadgerResult.Playing && trailRounds < 20)
            {
                trailRounds++;
                BadgerRound ignoredRound;
                if (!Play(trail, 3, BadgerPower.None, out ignoredRound)) trailOk = false;
            }
            bool badgerWins = trailOk && trailRounds == 16
                && trail.PlayerScore == 0 && trail.BossScore == 8
                && trail.Result == BadgerResult.BadgerWon
                && trail.PlayerLeft == 0 && trail.BossLeft == 0;

            Check("exhaust-player", playerWins, "8 to 0 under the targets, grids empty, player wins");
            Check("exhaust-badger", badgerWins, "0 to 8 under the targets, grids empty, badger wins");
        }

        static void CheckTargetWin(System.Action<string, bool, string> Check)
        {
            var first = new BadgerFight(1, 4, Fill(16, 5), Fill(16, 1));
            BadgerRound earlyA;
            BadgerRound earlyB;
            bool two = Play(first, 5, BadgerPower.None, out earlyA)
                && Play(first, 5, BadgerPower.None, out earlyB);
            bool firstWin = two && first.Result == BadgerResult.PlayerWon
                && first.PlayerScore == 10 && first.BossScore == 0
                && first.BossPick() < 0 && first.PlayerScore == 10;

            var high = new BadgerFight(20, 4, Fill(16, 5), Fill(16, 1));
            BadgerRound midA;
            BadgerRound midB;
            BadgerRound midC;
            bool three = Play(high, 5, BadgerPower.None, out midA)
                && Play(high, 5, BadgerPower.None, out midB)
                && Play(high, 5, BadgerPower.None, out midC);
            bool still = three && high.Result == BadgerResult.Playing
                && high.PlayerScore == 15 && high.PlayerTarget == 18 && high.BadgerTarget == 18;
            BadgerRound midD;
            bool fourth = still && Play(high, 5, BadgerPower.None, out midD);
            bool capped = fourth && high.Result == BadgerResult.PlayerWon && high.PlayerScore == 20;

            var rival = new BadgerFight(1, 8, Fill(16, 1), Fill(16, 5));
            int steps = 0;
            bool walked = true;
            while (rival.Result == BadgerResult.Playing && steps < 6)
            {
                steps++;
                BadgerRound stepRound;
                if (!Play(rival, 1, BadgerPower.None, out stepRound)) walked = false;
            }
            bool badgerHit = walked && steps == 4 && rival.BossScore == 20
                && rival.PlayerScore == 0 && rival.Result == BadgerResult.BadgerWon;

            bool scoreTie = BadgerFight.Decide(6, 6, 10, 18, false) == BadgerResult.BadgerWon;
            bool scoreLead = BadgerFight.Decide(6, 4, 10, 18, false) == BadgerResult.PlayerWon;
            bool scoreTrail = BadgerFight.Decide(4, 6, 10, 18, false) == BadgerResult.BadgerWon;
            bool scoreLive = BadgerFight.Decide(6, 6, 10, 18, true) == BadgerResult.Playing;
            bool targetFirst = BadgerFight.Decide(10, 0, 10, 18, false) == BadgerResult.PlayerWon;

            Check("win-player", firstWin, "visit 1 target 10");
            Check("win-cap", capped, "visit 20 still targets 18, so 15 is short and 20 wins");
            Check("win-badger", badgerHit, "badger target 18");
            Check("tie-exact", scoreTie && scoreLead && scoreTrail && scoreLive && targetFirst,
                "equal scores on an empty grid go to the badger");
        }

        static void CheckPurse(System.Action<string, bool, string> Check)
        {
            SetCoins(0);
            var broke = new BadgerFight(1, 40, Fill(16, 4), Fill(16, 1));
            bool dealt = broke.BossPick() >= 0 && broke.PlayerPick(0);
            bool refused = dealt && !broke.TryPower(BadgerPower.X2)
                && Purse.Coins == 0 && broke.PowerReady(BadgerPower.X2);
            BadgerRound free = default;
            bool played = refused && broke.Resolve(out free);
            bool live = played && free.PlayerGained == 4 && free.BossGained == 0
                && broke.PlayerScore == 4 && Purse.Coins == 0;

            SetCoins(300);
            var shorty = new BadgerFight(3, 41, Fill(16, 2), Fill(16, 5));
            int cost = BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 3);
            bool opened = shorty.BossPick() >= 0 && shorty.PlayerPick(0);
            int leftBefore = shorty.BossLeft;
            int scoreBefore = shorty.BossScore;
            bool noBuy = opened && !shorty.TryPower(BadgerPower.HotSauce);
            bool clean = noBuy && cost == 400 && Purse.Coins == 300
                && shorty.PowerReady(BadgerPower.HotSauce)
                && shorty.BossLeft == leftBefore
                && shorty.BossScore == scoreBefore
                && shorty.Result == BadgerResult.Playing;
            BadgerRound rawRound = default;
            bool raw = clean && shorty.Resolve(out rawRound);
            bool unblocked = raw && rawRound.BossFinal == 5 && rawRound.BossGained == 5 && Purse.Coins == 300;

            SetCoins(400);
            bool next = shorty.BossPick() >= 0 && shorty.PlayerPick(1)
                && shorty.TryPower(BadgerPower.HotSauce) && Purse.Coins == 0;
            BadgerRound blockedRound;
            bool blocked = next && shorty.Resolve(out blockedRound)
                && blockedRound.BossFinal == 0 && blockedRound.BossGained == 0
                && blockedRound.PlayerGained == 2
                && !shorty.PowerReady(BadgerPower.HotSauce);
            bool third = shorty.BossPick() >= 0 && shorty.PlayerPick(2)
                && !shorty.TryPower(BadgerPower.HotSauce) && Purse.Coins == 0;

            Check("purse-zero", live, "0 coins can still play; x2 is refused");
            Check("purse-short", unblocked && blocked && third, "short purse changes nothing; a full purse then buys once");
        }

        static void CheckSeed(System.Action<string, bool, string> Check)
        {
            var mix = BadgerSchedule.BossTileMix(1, 99);
            var left = new BadgerFight(1, 5, Fill(16, 2), mix);
            var right = new BadgerFight(1, 5, Fill(16, 2), mix);
            bool match = true;
            for (int step = 0; step < 3; step++)
            {
                int leftIx = left.BossPick();
                int rightIx = right.BossPick();
                if (leftIx < 0 || leftIx != rightIx) match = false;
                if (!left.PlayerPick(step) || !right.PlayerPick(step)) match = false;
                BadgerRound leftRound;
                BadgerRound rightRound;
                if (!left.Resolve(out leftRound) || !right.Resolve(out rightRound)) match = false;
            }
            bool still = match && left.Result == BadgerResult.Playing && right.Result == BadgerResult.Playing;
            Check("boss-seed", still, "same seed picks the same three tiles");
        }

        static void CheckTieFight(System.Action<string, bool, string> Check)
        {
            var even = new BadgerFight(1, 2, Fill(16, 2), Fill(16, 2));
            int rounds = 0;
            bool all = true;
            while (even.Result == BadgerResult.Playing && rounds < 20)
            {
                rounds++;
                BadgerRound round;
                if (!Play(even, 2, BadgerPower.None, out round)) all = false;
                else if (round.PlayerGained != 0 || round.BossGained != 0) all = false;
            }
            bool badger = all && rounds == 16 && even.PlayerScore == 0 && even.BossScore == 0
                && even.Result == BadgerResult.BadgerWon
                && even.PlayerLeft == 0 && even.BossLeft == 0;
            Check("tie-grid", badger, "sixteen pushes, 0-0, badger takes it");
        }

        static bool Play(BadgerFight fight, int honey, BadgerPower power, out BadgerRound round)
        {
            round = default;
            if (fight.BossPick() < 0) return false;
            int ix = FindPlayer(fight, honey);
            if (ix < 0) return false;
            if (!fight.PlayerPick(ix)) return false;
            if (power != BadgerPower.None && !fight.TryPower(power)) return false;
            return fight.Resolve(out round);
        }

        static int FindPlayer(BadgerFight fight, int honey)
        {
            for (int i = 0; i < fight.PlayerCount; i++)
            {
                if (!fight.PlayerOpen(i)) continue;
                if (fight.PlayerHoney(i) == honey) return i;
            }
            return -1;
        }

        static int[] Fill(int count, int honey)
        {
            var tiles = new int[count];
            for (int i = 0; i < count; i++) tiles[i] = honey;
            return tiles;
        }

        static int[] TwoHigh(int high, int low)
        {
            var tiles = new int[16];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = i < 2 ? high : low;
            return tiles;
        }

        static bool Shape(int[] tiles, out string why)
        {
            why = "";
            if (tiles == null || tiles.Length != BadgerSchedule.Tiles)
            {
                why = "len";
                return false;
            }
            int ones = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                int face = tiles[i];
                if (face < 1 || face > 5)
                {
                    why = "face " + face;
                    return false;
                }
                if (face == 1) ones++;
            }
            if (ones != 2)
            {
                why = "ones " + ones;
                return false;
            }
            return true;
        }

        static bool Rises(int[] tiles)
        {
            int twos = Count(tiles, 2);
            int threes = Count(tiles, 3);
            int fours = Count(tiles, 4);
            int fives = Count(tiles, 5);
            return threes > twos && fours > threes && fives > fours;
        }

        static int Count(int[] tiles, int face)
        {
            int n = 0;
            if (tiles == null) return 0;
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i] == face) n++;
            return n;
        }

        static int Sum(int[] tiles)
        {
            int total = 0;
            if (tiles == null) return 0;
            for (int i = 0; i < tiles.Length; i++) total += tiles[i];
            return total;
        }

        static bool Same(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        static void SetCoins(int n)
        {
            n = Mathf.Max(0, n);
            int c = Purse.Coins;
            if (c < n) Purse.Credit(n - c);
            else if (c > n) Purse.TrySpend(c - n);
        }
    }
}
#endif
