using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // The ostrich's attack, staged. The board has already decided every peck (OstrichPest.AfterMove);
    // this plays them: wind-up, head lunge to the bird, contact pop + squawk, the bird startles and
    // flies in an arc (shared FlockRedistribute flight via PerchOne) to its new perch while the head
    // recoils and settles. When the ostrich dies, the cleared sets take a strike (if no peck did)
    // and fly to their feeder instead of vanishing.
    public partial class FlockFiveApp
    {
        bool _ostrichFight;
        const float StartleSec = 0.14f;
        const float StartleKick = 0.38f;

        IEnumerator OstrichFight()
        {
            int gen = _motionGen;
            var board = _board;
            var mark = _garden.Ostrich;
            if (board == null || board.Ostrich == null) yield break;
            int nb = board.Branches.Count;

            // Views are in sync with the board here (the move just landed). Snapshot them.
            var stacks = new List<SpriteRenderer>[nb];
            for (int i = 0; i < nb; i++)
            {
                stacks[i] = new List<SpriteRenderer>(BranchState.Cap);
                var v = i < _garden.Branches.Length ? _garden.Branches[i] : null;
                if (v == null) continue;
                for (int s = 0; s < BranchState.Cap; s++)
                    if (v.Birds[s] != null && v.Birds[s].enabled) stacks[i].Add(v.Birds[s]);
            }
            var liveBefore = board.Live != null ? (BirdColor?[])board.Live.Clone() : null;
            var fullBefore = new BirdColor?[nb];
            for (int i = 0; i < nb; i++)
                if (board.Branches[i].IsFullMatch(out var c)) fullBefore[i] = c;

            var log = new List<PeckHit>();
            ulong bonkedBefore = board.Ostrich.Bonked;
            int hitsBefore = board.Ostrich.Hits;
            GardenSolve.AfterMove(board, log, out int finale);
            // Sets that landed a hit on this move, in limb order. Each one plays its own hit below.
            var hitSets = new List<int>();
            ulong newBits = board.Ostrich.Bonked & ~bonkedBefore;
            for (int i = 0; i < nb && i < 64; i++)
                if ((newBits & (1UL << i)) != 0) hitSets.Add(i);
            if (mark != null) mark.NotePecks(board.Ostrich.PeckIndex);

            bool died = !board.Ostrich.Alive;
            // He stays put (no daze/flee from Sync) until the last hit has landed; then he exits.
            if (died && mark != null) mark.HoldFlee = true;
            var held = new HashSet<int>();
            void Hold(int i)
            {
                if ((uint)i >= (uint)nb || held.Contains(i) || _locked.Contains(i)) return;
                held.Add(i);
                Lock(i);
            }
            for (int i = 0; i < log.Count; i++) { Hold(log[i].From); Hold(log[i].To); }
            if (died)
                for (int i = 0; i < nb; i++)
                    if (fullBefore[i].HasValue && board.Branches[i].Broken) Hold(i);
            Hold(finale);
            for (int i = 0; i < hitSets.Count; i++) Hold(hitSets[i]);

            _ostrichFight = true;
            var landAt = new Dictionary<SpriteRenderer, float>();
            float lastLand = 0f;
            bool first = true;
            for (int k = 0; k < log.Count && mark != null; k++)
            {
                var h = log[k];
                if ((uint)h.From >= (uint)nb || (uint)h.To >= (uint)nb) continue;
                var srcStack = stacks[h.From];
                var to = stacks[h.To];
                // Never teleport: if a stack drifted, still glide the bird that is on the limb's tip.
                // Only an empty source limb is left to SyncAll.
                if (srcStack.Count == 0) continue;
                var sr = srcStack[srcStack.Count - 1];
                if (sr == null) continue;
                // One relocation at a time: the previous bird lands (plus a short beat) before the next
                // wind-up, so the flock never lurches and no two flights overlap.
                if (k > 0)
                {
                    float settleAt = lastLand + OstrichTurn.PeckGapSec;
                    while (Time.time < settleAt) { if (gen != _motionGen) { ReleaseFight(held); yield break; } yield return null; }
                }
                if (landAt.TryGetValue(sr, out float until))
                    while (Time.time < until) { if (gen != _motionGen) { ReleaseFight(held); yield break; } yield return null; }
                srcStack.RemoveAt(srcStack.Count - 1);
                to.Add(sr);
                var dstView = _garden.Branches[h.To];
                if (dstView == null) continue;

                if (first) { first = false; SfxLibrary.Honk(); }
                mark.BeginStrike(sr.transform.position + Vector3.up * 0.05f);
                while (mark.Striking && mark.StrikeTime < OstrichStrike.Contact)
                {
                    if (gen != _motionGen) { ReleaseFight(held); yield break; }
                    yield return null;
                }
                var hitPos = sr.transform.position;
                Vector3 away = hitPos - mark.Aim.position;
                away.z = 0f;
                if (away.sqrMagnitude < 1e-4f) away = Vector3.down;
                away.Normalize();
                Impact(hitPos, h.Bird.Color);
                var dest = dstView.SeatWorld(h.ToCount)
                    + dstView.transform.TransformVector(new Vector3(0f, BranchView.RestLift, 0f));
                StartCoroutine(StartleThenFly(sr, away, dest, dstView.FromRight, dstView, 0, true));
                float flight = StartleSec + OstrichTurn.FlightSec;
                landAt[sr] = Time.time + flight;
                lastLand = Mathf.Max(lastLand, Time.time + flight);
                while (mark.Striking)
                {
                    if (gen != _motionGen) { ReleaseFight(held); yield break; }
                    yield return null;
                }
            }

            // Every completed set hits the ostrich: its birds fly up one after another, each lands an impact,
            // then they fly back and reseat. The branch does not break here.
            int hitNo = hitsBefore;
            for (int k = 0; k < hitSets.Count && mark != null; k++)
            {
                hitNo++;
                int bi = hitSets[k];
                yield return OstrichHit(bi, hitNo, fullBefore[bi] ?? BirdColor.Ruby, gen);
                if (gen != _motionGen) { if (mark != null) mark.HoldFlee = false; ReleaseFight(held); yield break; }
            }
            if (mark != null && died)
            {
                // Third hit: a staggering, flustered exit off screen with a feather puff. Branches break after.
                yield return OstrichExit(mark, gen);
                if (gen != _motionGen) { ReleaseFight(held); yield break; }
            }

            // The other cleared sets fly to their feeder rather than blinking out.
            if (died)
            {
                for (int i = 0; i < nb; i++)
                {
                    if (!fullBefore[i].HasValue || !board.Branches[i].Broken) continue;
                    var v = i < _garden.Branches.Length ? _garden.Branches[i] : null;
                    if (v == null) continue;
                    int slot = SlotOf(liveBefore, fullBefore[i].Value);
                    int idx = 0;
                    for (int s = 0; s < BranchState.Cap; s++)
                    {
                        var sr = v.Birds[s];
                        if (sr == null || !sr.enabled) continue;
                        StartCoroutine(StartleThenFly(sr, Vector3.up, FeederMouth(slot), v.FromRight, null, idx, false));
                        lastLand = Mathf.Max(lastLand, Time.time + FlockRedistribute.Duration(idx));
                        idx++;
                    }
                }
            }

            // Let every flight land before the views re-sync to the board.
            while (Time.time < lastLand)
            {
                if (gen != _motionGen) { ReleaseFight(held); yield break; }
                yield return null;
            }
            ReleaseFight(held);
        }

        // One hit. Birds of the set launch in order (OstrichHitTimeline), hop to the ostrich, impact, hold, then
        // hop home to their own seats. Time-driven from one clock so the order is serial by construction.
        IEnumerator OstrichHit(int bi, int hitNo, BirdColor col, int gen)
        {
            var mark = _garden.Ostrich;
            var v = bi < _garden.Branches.Length ? _garden.Branches[bi] : null;
            if (mark == null || v == null) yield break;
            var birds = new List<SpriteRenderer>(BranchState.Cap);
            for (int s = 0; s < BranchState.Cap; s++)
                if (v.Birds[s] != null && v.Birds[s].enabled) birds.Add(v.Birds[s]);
            int n = birds.Count;
            if (n == 0) yield break;
            var homes = new Vector3[n];
            var idles = new BirdIdle[n];
            for (int i = 0; i < n; i++)
            {
                homes[i] = birds[i].transform.position;
                idles[i] = birds[i].GetComponent<BirdIdle>();
                if (idles[i] != null)
                {
                    idles[i].Sleeping = false;
                    idles[i].Frozen = true;
                    idles[i].Flapping = true;
                    idles[i].Lift = 0f;
                }
                birds[i].transform.SetParent(_garden.Root, true);
                birds[i].sortingOrder = FlockSort.Fly;
            }
            var arrived = new bool[n];
            bool reacted = false, swayed = false;
            float total = OstrichHitTimeline.HitSec(n);
            float t = 0f;
            while (t < total)
            {
                if (gen != _motionGen) yield break;
                t += Time.deltaTime;
                Vector3 head = mark.Aim.position + Vector3.down * 0.1f;
                for (int i = 0; i < n; i++)
                {
                    var tr = birds[i] != null ? birds[i].transform : null;
                    if (tr == null) continue;
                    var tgt = head + new Vector3((i - (n - 1) * 0.5f) * 0.14f, 0.06f * (i % 2), 0f);
                    tr.position = OstrichHitTimeline.BirdAt(n, i, t, homes[i], tgt);
                    float since = t - OstrichHitTimeline.ArriveAt(i);
                    float sq = since >= 0f && since < 0.14f ? 1f + 0.28f * Mathf.Sin(since / 0.14f * Mathf.PI) : 1f;
                    tr.localScale = new Vector3(BranchView.BirdScale.x * sq, BranchView.BirdScale.y / sq, 1f);
                    if (!arrived[i] && t >= OstrichHitTimeline.ArriveAt(i))
                    {
                        arrived[i] = true;
                        Impact(tr.position, col);
                        if (i == n - 1) StartCoroutine(Wow.Burst(tr.position, col, _garden.Root, 2));
                        if (!reacted)
                        {
                            reacted = true;
                            mark.ShowHit(hitNo);
                            SfxLibrary.Honk();
                        }
                    }
                }
                if (!swayed && t >= OstrichHitTimeline.LandAt(n, n - 1) - 0.1f) { swayed = true; v.LandSway(); }
                yield return null;
            }
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                birds[i].transform.position = homes[i];
                birds[i].transform.localScale = BranchView.BirdScale;
            }
            float gap = 0f;
            while (gap < OstrichHitTimeline.BetweenHitsSec)
            {
                if (gen != _motionGen) yield break;
                gap += Time.deltaTime;
                yield return null;
            }
        }

        // The exit: stagger in place, then a hurried run off screen, feathers flying at both ends.
        IEnumerator OstrichExit(OstrichMark mark, int gen)
        {
            mark.BeginExit();
            SfxLibrary.Honk();
            StartCoroutine(Wow.Burst(mark.Aim.position, BirdColor.Peach, _garden.Root, 3));
            bool puffRun = false, puffMid = false;
            float t = 0f;
            int half = OstrichAnim.Count(OstrichAct.Flee) / 2;
            while (mark.Exiting && t < OstrichHitTimeline.ExitTotalSec * 3f)
            {
                if (gen != _motionGen) yield break;
                t += Time.deltaTime;
                if (!puffRun && mark.Act == OstrichAct.Flee)
                {
                    puffRun = true;
                    StartCoroutine(Wow.Burst(mark.Aim.position, BirdColor.Peach, _garden.Root, 3));
                    Sfx.FlockFlutter(4);
                }
                if (puffRun && !puffMid && mark.Act == OstrichAct.Flee && mark.Frame >= half)
                {
                    puffMid = true;
                    StartCoroutine(Wow.Burst(mark.Aim.position, BirdColor.Peach, _garden.Root, 2));
                }
                yield return null;
            }
            float beat = 0f;
            while (beat < OstrichHitTimeline.BreakDelaySec)
            {
                if (gen != _motionGen) yield break;
                beat += Time.deltaTime;
                yield return null;
            }
        }

        void ReleaseFight(HashSet<int> held)
        {
            var m = _garden != null ? _garden.Ostrich : null;
            if (m != null) m.HoldFlee = false;
            foreach (int i in held) Unlock(i);
            _ostrichFight = false;
        }

        static int SlotOf(BirdColor?[] live, BirdColor c)
        {
            if (live != null)
                for (int i = 0; i < live.Length; i++)
                    if (live[i].HasValue && live[i].Value == c) return i;
            return 0;
        }

        Vector3 FeederMouth(int slot)
        {
            if (_garden.Feeders != null && slot >= 0 && slot < _garden.Feeders.Length && _garden.Feeders[slot] != null)
                return _garden.Feeders[slot].Mouth;
            return Vector3.up * 3f;
        }

        // Contact: color pop (shared Wow burst) and the bird's squawk.
        void Impact(Vector3 pos, BirdColor col)
        {
            StartCoroutine(Wow.Burst(pos, col, _garden.Root, 1));
            Sfx.Chirp(col);
            Sfx.FlockFlutter(2);
        }

        // Kick away from the head with a squash, then the shared arc flight to dest.
        IEnumerator StartleThenFly(SpriteRenderer sr, Vector3 away, Vector3 dest, bool faceLeft, BranchView limb, int index, bool kick)
        {
            int gen = _motionGen;
            if (sr == null) yield break;
            var tr = sr.transform;
            var idle = sr.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.Sleeping = false;
                idle.Frozen = true;
                idle.Flapping = true;
                idle.Lift = 0f;
            }
            tr.SetParent(_garden.Root, true);
            sr.sortingOrder = FlockSort.Fly;
            sr.enabled = true;
            tr.gameObject.SetActive(true);
            if (kick)
            {
                var p0 = tr.position;
                float t = 0f;
                while (t < StartleSec)
                {
                    if (gen != _motionGen || tr == null) yield break;
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / StartleSec);
                    float e = 1f - (1f - u) * (1f - u);
                    tr.position = p0 + away * (StartleKick * e) + Vector3.up * (0.14f * Mathf.Sin(u * Mathf.PI));
                    float sq = 1f + 0.22f * Mathf.Sin(u * Mathf.PI);
                    tr.localScale = new Vector3(BranchView.BirdScale.x * sq, BranchView.BirdScale.y / sq, 1f);
                    yield return null;
                }
            }
            yield return PerchOne(tr, dest, index, faceLeft, limb);
        }
    }
}
