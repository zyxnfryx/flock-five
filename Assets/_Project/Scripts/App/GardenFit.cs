using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    public static class GardenFit
    {
        public struct Spot
        {
            public int Index;
            public int Column; // 0 left, 1 right
            public int Row;
            public Vector3 Pos;
            public Vector3 Scale;
        }

        // Length stays 1 so perch gaps and the gap to center never change
        // when a limb is gone. Only the row pitch is recomputed.
        public static readonly Vector3 LimbScale = Vector3.one;

        // Long enough that a row, or a short stack, reads as a slide.
        const float SettleSeconds = 0.62f;
        const float GapTol = 0.16f;

        static int _fitSerial;
        static bool _busy;
        static readonly List<Spot> _snapSpots = new List<Spot>(16);
        static readonly List<int> _packLeft = new List<int>(8);
        static readonly List<int> _packRight = new List<int>(8);

        public static bool Busy => _busy;

        // A stopped tween must not leave the snap guard stuck.
        public static void ClearBusy()
        {
            _fitSerial++;
            _busy = false;
        }

        public static IEnumerator Tween(WorldBuilder.Garden garden, Board board, bool instant)
        {
            return Settle(garden, board, instant);
        }

        // One settle for every caller, including a gap noticed mid-play.
        // X stays on the column bezel. Only Y eases. A falling limb is left to BreakAway.
        static IEnumerator Settle(WorldBuilder.Garden garden, Board board, bool instant)
        {
            // A play settle already owns this gap. Wait it out. A newer serial
            // (ClearBusy, or an instant fit) owns the pose, so do not start another ease.
            if (!instant && _busy)
            {
                int waitFor = _fitSerial;
                while (_busy && _fitSerial == waitFor)
                    yield return null;
                if (_fitSerial != waitFor)
                    yield break;
            }

            int run = ++_fitSerial;
            _busy = true;
            try
            {
                if (garden.Branches == null || board == null) yield break;
                var spots = new List<Spot>(board.Branches.Count);
                Collect(board, garden.Cam, spots);
                var views = new List<BranchView>(spots.Count);
                var fromPos = new List<Vector3>(spots.Count);
                var fromS = new List<Vector3>(spots.Count);
                var toPos = new List<Vector3>(spots.Count);
                for (int i = 0; i < spots.Count; i++)
                {
                    int bi = spots[i].Index;
                    if ((uint)bi >= (uint)garden.Branches.Length) continue;
                    var v = garden.Branches[bi];
                    if (v == null || v.Breaking) continue;
                    views.Add(v);
                    fromPos.Add(v.Planted);
                    fromS.Add(v.transform.localScale);
                    toPos.Add(spots[i].Pos);
                }

                int n = views.Count;
                if (n <= 0) yield break;
                if (instant)
                {
                    for (int i = 0; i < n; i++)
                        views[i].Fit(toPos[i], LimbScale);
                    yield break;
                }
                if (!NeedsEase(views, fromPos, toPos)) yield break;

                float u = 0f;
                while (u < 1f)
                {
                    if (run != _fitSerial) yield break;
                    u += Time.deltaTime / SettleSeconds;
                    if (u > 1f) u = 1f;
                    float k = Mathf.SmoothStep(0f, 1f, u);
                    for (int i = 0; i < n; i++)
                        ApplyEase(views[i], fromPos[i], fromS[i], toPos[i], k);
                    yield return null;
                }
                if (run != _fitSerial) yield break;
                for (int i = 0; i < n; i++)
                {
                    if (views[i] == null || views[i].Breaking) continue;
                    views[i].Fit(toPos[i], LimbScale);
                }
            }
            finally
            {
                if (run == _fitSerial) _busy = false;
            }
        }

        static bool NeedsEase(List<BranchView> views, List<Vector3> fromPos, List<Vector3> toPos)
        {
            const float eps = 0.0008f;
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                if (v == null || v.Breaking) continue;
                if (Mathf.Abs(fromPos[i].y - toPos[i].y) > eps) return true;
                if (Mathf.Abs(fromPos[i].x - toPos[i].x) > eps) return true;
                var s = v.transform.localScale;
                if (Mathf.Abs(s.x - LimbScale.x) > eps || Mathf.Abs(s.y - LimbScale.y) > eps)
                    return true;
            }
            return false;
        }

        // X locks to the packed bezel. Y eases. Scale eases onto LimbScale.
        static void ApplyEase(BranchView view, Vector3 fromPos, Vector3 fromS, Vector3 toPos, float k)
        {
            if (view == null || view.Breaking) return;
            var p = Vector3.Lerp(fromPos, toPos, k);
            p.x = toPos.x;
            var s = Vector3.Lerp(fromS, LimbScale, k);
            s.x = LimbScale.x;
            view.Fit(p, s);
        }

        // Packed Y is the source of truth. Wind moves the transform, not Planted.
        // True when a survivor (or its bonus sign) sits off the packed Y.
        // The caller starts one Tween(..., false). Fitting here would park the
        // column before that ease could see a delta.
        public static bool SnapIfGapped(WorldBuilder.Garden garden, Board board)
        {
            if (_busy || board == null || garden.Branches == null) return false;
            Collect(board, garden.Cam, _snapSpots);
            for (int i = 0; i < _snapSpots.Count; i++)
            {
                var view = SpotView(garden, _snapSpots[i].Index);
                if (view == null || view.Breaking) continue;
                if (Mathf.Abs(view.Planted.y - _snapSpots[i].Pos.y) > GapTol)
                    return true;
            }
            return false;
        }

        static BranchView SpotView(WorldBuilder.Garden garden, int index)
        {
            if (garden.Branches == null || (uint)index >= (uint)garden.Branches.Length) return null;
            return garden.Branches[index];
        }

        public static void Collect(Board board, Camera cam, List<Spot> into)
        {
            if (into == null) return;
            into.Clear();
            if (board == null) return;
            var left = _packLeft;
            var right = _packRight;
            left.Clear();
            right.Clear();
            int plain = 0;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                var st = board.Branches[i];
                if (st == null || st.IsBonus) continue;
                if (!st.Broken)
                {
                    if ((plain & 1) == 1) right.Add(i);
                    else left.Add(i);
                }
                plain++;
            }
            // Bonus limbs are the last row of their own column, then the same
            // pitch as every other limb. A clear packs that column from the top,
            // so the sign rises with the limbs under the gap. Ordinal keeps the
            // side when the other gift is gone, so it cannot slide across.
            int giftOrd = 0;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                var st = board.Branches[i];
                if (st == null || !st.IsBonus) continue;
                if (!st.Broken)
                {
                    if ((giftOrd & 1) == 1) right.Add(i);
                    else left.Add(i);
                }
                giftOrd++;
            }

            // Before any vertical fit. Pitch and count must not revise this.
            float x = WorldBuilder.ColumnX();

            float gap = WorldBuilder.RowGap;
            float y0 = WorldBuilder.RowY0;
            int tall = Mathf.Max(left.Count, right.Count);
            // Keep the top perch under the feeder tray after the island pushes it down.
            float shelf = WorldBuilder.FeederShelf();
            const float topReach = 1.30f;
            if (y0 + topReach > shelf) y0 = shelf - topReach;
            // Whole column, bonus included, stays above the restart / hive band.
            // Tall phones letterbox that band below the play field.
            float yCap = shelf - topReach;
            float floor = HudFloor(cam);
            if (tall > 1)
            {
                float bottom = y0 - (tall - 1) * gap;
                if (bottom < floor)
                {
                    float span = yCap - floor;
                    if (span > 0f && (tall - 1) * gap > span)
                        gap = Mathf.Max(1.12f, span / (tall - 1));
                    float lifted = floor + (tall - 1) * gap;
                    y0 = lifted < yCap ? lifted : yCap;
                }
            }

            for (int i = 0; i < left.Count; i++)
                into.Add(new Spot { Index = left[i], Column = 0, Row = i, Pos = new Vector3(-x, y0 - i * gap, 0f), Scale = LimbScale });
            for (int i = 0; i < right.Count; i++)
                into.Add(new Spot { Index = right[i], Column = 1, Row = i, Pos = new Vector3(x, y0 - i * gap, 0f), Scale = LimbScale });
        }

        public static float HudFloor(Camera cam)
        {
            float camY = cam != null ? cam.transform.position.y : WorldBuilder.CamRestY;
            float half = cam != null ? cam.orthographicSize : WorldBuilder.CamOrtho;
            float bottom = camY - half;
            float tall = Mathf.Max(1f, PortraitLock.TallFactor());
            const float hudFrac = 0.175f;
            float letter = (tall - 1f) / (2f * tall);
            float overlap = Mathf.Max(0f, hudFrac - letter);
            float inset = overlap * tall * (half * 2f);
            return bottom + inset + 0.70f;
        }

        public static bool RowsPacked(List<Spot> spots, int column)
        {
            int n = 0;
            var seen = new bool[32];
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].Column != column) continue;
                if (spots[i].Row < 0 || spots[i].Row >= seen.Length) return false;
                if (seen[spots[i].Row]) return false;
                seen[spots[i].Row] = true;
                n++;
            }
            for (int r = 0; r < n; r++)
                if (!seen[r]) return false;
            return true;
        }
    }
}
