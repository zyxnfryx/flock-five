using System.Collections.Generic;

namespace FlockFive
{
    // Timing and bed phase for the blackbird visit. Pure data: the view and
    // MixDesk play it, the edit-mode suite checks the numbers.
    public static class BlackbirdCue
    {
        public const float IntroCutSec = 2.5f;
        public const float DropSec = 0.55f;
        public const float TauntSec = 1f;
        public const float TellSec = 0.5f;
        public const float SilenceSec = 1.1f;
        // Same D-major porch tune as the calm bed, shorter beat. Not a second melody.
        public const float CalmBpm = 84f;
        public const float RushBpm = 116f;
        public const int RattleAt = 3;
        public const int FleeAt = 4;
        public const int CalmEvery = 3;
        public const int RattleEvery = 2;

        public enum Bed { Calm, Cut, Rush, Silence }

        public static int Every(int flocksDone) => flocksDone >= RattleAt ? RattleEvery : CalmEvery;

        // Intro holds the cut. After he bolts, silence, then the caller returns to calm.
        // Three flocks done and he is still here: the faster loop. Otherwise the porch tune.
        public static Bed BedFor(bool intro, bool fled, int flocksDone)
        {
            if (intro) return Bed.Cut;
            if (fled) return Bed.Silence;
            if (flocksDone >= RattleAt) return Bed.Rush;
            return Bed.Calm;
        }
    }

    // First visit is garden 44. Later visits sit every 13 gardens and each one
    // is a notch smarter. Returns stay behind Returns so only 44 is required.
    public static class BlackbirdSchedule
    {
        public const int FirstGarden = 44;
        public const int Every = 13;
        public static readonly int[] ReturnGardens = { 57, 70, 83, 96 };

        public static bool Returns;

        public static bool Due(int garden)
        {
            if (garden == FirstGarden) return true;
            if (!Returns || garden < FirstGarden) return false;
            return (garden - FirstGarden) % Every == 0;
        }

        public static int Notch(int garden) =>
            Due(garden) ? (garden - FirstGarden) / Every : 0;
    }

    public sealed class BlackbirdState
    {
        public int Notch;
        public int PlayerMoves;
        public int Spare = -1;
        public int Perch = -1;
        public int ColorMask;
        public bool Fled;
        public bool LastDitchPlayed;

        public BlackbirdState Clone() => (BlackbirdState)MemberwiseClone();
    }

    public struct BlackbirdChoice
    {
        public bool WillMove;
        public int From;
        public int To;
        public int Run;
        // "sorted", "last", or "pass".
        public string Why;
    }

    public struct BlackbirdAnswer
    {
        public bool Moved;
        public bool FledNow;
        public int From;
        public int To;
        public int Run;
        public string Why;
    }

    public static class BlackbirdRival
    {
        public static int FlocksDone(Board b)
        {
            if (b == null || b.Blackbird == null) return 0;
            int mask = b.Blackbird.ColorMask;
            int n = 0;
            for (int c = 0; c < Palette.Max; c++)
            {
                if ((mask & (1 << c)) == 0) continue;
                if (CountColor(b, (BirdColor)c) == 0) n++;
            }
            return n;
        }

        public static int CountColor(Board b, BirdColor color)
        {
            if (b == null) return 0;
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                    if (br.Birds[k].Color == color) n++;
            }
            for (int i = 0; i < b.Displaced.Count; i++)
                if (b.Displaced[i].Color == color) n++;
            return n;
        }

        public static bool HandsOff(Board b, int i)
        {
            if (b == null || (uint)i >= (uint)b.Branches.Count) return true;
            var br = b.Branches[i];
            if (br == null || br.Broken || br.AdLocked || br.IsSpare || br.RivalPerch) return true;
            if (br.TipLocked) return true;
            if (br.IsFullMatch(out _)) return true;
            return false;
        }

        // True when the move that is about to be counted should make him act.
        public static bool Owes(BlackbirdState bb, int flocksDone, int movesAfter)
        {
            if (bb == null || bb.Fled || movesAfter < 1) return false;
            if (flocksDone >= BlackbirdCue.FleeAt) return !bb.LastDitchPlayed;
            return movesAfter % BlackbirdCue.Every(flocksDone) == 0;
        }

        public static BlackbirdChoice Choose(Board b) => Choose(b, b != null && b.Blackbird != null ? b.Blackbird.Notch : 0);

        // Deterministic. Most-sorted unfinished source first. Notch 0 tries only
        // that branch; each later visit tries one more ranked source before the
        // last-moved bird. Destination is the lowest legal index he may touch.
        public static BlackbirdChoice Choose(Board b, int notch)
        {
            var pass = new BlackbirdChoice { From = -1, To = -1, Why = "pass" };
            if (b == null || b.Blackbird == null || b.Blackbird.Fled) return pass;
            if (notch < 0) notch = 0;
            var ranked = Rank(b, notch);
            int tries = 1 + notch;
            if (tries > ranked.Count) tries = ranked.Count;
            for (int i = 0; i < tries; i++)
            {
                if (TryDest(b, ranked[i], out int to, out int run))
                    return new BlackbirdChoice { WillMove = true, From = ranked[i], To = to, Run = run, Why = "sorted" };
            }
            int last = b.LastHopTo;
            if (!HandsOff(b, last) && b.Branches[last].Count > 0 && TryDest(b, last, out int lto, out int lrun))
                return new BlackbirdChoice { WillMove = true, From = last, To = lto, Run = lrun, Why = "last" };
            return pass;
        }

        // One player hop has already landed and its collects have drained.
        // Counts the move, maybe acts, then flees once four flocks are done.
        public static BlackbirdAnswer Commit(Board b)
        {
            var answer = new BlackbirdAnswer { From = -1, To = -1, Why = "pass" };
            if (b == null || b.Blackbird == null || b.Blackbird.Fled) return answer;
            var bb = b.Blackbird;
            int playerFrom = b.LastHopFrom;
            int playerTo = b.LastHopTo;
            bb.PlayerMoves++;
            int flocks = FlocksDone(b);
            bool ditch = flocks >= BlackbirdCue.FleeAt && !bb.LastDitchPlayed;
            if (Owes(bb, flocks, bb.PlayerMoves))
            {
                var choice = Choose(b, bb.Notch);
                answer.Why = choice.Why;
                answer.From = choice.From;
                answer.To = choice.To;
                answer.Run = choice.Run;
                if (choice.WillMove && b.TryMove(choice.From, choice.To, out int run))
                {
                    answer.Moved = true;
                    answer.Run = run;
                    GardenSolve.Drain(b);
                }
            }
            // His hop is not the player's last move.
            b.LastHopFrom = playerFrom;
            b.LastHopTo = playerTo;
            if (ditch) bb.LastDitchPlayed = true;
            flocks = FlocksDone(b);
            if (flocks >= BlackbirdCue.FleeAt)
            {
                bb.LastDitchPlayed = true;
                if (!bb.Fled)
                {
                    bb.Fled = true;
                    answer.FledNow = true;
                }
            }
            return answer;
        }

        public static void Dress(Board b, int notch)
        {
            if (b == null || b.Blackbird != null) return;
            int spare = b.Branches.Count;
            b.Branches.Add(new BranchState { IsSpare = true });
            int perch = b.Branches.Count;
            b.Branches.Add(new BranchState { RivalPerch = true, SeatCap = 1 });
            int mask = 0;
            for (int c = 0; c < Palette.Max; c++)
                if (CountColor(b, (BirdColor)c) > 0) mask |= 1 << c;
            b.Blackbird = new BlackbirdState
            {
                Notch = notch < 0 ? 0 : notch,
                Spare = spare,
                Perch = perch,
                ColorMask = mask
            };
        }

        public struct Point
        {
            public float X;
            public float Y;
        }

        // Intro drop, rattled edge swoop, and the bolt. Visual only — no board change.
        public static class Flight
        {
            public static Point Drop(Point perch, float u)
            {
                u = Clamp01(u);
                float e = u * u;
                return new Point { X = perch.X, Y = Lerp(perch.Y + 3.2f, perch.Y, e) };
            }

            // edge 0 left, 1 right, 2 top. u = 0 is off-screen, u = 1 is the perch.
            public static Point Swoop(Point perch, int edge, float u, float halfW, float halfH)
            {
                u = Clamp01(u);
                float e = u * u * (3f - 2f * u);
                float x = perch.X;
                float y = perch.Y + 1.4f;
                if (edge == 1) x = perch.X + halfW + 1.2f;
                else if (edge == 2) { x = perch.X; y = perch.Y + halfH + 1.2f; }
                else x = perch.X - halfW - 1.2f;
                float arc = 4f * u * (1f - u) * 0.8f;
                return new Point { X = Lerp(x, perch.X, e), Y = Lerp(y, perch.Y, e) + arc };
            }

            public static Point Bolt(Point perch, float u, float halfW)
            {
                u = Clamp01(u);
                float e = u * u;
                return new Point
                {
                    X = Lerp(perch.X, perch.X + halfW + 2f, e),
                    Y = Lerp(perch.Y, perch.Y + 2.4f, e)
                };
            }

            static float Clamp01(float u) => u < 0f ? 0f : (u > 1f ? 1f : u);
            static float Lerp(float a, float b, float t) => a + (b - a) * t;
        }

        static bool TryDest(Board b, int from, out int to, out int run)
        {
            to = -1;
            run = 0;
            if (HandsOff(b, from) || b.Branches[from].Count == 0) return false;
            int n = b.Branches.Count;
            for (int i = 0; i < n; i++)
            {
                if (i == from || HandsOff(b, i)) continue;
                // SameFlock only. An empty limb, including the spare, is not a pull.
                if (b.Branches[i].Empty) continue;
                if (!b.CanMove(from, i, out int r) || r <= 0) continue;
                to = i;
                run = r;
                return true;
            }
            return false;
        }

        // Higher sort score first. Equal scores keep the earlier limb.
        static List<int> Rank(Board b, int notch)
        {
            var src = new List<int>();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (HandsOff(b, i) || b.Branches[i].Count == 0) continue;
                src.Add(i);
            }
            src.Sort((a, c) =>
            {
                int s = SortScore(b.Branches[c], notch).CompareTo(SortScore(b.Branches[a], notch));
                if (s != 0) return s;
                return a.CompareTo(c);
            });
            return src;
        }

        // Longest same-flock run, then more birds. A higher notch prefers the
        // fuller of two equal runs so he pulls the stack closest to finishing.
        public static int SortScore(BranchState br, int notch)
        {
            if (br == null || br.Count == 0) return 0;
            int best = 0;
            int run = 0;
            Bird prev = default;
            bool any = false;
            for (int i = 0; i < br.Count; i++)
            {
                if (br.IsShrouded(i)) { run = 0; any = false; continue; }
                if (!any || !br.Birds[i].SameFlock(prev))
                {
                    run = 1;
                    prev = br.Birds[i];
                    any = true;
                }
                else run++;
                if (run > best) best = run;
            }
            int score = best * 1000 + br.Count * 10;
            if (notch > 0) score += notch * br.Count;
            return score;
        }
    }
}
