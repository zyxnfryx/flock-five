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

        // Play Mode only: opens the contest screen at the given cleared level (the screen holds
        // the kill switch on until you leave it). The real flow never reaches it yet.
        [MenuItem("Flock Five/Open Badger Fight Screen (Play Mode)")]
        public static void OpenScreen()
        {
            File.WriteAllText("/tmp/flock-five-badger-fight", "15");
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
                CheckBossLeads(Check);
                CheckTie(Check);
                CheckBlock(Check);
                CheckFreeze(Check);
                CheckMultiplier(Check);
                CheckEmpty(Check);
                CheckExhaust(Check);
                CheckTargetWin(Check);
                CheckTieFight(Check);
                CheckPurse(Check);
                CheckSeed(Check);
                CheckHoneyLabel(Check);
                CheckSaveFlag(Check);
                CheckHook(Check);
                CheckHoneyShared(Check);
                CheckLoadoutPreload(Check);
                CheckLoadoutSwap(Check);
                CheckPowerPlay(Check);
                CheckScriptedFight(Check);
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
                && BadgerSchedule.PowerUpPrice(BadgerPower.FreezeSpray, 1) == 600;
            bool step = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 2) == 150
                && BadgerSchedule.PowerUpPrice(BadgerPower.X3, 2) == 250
                && BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 2) == 350
                && BadgerSchedule.PowerUpPrice(BadgerPower.FreezeSpray, 3) == 700;
            bool split = BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 6) == 550
                && BadgerSchedule.PowerUpPrice(BadgerPower.FreezeSpray, 6) == 850;
            int far = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 100);
            int near = BadgerSchedule.PowerUpPrice(BadgerPower.X2, 99);
            bool open = far == 100 + 50 * 99 && far - near == 50
                && BadgerSchedule.PowerUpPrice(BadgerPower.FreezeSpray, 40) == 600 + 50 * 39
                && BadgerSchedule.PowerUpPrice(BadgerPower.None, 5) == 0;
            Check("price", bases && step && split && open, "base + 50 per visit, no cap; freeze 600");
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

        static void CheckHoneyLabel(System.Action<string, bool, string> Check)
        {
            bool labels = Hive.HoneyLabel(BeeFinish.Normal) == "H2"
                && Hive.HoneyLabel(BeeFinish.Holo) == "H3"
                && Hive.HoneyLabel(BeeFinish.InverseRainbow) == "H5";
            bool keep = BadgerSchedule.Enabled;
            try
            {
                BadgerSchedule.Enabled = false;
                bool hidden = Hive.HoneyChips(BeeFinish.Holo) == null;
                BadgerSchedule.Enabled = true;
                var row = Hive.HoneyChips(BeeFinish.Holo);
                bool shape = row != null && row.Length == 4 && row[0] == "H3"
                    && row[1] == "" && row[2] == "" && row[3] == "";
                var inv = Hive.HoneyChips(BeeFinish.InverseRainbow);
                bool inverse = inv != null && inv[0] == "H5";
                Check("honey label", labels && hidden && shape && inverse,
                    "H2/H3/H5; chip row hidden when off, 1 label + 3 empty when on");
            }
            finally { BadgerSchedule.Enabled = keep; }
        }

        static void CheckSaveFlag(System.Action<string, bool, string> Check)
        {
            bool keepOn = BadgerSchedule.Enabled;
            int keepFlag = BadgerSave.PendingFor(true);
            try
            {
                BadgerSave.Clear();
                bool off = !BadgerSave.Set(15, false) && BadgerSave.PendingFor(true) == 0;
                bool on = BadgerSave.Set(15, true) && BadgerSave.PendingFor(true) == 15;
                BadgerSchedule.Enabled = false;
                bool gated = BadgerSave.Pending == 0 && BadgerSave.PendingFor(false) == 0;
                BadgerSchedule.Enabled = true;
                bool read = BadgerSave.Pending == 15;
                BadgerSave.Set(20);
                bool again = BadgerSave.Pending == 20;
                BadgerSave.Clear();
                bool clear = BadgerSave.Pending == 0 && BadgerSave.PendingFor(true) == 0;
                bool bad = !BadgerSave.Set(0, true) && !BadgerSave.Set(-5, true);
                Check("save flag", off && on && gated && read && again && clear && bad,
                    "Set/Clear/Pending round trip; Pending 0 when switch off; zero and negative refused");
            }
            finally
            {
                BadgerSchedule.Enabled = keepOn;
                if (keepFlag > 0) BadgerSave.Set(keepFlag, true);
                else BadgerSave.Clear();
            }
        }

        static void CheckHook(System.Action<string, bool, string> Check)
        {
            bool keepOn = BadgerSchedule.Enabled;
            int keepFlag = BadgerSave.PendingFor(true);
            try
            {
                bool due = BadgerSchedule.Due(15) && !BadgerSchedule.Due(16) && BadgerSchedule.Due(20);
                bool rule = BadgerSchedule.ShouldFlag(15, true) && !BadgerSchedule.ShouldFlag(16, true)
                    && BadgerSchedule.ShouldFlag(20, true) && !BadgerSchedule.ShouldFlag(15, false)
                    && !BadgerSchedule.ShouldFlag(20, false);
                BadgerSave.Clear();
                bool offNoWrite = !BadgerSave.SetIfDue(15, false) && BadgerSave.PendingFor(true) == 0;
                bool notDue = !BadgerSave.SetIfDue(16, true) && BadgerSave.PendingFor(true) == 0;
                bool wrote = BadgerSave.SetIfDue(20, true) && BadgerSave.PendingFor(true) == 20;
                BadgerSave.Clear();
                BadgerSchedule.Enabled = false;
                bool realOff = !BadgerSave.SetIfDue(15) && BadgerSave.PendingFor(true) == 0;
                Check("hook", due && rule && offNoWrite && notDue && wrote && realOff,
                    "Due 15/20 yes, 16 no; switch off writes nothing; due writes the cleared level");
            }
            finally
            {
                BadgerSchedule.Enabled = keepOn;
                if (keepFlag > 0) BadgerSave.Set(keepFlag, true);
                else BadgerSave.Clear();
            }
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


        // Boss always leads: PlayerPick / PlayerPlay before BossPick change nothing.
        static void CheckBossLeads(System.Action<string, bool, string> Check)
        {
            var fight = new BadgerFight(1, 9, Fill(16, 4), Fill(16, 2));
            bool blocked = !fight.PlayerPick(0) && !fight.PlayerPlay(0, BadgerPower.None)
                && fight.PlayerOpen(0) && fight.PlayerLeft == 16 && fight.BossLeft == 16
                && fight.Result == BadgerResult.Playing;
            int boss = fight.BossPick();
            bool after = boss >= 0 && fight.PlayerPick(0) && !fight.BossOpen(boss) && !fight.PlayerOpen(0);
            Check("boss leads", blocked && after, "player cannot pick first; boss pick unlocks the player");
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
            Check("compare", math && scored, "higher honey scores that honey; early player pick blocked");
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
            int freezeFinal;
            int freezeBoss;
            int freezeGain;
            int freezeBossGain;
            BadgerFight.ApplyRound(2, 5, BadgerPower.HotSauce, out sauceFinal, out sauceBoss, out sauceGain, out sauceBossGain);
            BadgerFight.ApplyRound(2, 5, BadgerPower.FreezeSpray, out freezeFinal, out freezeBoss, out freezeGain, out freezeBossGain);
            bool math = sauceFinal == 2 && sauceBoss == 0 && sauceGain == 2 && sauceBossGain == 0
                && freezeFinal == 2 && freezeBoss == 0 && freezeGain == 2 && freezeBossGain == 0
                && BadgerFight.SkipTurnsOf(BadgerPower.HotSauce) == 1
                && BadgerFight.SkipTurnsOf(BadgerPower.FreezeSpray) == 2
                && BadgerFight.IsBlock(BadgerPower.HotSauce) && BadgerFight.IsBlock(BadgerPower.FreezeSpray)
                && !BadgerFight.IsBlock(BadgerPower.None);
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
                && sauce.PlayerScore == 2 && sauce.BossScore == 0 && sauce.BossSkipLeft == 0
                && !sauce.BossOpen(bossIx) && !sauce.PlayerOpen(0)
                && !sauce.PowerReady(BadgerPower.HotSauce);
            bool second = sauce.BossPick() >= 0 && sauce.PlayerPick(1)
                && !sauce.TryPower(BadgerPower.HotSauce)
                && Purse.Coins == before - price;
            BadgerRound afterSauce;
            bool sauceNext = sauce.Resolve(out afterSauce) && afterSauce.BossFinal == 5
                && afterSauce.BossGained == 5 && sauce.BossScore == 5;

            Check("block-sauce", math && unblocked && effect && second && sauceNext,
                "sauce knocks badger honey to 0 for one turn; one use");
        }

        static void CheckFreeze(System.Action<string, bool, string> Check)
        {
            SetCoins(5000);
            int before = Purse.Coins;
            int price = BadgerSchedule.PowerUpPrice(BadgerPower.FreezeSpray, 1);
            bool cost = price == 600;
            var fight = new BadgerFight(1, 22, Fill(16, 2), Fill(16, 5));
            BadgerRound r1;
            bool first = Play(fight, 2, BadgerPower.FreezeSpray, out r1)
                && r1.BossFinal == 0 && r1.BossGained == 0 && r1.PlayerGained == 2
                && fight.BossScore == 0 && fight.BossSkipLeft == 1
                && !fight.PowerReady(BadgerPower.FreezeSpray)
                && Purse.Coins == before - price;
            BadgerRound r2;
            bool held = first && Play(fight, 2, BadgerPower.None, out r2)
                && r2.Power == BadgerPower.None
                && r2.BossFinal == 0 && r2.BossGained == 0 && r2.PlayerGained == 2
                && fight.BossScore == 0 && fight.BossSkipLeft == 0;
            BadgerRound r3;
            bool thawed = held && Play(fight, 2, BadgerPower.None, out r3)
                && r3.BossFinal == 5 && r3.BossGained == 5
                && fight.BossScore == 5 && fight.BossSkipLeft == 0;
            // Hot Sauce still one turn only (unchanged).
            var sauce = new BadgerFight(1, 23, Fill(16, 2), Fill(16, 5));
            SetCoins(5000);
            BadgerRound s1, s2;
            bool sauceOk = Play(sauce, 2, BadgerPower.HotSauce, out s1)
                && s1.BossFinal == 0 && sauce.BossSkipLeft == 0
                && Play(sauce, 2, BadgerPower.None, out s2)
                && s2.BossFinal == 5 && s2.BossGained == 5;
            Check("freeze-spray", cost && first && held && thawed && sauceOk,
                "freeze skips exactly two boss turns at 600; sauce still one");
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

        // A small fake album: kind 0 has two regular, kind 1 one foil, kind 2 one Inverse Rainbow,
        // kind 3 one regular. Five bees, so a 16-tile grid pads eleven yard tiles.
        static int FakeAlbum(int kind, BeeFinish finish)
        {
            if (kind == 0 && finish == BeeFinish.Normal) return 2;
            if (kind == 1 && finish == BeeFinish.Holo) return 1;
            if (kind == 2 && finish == BeeFinish.InverseRainbow) return 1;
            if (kind == 3 && finish == BeeFinish.Normal) return 1;
            return 0;
        }

        static void CheckHoneyShared(System.Action<string, bool, string> Check)
        {
            bool table = Hive.HoneyOfFinish(BeeFinish.Normal) == 2
                && Hive.HoneyOfFinish(BeeFinish.Holo) == 3
                && Hive.HoneyOfFinish(BeeFinish.InverseRainbow) == 5;
            bool tiles = BadgerTile.Bee(0, BeeFinish.Normal).Honey == Hive.HoneyOfFinish(BeeFinish.Normal)
                && BadgerTile.Bee(0, BeeFinish.Holo).Honey == Hive.HoneyOfFinish(BeeFinish.Holo)
                && BadgerTile.Bee(0, BeeFinish.InverseRainbow).Honey == Hive.HoneyOfFinish(BeeFinish.InverseRainbow)
                && BadgerTile.YardTile().Honey == 1 && BadgerTile.YardTile().Yard;
            bool label = Hive.HoneyLabel(BeeFinish.Holo) == "H" + Hive.HoneyOfFinish(BeeFinish.Holo);
            Check("honey shared", table && tiles && label, "tiles and chip label both read Hive.HoneyOfFinish (2/3/5, yard 1)");
        }

        static void CheckLoadoutPreload(System.Action<string, bool, string> Check)
        {
            var load = BadgerLoadout.Preload(FakeAlbum, 6, BadgerSchedule.Tiles);
            var honey = load.Honeys();
            bool best = honey.Length == 16 && honey[0] == 5 && honey[1] == 3
                && honey[2] == 2 && honey[3] == 2 && honey[4] == 2;
            int yard = 0;
            for (int i = 5; i < honey.Length; i++) if (honey[i] == 1 && load[i].Yard) yard++;
            bool pad = yard == 11 && load.BeeTiles == 5;
            var none = BadgerLoadout.Preload((k, f) => 0, 6, 16);
            bool empty = none.BeeTiles == 0 && Sum(none.Honeys()) == 16;
            var few = BadgerLoadout.Preload(FakeAlbum, 6, 3);
            bool cut = few.Length == 3 && few[0].Honey == 5 && few[1].Honey == 3 && few[2].Honey == 2;
            var live = BadgerSchedule.PlayerLoadout();
            bool real = live.Length == 16;
            Check("loadout preload", best && pad && empty && cut && real,
                "best bees first (5,3,2,2,2), padded with eleven yard 1s, empty album all yard, cut to slot count");
        }

        static void CheckLoadoutSwap(System.Action<string, bool, string> Check)
        {
            var load = BadgerLoadout.Preload(FakeAlbum, 6, 16);
            bool noCopy = !load.Set(10, BadgerTile.Bee(0, BeeFinish.Normal));
            bool notOwned = !load.Set(10, BadgerTile.Bee(5, BeeFinish.Holo));
            bool freeUp = load.Free(2, BeeFinish.InverseRainbow) == 0;
            load.Set(0, BadgerTile.YardTile());
            bool freed = load.Free(2, BeeFinish.InverseRainbow) == 1;
            bool moved = load.Set(11, BadgerTile.Bee(2, BeeFinish.InverseRainbow)) && load[11].Honey == 5 && load[0].Yard;
            bool again = !load.Set(12, BadgerTile.Bee(2, BeeFinish.InverseRainbow));
            var list = new System.Collections.Generic.List<BadgerTile>();
            load.Options(12, list);
            bool none = list.Count == 0;
            load.Set(1, BadgerTile.YardTile());
            load.Options(12, list);
            bool one = list.Count == 1 && list[0].Honey == 3;
            load.Options(2, list);
            bool own = list.Count == 2 && list[0].Honey == 3 && list[1].Honey == 2;
            bool opts = none && one && own;
            bool sameOk = load.Set(11, BadgerTile.Bee(2, BeeFinish.InverseRainbow));
            bool range = !load.Set(-1, BadgerTile.YardTile()) && !load.Set(16, BadgerTile.YardTile())
                && load[99].Yard;
            Check("loadout swap", noCopy && notOwned && freeUp && freed && moved && again && opts && sameOk && range,
                "swap needs a free copy; yard always allowed; options skip bees already on the grid");
        }

        static void CheckPowerPlay(System.Action<string, bool, string> Check)
        {
            var fight = new BadgerFight(1, 5, Fill(16, 3), Fill(16, 2));
            fight.BossPick();
            SetCoins(99);
            bool short1 = !fight.PlayerPlay(0, BadgerPower.X2) && fight.PlayerOpen(0)
                && Purse.Coins == 99 && fight.PowerReady(BadgerPower.X2) && !fight.CanAfford(BadgerPower.X2);
            SetCoins(100);
            bool paid = fight.PlayerPlay(0, BadgerPower.X2) && Purse.Coins == 0 && !fight.PowerReady(BadgerPower.X2);
            BadgerRound round;
            bool boosted = fight.Resolve(out round) && round.PlayerFinal == 6 && round.PlayerGained == 6 && round.Power == BadgerPower.X2;
            fight.BossPick();
            SetCoins(500);
            bool once = !fight.PlayerPlay(1, BadgerPower.X2) && Purse.Coins == 500 && fight.PlayerOpen(1);
            bool plain = fight.PlayerPlay(1, BadgerPower.None) && Purse.Coins == 500;
            fight.Resolve(out round);

            var later = new BadgerFight(3, 5, Fill(16, 3), Fill(16, 2));
            bool price = later.PriceOf(BadgerPower.X2) == 200 && later.PriceOf(BadgerPower.X3) == 300
                && later.PriceOf(BadgerPower.HotSauce) == 400 && later.PriceOf(BadgerPower.FreezeSpray) == 700
                && later.PriceOf(BadgerPower.HotSauce) == BadgerSchedule.PowerUpPrice(BadgerPower.HotSauce, 3);

            var sauce = new BadgerFight(1, 5, Fill(16, 1), Fill(16, 5));
            var freeze = new BadgerFight(1, 5, Fill(16, 1), Fill(16, 5));
            SetCoins(300);
            sauce.BossPick();
            bool s1 = sauce.PlayerPlay(0, BadgerPower.HotSauce) && Purse.Coins == 0;
            BadgerRound a;
            bool sr = sauce.Resolve(out a);
            SetCoins(600);
            freeze.BossPick();
            bool f1 = freeze.PlayerPlay(0, BadgerPower.FreezeSpray) && Purse.Coins == 0;
            BadgerRound b;
            bool fr = freeze.Resolve(out b);
            bool block = s1 && sr && f1 && fr && a.BossGained == 0 && b.BossGained == 0
                && a.PlayerGained == 1 && b.PlayerGained == 1 && a.BossFinal == 0 && b.BossFinal == 0
                && sauce.BossSkipLeft == 0 && freeze.BossSkipLeft == 1;
            SetCoins(0);
            var broke = new BadgerFight(1, 5, Fill(16, 4), Fill(16, 2));
            broke.BossPick();
            bool freeFight = broke.PlayerPlay(0, BadgerPower.None) && broke.Resolve(out a) && a.PlayerGained == 4;
            Check("power play", short1 && paid && boosted && once && plain && price && block && freeFight,
                "short purse picks nothing; x2 pays and doubles; one use; price +50 per visit; freeze 600; 0 coins playable");
        }

        static void CheckScriptedFight(System.Action<string, bool, string> Check)
        {
            SetCoins(0);
            var load = BadgerLoadout.Preload(FakeAlbum, 6, 16);
            var win = new BadgerFight(1, 9, load.Honeys(), Fill(16, 1));
            int rounds = 0;
            while (win.Result == BadgerResult.Playing && rounds < 20)
            {
                rounds++;
                int ix = FindPlayer(win, 5);
                if (ix < 0) ix = FindPlayer(win, 3);
                if (ix < 0) ix = FindPlayer(win, 2);
                if (win.BossPick() < 0 || ix < 0 || !win.PlayerPlay(ix, BadgerPower.None)) break;
                BadgerRound r;
                win.Resolve(out r);
            }
            bool won = win.Result == BadgerResult.PlayerWon && win.PlayerScore == 10 && win.BossScore == 0 && rounds == 3;

            var lose = new BadgerFight(1, 9, Fill(16, 1), Fill(16, 5));
            rounds = 0;
            while (lose.Result == BadgerResult.Playing && rounds < 20)
            {
                rounds++;
                if (lose.BossPick() < 0 || !lose.PlayerPlay(FindPlayer(lose, 1), BadgerPower.None)) break;
                BadgerRound r;
                lose.Resolve(out r);
            }
            bool lost = lose.Result == BadgerResult.BadgerWon && lose.BossScore == 20 && lose.PlayerScore == 0 && rounds == 4;

            var out1 = new BadgerFight(1, 9, Fill(2, 3), Fill(2, 3));
            rounds = 0;
            while (out1.Result == BadgerResult.Playing && rounds < 20)
            {
                rounds++;
                if (out1.BossPick() < 0 || !out1.PlayerPlay(FindPlayer(out1, 3), BadgerPower.None)) break;
                BadgerRound r;
                out1.Resolve(out r);
            }
            bool exhausted = out1.Result == BadgerResult.BadgerWon && rounds == 2 && out1.PlayerLeft == 0 && out1.BossLeft == 0;
            Check("scripted fight", won && lost && exhausted,
                "loadout 5+3+2 beats an all-1 grid in three rounds; all-1 loses to all-5 in four; dead heat on empty goes to the badger");
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
