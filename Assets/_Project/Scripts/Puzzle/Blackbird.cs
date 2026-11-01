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
        // Stem fade when the porch tune ramps between calm and rush. Not baked into the loop.
        public const float BedCrossfadeSec = 0.4f;
        // Real chook, played as recorded. Level is the shot gain; pitch stays at 1.
        public const float TauntLevel = 0.6f;
        public const float TauntPitchMax = 1.04f;
        public const int RattleAt = 3;
        public const int FleeAt = 4;
        public const int CalmEvery = 3;
        public const int RattleEvery = 2;

        public enum Bed { Calm, Cut, Rush, Silence }

        public static int Every(int flocksDone) => flocksDone >= RattleAt ? RattleEvery : CalmEvery;

        public static int Every(BlackbirdState bb, int flocksDone)
        {
            int calm = bb != null && bb.CalmEvery > 0 ? bb.CalmEvery : CalmEvery;
            int rattle = bb != null && bb.RattleEvery > 0 ? bb.RattleEvery : RattleEvery;
            return flocksDone >= RattleAt ? rattle : calm;
        }

        // Intro holds the cut. After he bolts, silence, then the caller returns to calm.
        // Three flocks done and he is still here: the faster loop. Otherwise the porch tune.
        public static Bed BedFor(bool intro, bool fled, int flocksDone)
        {
            if (intro) return Bed.Cut;
            if (fled) return Bed.Silence;
            if (flocksDone >= RattleAt) return Bed.Rush;
            return Bed.Calm;
        }

        // The croak is lead. A hop already on the lead seat skips it.
        public static bool PlayTaunt(bool leadHot) => !leadHot;

        // Wing tell is skipped while a crunch (lead) is still ringing.
        public static bool HearTell(bool leadHot) => !leadHot;

        public static bool HoldTaps(bool introRunning) => introRunning;

        // An interrupted flee is still on silence. Hand the porch tune back.
        public static Bed BedAfterInterrupt(Bed bed) => bed == Bed.Silence ? Bed.Calm : bed;

        // Where the calm loop is when the rush (or the silence after it) gives it back.
        // elapsed is how long the calm clip was parked, so the tune does not jump to bar 0.
        public static float ResumeCalm(float leftAt, float elapsed, float length)
        {
            if (length <= 0.01f) return 0f;
            if (leftAt < 0f) leftAt = 0f;
            if (elapsed < 0f) elapsed = 0f;
            float t = (leftAt + elapsed) % length;
            if (t < 0f) t += length;
            return t;
        }
    }

    // One value: first garden, then a return every Every gardens after the
    // garden he actually landed on. A landing that is a badger, an ostrich,
    // or a season change slides +1 until it is clear. Smarts stay off until
    // SmartFrom. The trap-free ramp (through RampThrough) uses the gentle cadence.
    public static class BlackbirdSchedule
    {
        public struct Cadence
        {
            public int First;
            public int Every;
        }

        public static Cadence Visit = new Cadence { First = 13, Every = 11 };

        // First visit that is allowed a smarter pull. Earlier visits stay at notch 0.
        public const int SmartFrom = 35;
        // Gardens 1-15 are the trap-free ramp. He answers less often there.
        public const int RampThrough = 15;
        public const int GentleCalm = 4;
        public const int GentleRattle = 3;
        // Last authored ostrich. Past the ladder the same 11-garden spacing continues.
        public const int LastPlacedOstrich = 43;
        public const int UnplacedOstrichEvery = 11;

        public static int CalmEveryFor(int garden) =>
            garden <= RampThrough ? GentleCalm : BlackbirdCue.CalmEvery;

        public static int RattleEveryFor(int garden) =>
            garden <= RampThrough ? GentleRattle : BlackbirdCue.RattleEvery;

        public static int VisitAt(int k)
        {
            if (k < 0) k = 0;
            int at = Slide(Visit.First);
            int every = Gap();
            for (int i = 0; i < k; i++)
                at = Slide(at + every);
            return at;
        }

        public static bool Due(int garden)
        {
            if (garden < Visit.First) return false;
            for (int k = 0; k < 64; k++)
            {
                int at = VisitAt(k);
                if (at == garden) return true;
                if (at > garden) return false;
            }
            return false;
        }

        // 0 through the visit before SmartFrom. The visit on SmartFrom is notch 1.
        public static int Notch(int garden)
        {
            if (!Due(garden) || garden < SmartFrom) return 0;
            int n = 0;
            for (int k = 0; k < 64; k++)
            {
                int at = VisitAt(k);
                if (at > garden) break;
                if (at >= SmartFrom) n++;
            }
            return n;
        }

        // Badger, ostrich, or a backdrop change. Same +1 slide seasons use for an ostrich,
        // also stepping off a badger or a season change. Authored flags win inside the ladder.
        // Past it, ostriches continue every 11 gardens from the last placed one.
        public static bool Blocked(int level)
        {
            if (level < 1) return false;
            if (GardenSeason.IsBadgerLevel(level)) return true;
            if (GardenSeason.IsChangeLevel(level)) return true;
            if (GardenSeason.IsOstrichLevel(level)) return true;
            if (level <= LevelData.Count) return false;
            int every = UnplacedOstrichEvery < 1 ? 1 : UnplacedOstrichEvery;
            return level > LastPlacedOstrich && (level - LastPlacedOstrich) % every == 0;
        }

        static int Slide(int at)
        {
            int guard = 0;
            while (Blocked(at) && guard < 12)
            {
                at++;
                guard++;
            }
            return at;
        }

        static int Gap()
        {
            int every = Visit.Every;
            return every < 1 ? 1 : every;
        }
    }

    public sealed class BlackbirdState
    {
        public int Notch;
        public int CalmEvery;
        public int RattleEvery;
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
            return movesAfter % BlackbirdCue.Every(bb, flocksDone) == 0;
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

        public static void Dress(Board b, int notch, int calmEvery = 0, int rattleEvery = 0)
        {
            if (b == null || b.Blackbird != null) return;
            if (calmEvery < 1) calmEvery = BlackbirdCue.CalmEvery;
            if (rattleEvery < 1) rattleEvery = BlackbirdCue.RattleEvery;
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
                CalmEvery = calmEvery,
                RattleEvery = rattleEvery,
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
