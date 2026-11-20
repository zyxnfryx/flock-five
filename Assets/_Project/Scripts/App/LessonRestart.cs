namespace FlockFive
{
    // Which lesson a Restart interrupted, and the first step it should show again.
    // Pure so an editor test can walk every stage. The app reads the live flags,
    // never the caption: a Nice lesson can still be drawing the Crowns line.
    public static class LessonRestart
    {
        public enum Stage
        {
            None = 0,
            Garden,
            Crowns,
            Nice,
            Sleep,
            Sparrow,
            Hawk,
            Bee,
            Ostrich,
            Fire,
            Leaf,
            HiveGarden,
            HiveHome,
            Gift,
            BadgerLesson,
            BadgerGuide,
            BadgerPower,
            BreakFree,
            Album,
            Upgrade,
            Daily,
            Adopt,
            PokerIntro,
            PokerTable,
            PokerDeal,
            PokerBack,
            AdHand,
            Vip,
            RestartAsk,
        }

        public struct Marks
        {
            public RuleTip Rule;
            public bool Coach;
            public int Pest;
            public bool Leaf, HiveGarden, HiveHome, Gift;
            public bool BadgerLesson, BadgerGuide, BadgerPower, BreakFree;
            public bool Album, Upgrade, Daily, Adopt;
            public bool PokerIntro, PokerTable, PokerDeal, PokerBack;
            public bool AdHand, Vip;
            public int RestartAsk;
        }

        public static Stage Of(CoachStep step)
        {
            switch (step)
            {
                case CoachStep.CoachPickup:
                case CoachStep.CoachPlace:
                case CoachStep.CoachSecondHop:
                case CoachStep.CoachNowhere:
                    return Stage.Garden;
                case CoachStep.RuleKit: return Stage.Crowns;
                case CoachStep.RuleFeeder: return Stage.Nice;
                case CoachStep.RuleSleep: return Stage.Sleep;
                case CoachStep.Sparrow: return Stage.Sparrow;
                case CoachStep.Hawk: return Stage.Hawk;
                case CoachStep.Bee: return Stage.Bee;
                case CoachStep.Ostrich: return Stage.Ostrich;
                case CoachStep.Fire: return Stage.Fire;
                case CoachStep.Leaf: return Stage.Leaf;
                case CoachStep.HiveGarden: return Stage.HiveGarden;
                case CoachStep.HiveHome: return Stage.HiveHome;
                case CoachStep.GiftStuck: return Stage.Gift;
                case CoachStep.BadgerPick:
                case CoachStep.BadgerPowers:
                case CoachStep.BadgerColumn:
                    return Stage.BadgerLesson;
                case CoachStep.BadgerComb1:
                case CoachStep.BadgerComb2:
                case CoachStep.BadgerComb3:
                    return Stage.BadgerGuide;
                case CoachStep.BadgerPowerIntro:
                case CoachStep.BadgerPowerArmed:
                case CoachStep.BadgerResult:
                    return Stage.BadgerPower;
                case CoachStep.BadgerBreakFree: return Stage.BreakFree;
                case CoachStep.AlbumEmpty:
                case CoachStep.AlbumTap:
                case CoachStep.AlbumFlip:
                case CoachStep.AlbumHoney:
                case CoachStep.AlbumPage:
                    return Stage.Album;
                case CoachStep.AlbumUpgrade: return Stage.Upgrade;
                case CoachStep.DailyRail:
                case CoachStep.DailyClaim:
                    return Stage.Daily;
                case CoachStep.AdoptGreet:
                case CoachStep.AdoptLook:
                    return Stage.Adopt;
                case CoachStep.PokerIntro: return Stage.PokerIntro;
                case CoachStep.PokerBet:
                case CoachStep.PokerHold:
                case CoachStep.PokerKeep:
                    return Stage.PokerTable;
                case CoachStep.PokerDeal: return Stage.PokerDeal;
                case CoachStep.PokerBack: return Stage.PokerBack;
                case CoachStep.AdHand: return Stage.AdHand;
                case CoachStep.VipWelcome:
                case CoachStep.VipPitch:
                    return Stage.Vip;
                case CoachStep.RestartSure:
                case CoachStep.RestartKeep:
                    return Stage.RestartAsk;
            }
            return Stage.None;
        }

        public static bool CoversAll()
        {
            var all = CoachTap.All;
            for (int i = 0; i < all.Length; i++)
                if (Of(all[i]) == Stage.None) return false;
            return all.Length > 0;
        }

        // The lesson the flags say is up. Pest numbers match FlockFiveApp's PestCue* constants.
        // The caption is not an input.
        public static Stage Identify(Marks m)
        {
            if (m.Rule == RuleTip.Kit) return Stage.Crowns;
            if (m.Rule == RuleTip.Feeder) return Stage.Nice;
            if (m.Rule == RuleTip.Sleep) return Stage.Sleep;
            if (m.Pest == 4) return Stage.Ostrich;
            if (m.Pest == 2) return Stage.Hawk;
            if (m.Pest == 3) return Stage.Bee;
            if (m.Pest == 5) return Stage.Fire;
            if (m.Pest != 0) return Stage.Sparrow;
            if (m.Leaf) return Stage.Leaf;
            if (m.HiveGarden) return Stage.HiveGarden;
            if (m.Gift) return Stage.Gift;
            if (m.Coach) return Stage.Garden;
            if (m.BadgerLesson) return Stage.BadgerLesson;
            if (m.BreakFree) return Stage.BreakFree;
            if (m.BadgerPower) return Stage.BadgerPower;
            if (m.BadgerGuide) return Stage.BadgerGuide;
            if (m.Upgrade) return Stage.Upgrade;
            if (m.Album) return Stage.Album;
            if (m.Daily) return Stage.Daily;
            if (m.Adopt) return Stage.Adopt;
            if (m.PokerIntro) return Stage.PokerIntro;
            if (m.PokerDeal) return Stage.PokerDeal;
            if (m.PokerBack) return Stage.PokerBack;
            if (m.PokerTable) return Stage.PokerTable;
            if (m.AdHand) return Stage.AdHand;
            if (m.Vip) return Stage.Vip;
            if (m.HiveHome) return Stage.HiveHome;
            if (m.RestartAsk != 0) return Stage.RestartAsk;
            return Stage.None;
        }

        public static CoachStep Step1(Stage stage)
        {
            switch (stage)
            {
                case Stage.Garden: return CoachStep.CoachPickup;
                case Stage.Crowns: return CoachStep.RuleKit;
                case Stage.Nice: return CoachStep.RuleFeeder;
                case Stage.Sleep: return CoachStep.RuleSleep;
                case Stage.Sparrow: return CoachStep.Sparrow;
                case Stage.Hawk: return CoachStep.Hawk;
                case Stage.Bee: return CoachStep.Bee;
                case Stage.Ostrich: return CoachStep.Ostrich;
                case Stage.Fire: return CoachStep.Fire;
                case Stage.Leaf: return CoachStep.Leaf;
                case Stage.HiveGarden: return CoachStep.HiveGarden;
                case Stage.HiveHome: return CoachStep.HiveHome;
                case Stage.Gift: return CoachStep.GiftStuck;
                case Stage.BadgerLesson: return CoachStep.BadgerPick;
                case Stage.BadgerGuide: return CoachStep.BadgerComb1;
                case Stage.BadgerPower: return CoachStep.BadgerPowerIntro;
                case Stage.BreakFree: return CoachStep.BadgerBreakFree;
                case Stage.Album: return CoachStep.AlbumEmpty;
                case Stage.Upgrade: return CoachStep.AlbumUpgrade;
                case Stage.Daily: return CoachStep.DailyRail;
                case Stage.Adopt: return CoachStep.AdoptGreet;
                case Stage.PokerIntro: return CoachStep.PokerIntro;
                case Stage.PokerTable: return CoachStep.PokerBet;
                case Stage.PokerDeal: return CoachStep.PokerDeal;
                case Stage.PokerBack: return CoachStep.PokerBack;
                case Stage.AdHand: return CoachStep.AdHand;
                case Stage.Vip: return CoachStep.VipWelcome;
                case Stage.RestartAsk: return CoachStep.RestartSure;
            }
            return CoachStep.CoachPickup;
        }

        public static CoachStep Step1(CoachStep step) => Step1(Of(step));

        public static string Line(CoachStep step) => CoachTap.Caption(Step1(step));

        // Rule captions have no hand. An arrow step points without the glove.
        // Every other step 1 poses the pointing-finger glove.
        public static bool ArmsGlove(CoachStep step)
        {
            var first = Step1(step);
            if (first == CoachStep.RuleKit || first == CoachStep.RuleFeeder || first == CoachStep.RuleSleep)
                return false;
            return CoachTap.Pointer(first) == CoachPointer.Glove;
        }

        public static bool Foreign(CoachStep live, string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            var mine = Of(live);
            if (mine != Stage.None && line == CoachTap.Caption(Step1(mine))) return false;
            var all = CoachTap.All;
            for (int i = 0; i < all.Length; i++)
            {
                if (Of(all[i]) == mine) continue;
                if (line == CoachTap.Caption(all[i])) return true;
            }
            return false;
        }
    }
}
