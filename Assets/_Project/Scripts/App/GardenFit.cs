using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    public static class GardenFit
    {
        // Left and right columns pack independently so a missing limb never
        // leaves a hole in the middle of the other side. Bonus perches stack
        // on the right under that column.
        public static IEnumerator Tween(WorldBuilder.Garden garden, Board board, bool instant)
        {
            if (garden.Branches == null || board == null) yield break;

            var left = Column(board, garden, false);
            var right = Column(board, garden, true);
            int extras = 0;
            for (int i = WorldBuilder.GiftIndex; i < board.Branches.Count; i++)
                if (Alive(board, garden, i)) extras++;
            int rows = WorldBuilder.Rows;
            int slots = Mathf.Max(left.Count, right.Count + extras);
            float fill = slots <= 0 ? 1f : 1f - Mathf.Min(slots, rows) / (float)rows;
            float s = Mathf.Lerp(1f, 1.58f, fill);
            // Length stays near 1 so five birds still fit; only puff height when sparse.
            float sx = Mathf.Lerp(1f, 1.06f, fill);
            float gap = Mathf.Lerp(WorldBuilder.RowGap, 2.72f, fill);
            float used = (rows - 1) * WorldBuilder.RowGap;
            float pack = Mathf.Max(0f, slots - 1) * gap;
            float yTop = WorldBuilder.RowY0 - 0.5f * (used - pack) * fill;
            float x = WorldBuilder.EdgeX(garden.Cam, sx);
            var scale = new Vector3(sx, s, 1f);

            var views = new List<BranchView>();
            var fromPos = new List<Vector3>();
            var fromS = new List<Vector3>();
            var toPos = new List<Vector3>();

            for (int i = 0; i < left.Count; i++)
                TryAdd(garden, board, left[i], new Vector3(-x, yTop - i * gap, 0f), views, fromPos, fromS, toPos);
            int ri = 0;
            for (int i = 0; i < right.Count; i++, ri++)
                TryAdd(garden, board, right[i], new Vector3(x, yTop - ri * gap, 0f), views, fromPos, fromS, toPos);
            float giftX = WorldBuilder.EdgeX(garden.Cam, 1.22f, WorldBuilder.GiftWoodScaleX);
            for (int i = WorldBuilder.GiftIndex; i < board.Branches.Count; i++)
            {
                if (!Alive(board, garden, i)) continue;
                TryAdd(garden, board, i, new Vector3(giftX, yTop - ri * gap, 0f), views, fromPos, fromS, toPos);
                ri++;
            }

            int n = views.Count;
            if (n <= 0) yield break;
            if (instant)
            {
                for (int i = 0; i < n; i++)
                    views[i].Fit(toPos[i], ScaleOf(views[i], scale));
                yield break;
            }

            float u = 0f;
            const float dur = 0.42f;
            while (u < 1f)
            {
                u += Time.deltaTime / dur;
                float k = u * u * (3f - 2f * u);
                for (int i = 0; i < n; i++)
                {
                    var sc = ScaleOf(views[i], scale);
                    views[i].Fit(Vector3.Lerp(fromPos[i], toPos[i], k), Vector3.Lerp(fromS[i], sc, k));
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                views[i].Fit(toPos[i], ScaleOf(views[i], scale));
        }

        static Vector3 ScaleOf(BranchView v, Vector3 packed)
        {
            if (v != null && v.IsGift) return new Vector3(1.22f, 1.22f, 1f);
            return packed;
        }

        static List<int> Column(Board board, WorldBuilder.Garden garden, bool right)
        {
            var live = new List<int>();
            for (int row = 0; row < WorldBuilder.Rows; row++)
            {
                int i = row * 2 + (right ? 1 : 0);
                if (Alive(board, garden, i)) live.Add(i);
            }
            return live;
        }

        static bool Alive(Board board, WorldBuilder.Garden garden, int i)
        {
            if (i < 0 || i >= board.Branches.Count || i >= garden.Branches.Length) return false;
            if (board.Branches[i].Broken) return false;
            return garden.Branches[i] != null;
        }

        static void TryAdd(WorldBuilder.Garden garden, Board board, int i,
            Vector3 dest, List<BranchView> views, List<Vector3> fromPos, List<Vector3> fromS, List<Vector3> toPos)
        {
            if (!Alive(board, garden, i)) return;
            var v = garden.Branches[i];
            views.Add(v);
            fromPos.Add(v.Planted);
            fromS.Add(v.transform.localScale);
            toPos.Add(dest);
        }
    }
}


