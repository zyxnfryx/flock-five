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
        // The column step breaks on purpose: line 1, then "Honey badger don't care."
        // A width wrap that puts "Honey" on line 1 is wrong.
        // b123: lessons 1-3 are look-at steps. Each ends "Tap to continue." so the one press-to-continue is named.
        public const string LessonColumnTop = "Higher honey scores the difference.";
        public const string LessonColumnBot = "First full column wins. Tap to continue.";
        public static readonly string[] Lesson =
        {
            "Pick one honeycomb. Higher honey wins.\nTap to continue.",
            "x2 and x3 boost a tile. Locked for 3 moves.\nTap to continue.",
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
            "Watch out: he can halve\nyour honey. Tap this one.",
        };
        // Round start right after move 3 (power-ups unlock; tutorial free first use applies).
        public const string PowersOpen = "Power-ups are open. Tap x2.\nYour first one is free.";
        public const string X2Armed = "x2 doubles your next tile.\nTap the glowing comb.";
        // Power-up button sub-label while the coach script holds them.
        public const string Locked = "LOCKED";
        public const string Armed = "ARMED";

        // b123: one forced step per power, on its free first use. x2 keeps the round-4 unlock line.
        public static string PowerIntroLine(BadgerPower p, bool firstUnlock)
        {
            switch (p)
            {
                case BadgerPower.X2: return firstUnlock ? PowersOpen : "Tap x2.\nYour first one is free.";
                case BadgerPower.X3: return "New: x3 triples your next tile.\nTap x3. Free this once.";
                case BadgerPower.HotSauce: return "New: Hot Sauce. The badger\nscores nothing. Tap it. Free once.";
                case BadgerPower.FreezeSpray: return "New: Freeze Spray holds him\nfor 2 rounds. Tap it. Free once.";
            }
            return "";
        }

        public static string PowerArmedLine(BadgerPower p)
        {
            switch (p)
            {
                case BadgerPower.X2: return X2Armed;
                case BadgerPower.X3: return "x3 triples your next tile.\nTap the glowing comb.";
                case BadgerPower.HotSauce: return "Hot Sauce burns his tile.\nTap the glowing comb.";
                case BadgerPower.FreezeSpray: return "Freeze Spray stops him for 2 rounds.\nTap the glowing comb.";
            }
            return "";
        }

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
