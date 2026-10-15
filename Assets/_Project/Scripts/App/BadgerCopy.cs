using UnityEngine;

namespace FlockFive
{
    // Phase 5: original lines, the first-fight lesson, and the win/lose settle plan.
    // Pure (plus PrefGuard for the one-time coach stamp), so editor tests can drive it.
    // Mild and cartoonish only. No Yo! Noid text.
    public static class BadgerCopy
    {
        public const string CoachPref = "flockfive.coach.badger";

        // First-fight lesson. Static captions; the app aims the shared glove at each step.
        // The column step breaks on purpose: line 1, then the halving warning.
        // A width wrap that puts "He" on line 1 is wrong.
        public const string LessonColumnTop = "Fill your column first.";
        public const string LessonColumnBot = "He can halve your honey.";
        public static readonly string[] Lesson =
        {
            "Pick one honeycomb.\nHigher honey wins.",
            "Hot Sauce stops one bite.\nFreeze Spray stops two.",
            LessonColumnTop + "\n" + LessonColumnBot,
        };

        public const string OpenLater = "Hive's mine.\nYour bees took their tiles.";
        public const string GoesFirst = "Honey badger goes first.";
        public const string HowMany = "How many do you want?";
        public const string Tie = "That didn't prove much.";
        public const string DontCare = "Honey badger don't care.";
        public const string MineNow = "Mine now.";
        public const string CoinsShort = "Coins short.";
        public const string Win = "Fine. Take the yard.";
        public const string Lose = "Yard's still mine.";
        public const string YourPick = "Your pick.\nTap a power-up first.";

        // Honey theft beat: boss scored at least this much honey in the round.
        public const int TheftHoney = 4;

        // "Honey Badger Don't Care" slam (spec 2b): one word at a time. Brandon's line for the mode.
        public static readonly string[] DontCareWords = { "Honey", "Badger", "Don't", "Care" };
        // Caption after the slam resolves. Short, mild, original.
        public const string DontCareWasted = "Don't care.\nYour power-up did nothing.";
        public const string DontCareHalved = "Don't care.\nHalf your honey this round.";

        public static int DontCareWordCount => DontCareWords.Length;

        // Coach fight, moves 1-3: one static caption per guided pick (placeholders for Brandon).
        public static readonly string[] Guide =
        {
            "Tap the glowing comb.\nHigher honey wins.",
            "Tap this one.\nThe badger bites back.",
            "Tap this one.\nHe doesn't care.",
        };
        // Round start right after move 3 (power-ups unlock; tutorial free first use applies).
        public const string PowersOpen = "x2, x3, Hot Sauce, Freeze Spray\nare open. Your first is free.";
        // Arm banner (shared caption box) and the never-silent refusals.
        public const string LockedTap = "Power-ups unlock\nafter your first 3 picks.";
        public const string UsedTap = "Already used\nthis fight.";
        public const string ArmedSub = "ON";
        public static string ArmedLine(BadgerPower p)
        {
            string n = p == BadgerPower.X2 ? "x2" : p == BadgerPower.X3 ? "x3" : p == BadgerPower.HotSauce ? "Hot Sauce" : p == BadgerPower.FreezeSpray ? "Freeze Spray" : "";
            if (n.Length == 0) return "";
            return n + " armed -\ntap a comb.";
        }
        // Verdict line for a power-up that landed (below the Don't Care lines).
        public static string PowerLine(BadgerPower p)
        {
            if (p == BadgerPower.X2) return "x2! Your honey\ncounts double.";
            if (p == BadgerPower.X3) return "x3! Your honey\ncounts triple.";
            if (p == BadgerPower.HotSauce) return "Hot Sauce! The badger\nskips this bite.";
            if (p == BadgerPower.FreezeSpray) return "Freeze Spray! The badger\nskips two bites.";
            return "";
        }
        // Power-up button sub-label while the coach script holds them.
        public const string Locked = "LOCKED";

        // move is 1-based; "" outside the guided moves.
        public static string GuideLine(int move)
        {
            if (move < 1 || move > Guide.Length) return "";
            return Guide[move - 1];
        }

        public static string DontCareWordAt(int i)
        {
            if ((uint)i >= (uint)DontCareWords.Length) return "";
            return DontCareWords[i];
        }

        public static int LessonCount => Lesson.Length;

        public static string LessonAt(int step)
        {
            if ((uint)step >= (uint)Lesson.Length) return "";
            return Lesson[step];
        }

        // Appearance 1 with the lesson still pending: the lesson owns the box (no opening line).
        // Later appearances: the short attitude opening.
        public static string OpeningLine(int appearance, bool lessonPending)
        {
            if (appearance <= 1 && lessonPending) return "";
            if (appearance <= 1) return "";
            return OpenLater;
        }

        public static string RoundStartLine(int roundIndex)
        {
            return (roundIndex & 1) == 0 ? GoesFirst : HowMany;
        }

        public static string VerdictLine(BadgerRound r)
        {
            if (r.DontCare && r.Destroyed != BadgerPower.None) return DontCareWasted;
            if (r.DontCare && r.Halved) return DontCareHalved;
            if (r.Power != BadgerPower.None) return PowerLine(r.Power);
            if (r.PlayerGained > 0) return DontCare;
            if (r.BossGained > 0) return MineNow;
            return Tie;
        }

        public static bool ShowShrug(BadgerRound r) => r.PlayerGained > 0;

        public static bool ShowTheftSplat(BadgerRound r)
            => r.BossGained >= TheftHoney;

        public static string EndLine(BadgerResult result)
        {
            if (result == BadgerResult.PlayerWon) return Win;
            return Lose;
        }

        public static bool CoachDone()
        {
            return PrefGuard.GetInt(CoachPref, 0) != 0;
        }

        public static void MarkCoach()
        {
            if (CoachDone()) return;
            PrefGuard.SetInt(CoachPref, 1);
            PlayerPrefs.Save();
        }

        // Clear the stamp in tests only.
        public static void ClearCoachForTest()
        {
            PrefGuard.SetInt(CoachPref, 0);
            PlayerPrefs.Save();
        }

        public static bool NeedsLesson(int appearance)
        {
            return appearance <= 1 && !CoachDone();
        }
    }

    public enum BadgerArmResult { Ignored = 0, Armed = 1, Disarmed = 2, Locked = 3, Used = 4, Short = 5 }

    // One decision for a power button tap, pure so tests can drive it. Never silent:
    // every refusal has a result the view turns into a shake, a sound and a line.
    // sameFrameArm: this power was armed earlier in this very frame; a second press
    // event for the same tap must not toggle it back off.
    public static class BadgerArm
    {
        public static BadgerArmResult Decide(BadgerFight fight, bool pickStage, BadgerPower armed, BadgerPower p, bool sameFrameArm)
        {
            if (!pickStage || fight == null) return BadgerArmResult.Ignored;
            if (armed == p)
                return sameFrameArm ? BadgerArmResult.Ignored : BadgerArmResult.Disarmed;
            if (fight.PowersLocked) return BadgerArmResult.Locked;
            if (!fight.PowerReady(p)) return BadgerArmResult.Used;
            if (!fight.CanAfford(p)) return BadgerArmResult.Short;
            return BadgerArmResult.Armed;
        }
    }

    // What a finished (or quit) contest does to the flag, purse, and hive.
    // Pure plan: the app applies it. Tests check the numbers without Unity UI.
    public struct BadgerPayPlan
    {
        public bool Won;
        public bool ClearFlag;
        public int Credit;
        public bool TakeVisitor;
        public string Line;
        public int FlagDisplay;
    }

    public static class BadgerPay
    {
        // result Playing (quit / kill mid-fight) is a loss: flag stays, no pay.
        public static BadgerPayPlan Plan(BadgerResult result, int flagDisplay)
        {
            var plan = new BadgerPayPlan
            {
                Won = result == BadgerResult.PlayerWon,
                FlagDisplay = flagDisplay > 0 ? flagDisplay : 0,
                Line = BadgerCopy.EndLine(result == BadgerResult.PlayerWon
                    ? BadgerResult.PlayerWon
                    : BadgerResult.BadgerWon),
            };
            if (plan.Won && plan.FlagDisplay > 0)
            {
                plan.ClearFlag = true;
                plan.Credit = Purse.ClearRewardFor(plan.FlagDisplay);
                plan.TakeVisitor = true;
            }
            return plan;
        }

        // Apply the plan. Safe to call once per exit. Returns the coins credited.
        public static int Apply(BadgerPayPlan plan)
        {
            if (!plan.Won)
            {
                // Lose / quit: leave the flag alone. Spent power-up coins stay spent.
                return 0;
            }
            if (plan.ClearFlag) BadgerSave.Clear();
            if (plan.Credit > 0) Purse.Credit(plan.Credit);
            if (plan.TakeVisitor) Hive.TakeBossVisitor();
            return plan.Credit;
        }
    }
}
