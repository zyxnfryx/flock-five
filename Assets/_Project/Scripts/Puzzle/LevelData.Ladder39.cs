namespace FlockFive
{
    // Gardens 39-50 (Build 113). Authored by the garden generator (seed + Pack) and verified with
    // GardenSolve (Winnable, no gift) and TrapProbe. Genseed/metrics in the comment above each maker.
    // Garden 43 is the authored ostrich stage (Garden43Spec).
    public static partial class LevelData
    {
// DRAFT makers for 39, 40, 42 (generator-accepted, NOT wired into LevelData.All; need gardens 41, 43-50 first so the ladder stays contiguous).
// Each: Winnable no gift (solver), trap found by TrapProbe.FindPlayed. Proofs/recorded metrics in plan.md.

// level 39: genseed 15029 solve 5271/5307 sensible tangle 38% proof 7>8 11>13 0>1|4>6|10,10,1,10,6,2,14,3,11,4
        static Board Garden39()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Gold),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[6], 3);
            HideInner(b.Branches[4], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[2]);
            return b;
        }


// level 40: genseed 16008 solve 99/99 sensible tangle 44% proof 2>4 6>2 3>5 12>10 11>9 11>12 10>12 4>2 2>4 4>2 2>4|8>11|7,12,7,11,3,9,12,9,11,6
        static Board Garden40()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[6], 3);
            HideInner(b.Branches[1], 3);
            HideInner(b.Branches[2], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }


// level 42: genseed 17032 solve 124/134 sensible tangle 31% proof 7>10 6>7 1>0 1>7 4>1 10>8 0>3 8>10 6>4 10>8 8>10 10>8 6>2 2>6 2>4 2>6 2>7 7>2 2>7 7>2|11>7|6,14,6,1,11,4,11
        static Board Garden42()
        {
            var b = Eight(
                Row(BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[1], 1);
            HideInner(b.Branches[3], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[5]);
            return b;
        }


        // CAND level=41 genseed=31034 solve=3369/3369 tangle=44 % proof=5>3 1>2 8>9 10>12 4>5 4>1|3>4|9,13,9,13,9,11 t=956s
        static Board Garden41()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[3], 1);
            HideInner(b.Branches[5], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }

        // CAND level=45 genseed=36116 solve=223/223 tangle=44 % proof=2>5 9>12 3>1 4>3 10>8 4>6 10>11 10>13 5>10 1>4 3>5 10>1 1>10 5>3 1>2|8>1|14,7,10,14 t=232s
        static Board Garden45()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Ruby),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[1], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }

        // CAND level=46 genseed=36041 solve=4992/4992 tangle=50 % proof=12>10|5>1|3,9,3,8,8 t=1210s
        static Board Garden46()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Gold),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[2], 1);
            HideInner(b.Branches[5], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[4]);
            return b;
        }

        // CAND level=47 genseed=37052 solve=194/195 tangle=56 % proof=3>6 12>13|12>11|9,11,0,4,8,0,13,5,5,11,6,0 t=1169s
        static Board Garden47()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Teal, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[5], 3);
            HideInner(b.Branches[3], 4);
            HideInner(b.Branches[2], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            return b;
        }

        // CAND level=48 genseed=40118 solve=2018/2018 tangle=44 % proof=0>2 9>7 8>7 6>0 8>13 1>2 5>6 13>8 2>5 12>13 0>1 5>2 7>12 2>5 12>7 1>0|1>4|5,7,10,1,14,4,14 t=567s
        static Board Garden48()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[3]);
            return b;
        }

        // CAND level=49 genseed=39051 solve=410/410 tangle=50 % proof=12>7 10>9 5>0 3>2 3>5 12>10 13>12|6>3|11,3,12,5,8 t=820s
        static Board Garden49()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Gold, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[3], 2);
            HideInner(b.Branches[4], 1);
            HideInner(b.Branches[2], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            return b;
        }

        // CAND level=50 genseed=40054 solve=2675/2676 tangle=56 % proof=7>10 1>0 1>6 1>0|1>6|4,9,4,9,13,7,7,13 t=757s
        static Board Garden50()
        {
            var b = Eight(
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Ruby),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[5], 1);
            HideInner(b.Branches[1], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[4]);
            return b;
        }

        // CAND level=44 genseed=46943 solve=493/493 tangle=25 % proof=13>7 2>1 9>13 13>8 5>13 12>11 7>9 0>6 1>5 2>6 9>7 5>1 4>5 3>0 13>3 2>13 6>4 3>6 8>12 6>5 12>8 13>1 10>13 12>10 10>11 4>2 5>6 0>5 12>13 9>12 1>0 3>5 0>1 7>12 9>8 2>4|9>12|13,9 t=1555s
        static Board Garden44()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Gold),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[2], 2);
            return Prep(b);
        }

    }
}
