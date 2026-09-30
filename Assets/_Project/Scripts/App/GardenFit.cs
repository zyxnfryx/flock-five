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
            public int Column; // 0 left, 1 right, 2 bonus row
            public int Row;
            public Vector3 Pos;
            public Vector3 Scale;
        }

        // Branch roots stay at scale 1 so perch gaps and bird sprites never
        // grow when a limb is gone. Only the positions are recomputed.
        public static readonly Vector3 LimbScale = Vector3.one;

        public static IEnumerator Tween(WorldBuilder.Garden garden, Board board, bool instant)
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
                if (v == null) continue;
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

            float u = 0f;
            const float dur = 0.42f;
            while (u < 1f)
            {
                u += Time.deltaTime / dur;
                float k = u * u * (3f - 2f * u);
                for (int i = 0; i < n; i++)
                    views[i].Fit(Vector3.Lerp(fromPos[i], toPos[i], k), Vector3.Lerp(fromS[i], LimbScale, k));
                yield return null;
            }
            for (int i = 0; i < n; i++)
                views[i].Fit(toPos[i], LimbScale);
        }

        public static void Collect(Board board, Camera cam, List<Spot> into)
        {
            if (into == null) return;
            into.Clear();
            if (board == null) return;
            var left = new List<int>();
            var right = new List<int>();
            var bonus = new List<int>();
            var bonusRight = new List<bool>();
            int plain = 0;
            int giftOrd = 0;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                var st = board.Branches[i];
                if (st == null) continue;
                if (st.IsBonus)
                {
                    // Side stays put when the other gift is gone, so a snap
                    // cannot slide the survivor across the screen.
                    if (!st.Broken)
                    {
                        bonus.Add(i);
                        bonusRight.Add((giftOrd & 1) == 1);
                    }
                    giftOrd++;
                    continue;
                }
                bool rightSide = (plain & 1) == 1;
                if (!st.Broken)
                {
                    if (rightSide) right.Add(i);
                    else left.Add(i);
                }
                plain++;
            }

            float gap = WorldBuilder.RowGap;
            float y0 = WorldBuilder.RowY0;
            int tall = Mathf.Max(left.Count, right.Count);
            // Keep the top perch under the feeder tray after the island pushes it down.
            float shelf = WorldBuilder.FeederShelf();
            const float topReach = 1.30f;
            if (y0 + topReach > shelf) y0 = shelf - topReach;
            float lowest = tall > 0 ? y0 - (tall - 1) * gap : y0;
            float bonusY = BonusY(cam, lowest);
            float sep = tall > 0 ? lowest - bonusY : gap;
            if (tall > 0 && sep < gap * 0.85f)
                y0 += gap * 0.85f - sep;
            if (y0 + topReach > shelf)
            {
                float over = y0 + topReach - shelf;
                if (tall > 1) gap = Mathf.Max(1.12f, gap - over / (tall - 1));
                y0 = shelf - topReach;
            }

            float x = WorldBuilder.EdgeX(cam, 1f);
            for (int i = 0; i < left.Count; i++)
                into.Add(new Spot { Index = left[i], Column = 0, Row = i, Pos = new Vector3(-x, y0 - i * gap, 0f), Scale = LimbScale });
            for (int i = 0; i < right.Count; i++)
                into.Add(new Spot { Index = right[i], Column = 1, Row = i, Pos = new Vector3(x, y0 - i * gap, 0f), Scale = LimbScale });
            for (int i = 0; i < bonus.Count; i++)
            {
                bool onRight = bonusRight[i];
                into.Add(new Spot
                {
                    Index = bonus[i],
                    Column = 2,
                    Row = 0,
                    Pos = new Vector3(onRight ? x : -x, bonusY, 0f),
                    Scale = LimbScale
                });
            }
        }

        // Bottom gift row: one gap under the lowest plain limb, never under the
        // restart / hive band. Tall phones letterbox that band below the play field.
        public static float BonusY(Camera cam, float lowestPlain)
        {
            float floor = HudFloor(cam);
            float y = lowestPlain - WorldBuilder.RowGap;
            if (y < floor) y = floor;
            return y;
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
