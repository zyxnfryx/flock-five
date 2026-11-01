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
        // Surrender beat. Painted through the shared caption (CaptionLayout), not a new text box.
        public const string GiveUp = "I give!";
        // The one new line a NewTaunt visit adds. Other visits keep the old lines.
        public const string TwistTaunt = "Still here.\nStill don't care.";

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
                case BadgerPower.HotSauce: return "Hot Sauce replaces your weakest tile (it\nscores 0) and blocks the badger. Tap it.";
                case BadgerPower.FreezeSpray: return "Freeze replaces your weakest tile (it scores 0)\nand blocks the badger for 2 rounds. Tap it.";
            }
            return "";
        }

        // Result beat for a block power's forced round: the badger did not score.
        public static string BlockWorkedLine(BadgerPower p)
        {
            if (p == BadgerPower.FreezeSpray) return "Frozen! The badger scores nothing this round,\nthen spends the next one breaking free.";
            return "Blocked! The badger scores nothing.\nThe Hot Sauce tile scores 0.";
        }

        public static string PowerArmedLine(BadgerPower p)
        {
            switch (p)
            {
                case BadgerPower.X2: return X2Armed;
                case BadgerPower.X3: return "x3 triples your next tile.\nTap the glowing comb.";
                case BadgerPower.HotSauce: return "";
                case BadgerPower.FreezeSpray: return ""; // no armed step: the item assigns itself on the tap
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

        // Appearance whose twist is a new taunt speaks TwistTaunt as the opening.
        // Everyone else keeps OpeningLine. The lesson still owns appearance 1.
        public static string OpeningForVisit(int appearance, bool lessonPending)
        {
            if (lessonPending && appearance <= 1) return "";
            if (BadgerPolish.HasTwist(appearance, BadgerTwist.NewTaunt) && appearance > 1)
                return TwistTaunt;
            return OpeningLine(appearance, lessonPending);
        }

        // Rattled and desperate cut the known taunts down. Smug and surrender keep the line.
        // Instruction lines that are not in the map pass through unchanged.
        public static string Spoken(string line, BadgerMood mood)
        {
            if (mood != BadgerMood.Rattled && mood != BadgerMood.Desperate) return line ?? "";
            return ShortTaunt(line);
        }

        public static string ShortTaunt(string line)
        {
            if (line == GoesFirst) return "I go first.";
            if (line == HowMany) return "How many?";
            if (line == MineNow) return "Mine.";
            if (line == DontCare) return "Don't care.";
            if (line == Tie) return "Nothing.";
            if (line == OpenLater) return "Hive's mine.";
            if (line == TwistTaunt) return "Still here.";
            if (line == DontCareWasted) return "Don't care.";
            if (line == DontCareHalved) return "Half honey.";
            return line ?? "";
        }

        // Round banner. A NewTaunt visit swaps one round (index 3, 7, ...) for the new line.
        public static string RoundLine(int roundIndex, BadgerMood mood, bool newTaunt)
        {
            string line = RoundStartLine(roundIndex);
            if (newTaunt && (roundIndex % 4) == 3) line = TwistTaunt;
            return Spoken(line, mood);
        }

        public const string BreakFree = "The badger spent his turn\nbreaking free!";
        public const string FrozenSolid = "Frozen solid! He can't move.";
        // Hot Sauce is disabled while the badger is frozen (frozen turn and break-free turn).
        // Two authored lines (same words) so the shared caption fit never splits at "Hot / Sauce".
        public const string HotSauceFrozen = "Frozen badger.\nHot Sauce is on hold.";
        // Same words on one row, for a caption that already carries its own first line.
        const string HotSauceFrozenFlat = "Frozen badger. Hot Sauce is on hold.";
        public const string HotSauceFrozenSub = "FROZEN";
        // b110 NEW COPY (awaits Brandon): the same line for Freeze Spray (the Hot Sauce line names Hot Sauce).
        public const string FreezeSprayFrozen = "Frozen badger.\nFreeze Spray is on hold.";
        // Coach fight: the round after the Freeze demo is a gated step (CoachStep.BadgerBreakFree): two lines, glove on a comb.
        public const string FreezeBreakFreeWait = "Still frozen. Tap the glowing comb.\n" + HotSauceFrozenFlat;
        public const string BlockedBoth = "Blocked! The badger scores nothing.";

        public static string VerdictLine(BadgerRound r)
        {
            if (r.DontCare && r.Destroyed != BadgerPower.None) return DontCareWasted;
            if (r.DontCare && r.Halved) return DontCareHalved;
            if (r.FreezeBeat == 2) return BreakFree;
            if (r.PlayerGained > 0) return DontCare;
            if (r.BossGained > 0) return MineNow;
            if (r.FreezeBeat == 1) return FrozenSolid;
            if (r.BossHeld) return BlockedBoth;
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

        // Tests only: read and restore the stamp through PrefGuard (it shares the checksum with the coins stamp).
        public static int CoachStampForTest() => PrefGuard.GetInt(CoachPref, 0);

        public static void SetCoachStampForTest(int v)
        {
            PrefGuard.SetInt(CoachPref, v);
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

    // b110: fade the bottom badger caption paints with. The tutorial glove drives the shared fade (tutorial steps keep it);
    // every other fight line (repeat fights, appearance 2+) paints at full strength through the same caption helper.
    public static class BadgerCaptionFade
    {
        public static float For(bool coachDrivesFade, float coachFade)
            => coachDrivesFade ? Mathf.Clamp01(coachFade) : 1f;

        // Pure form of "is this a tutorial fight": any one tutorial signal keeps the glove-driven fade.
        public static bool IsTutorial(bool lessonLive, bool coachFightFlag, bool fightIsCoach, bool lessonStage, bool guideLive, bool resultLive)
            => lessonLive || coachFightFlag || fightIsCoach || lessonStage || guideLive || resultLive;
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

    // b123: the block power's visible beat. The icon flies from its button to the player's weakest tile and replaces the
    // number there; the reveal holds long enough to watch it. Pure timing so tests can check it.
    public static class BadgerPowerFly
    {
        public const float Start = 0.15f;
        public const float Seconds = 0.90f;
        public const float SwapFrom = 0.70f; // progress after which the icon has replaced the number

        public static float Progress(float t)
        {
            float u = (t - Start) / Seconds;
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        public static float RevealExtra(BadgerPower p) => BadgerFight.IsBlock(p) ? Start + Seconds : 0f;
    }
}
