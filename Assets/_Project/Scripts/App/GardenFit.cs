using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    public static class GardenFit
    {
        // Playfield is rows of a left limb and a right limb. A row stays
        // until BOTH sides are gone. Surviving limbs never swap columns.
        public static IEnumerator Tween(WorldBuilder.Garden garden, Board board, bool instant)
        {
            if (garden.Branches == null || board == null) yield break;

            var liveRows = new List<int>();
            int rows = WorldBuilder.Rows;
            for (int row = 0; row < rows; row++)
            {
                int L = row * 2;
                int R = L + 1;
                if (Alive(board, garden, L) || Alive(board, garden, R))
                    liveRows.Add(row);
            }
            int r = liveRows.Count;
            if (r <= 0) yield break;

            float fill = 1f - r / (float)rows;
            float s = Mathf.Lerp(1f, 1.58f, fill);
            float gap = Mathf.Lerp(WorldBuilder.RowGap, 2.72f, fill);
            float used = (rows - 1) * WorldBuilder.RowGap;
            float pack = Mathf.Max(0f, r - 1) * gap;
            float yTop = WorldBuilder.RowY0 - 0.5f * (used - pack) * fill;
            float x = WorldBuilder.LimbX;
            var scale = new Vector3(s, s, 1f);

            var views = new List<BranchView>();
            var fromPos = new List<Vector3>();
            var fromS = new List<Vector3>();
            var toPos = new List<Vector3>();

            for (int i = 0; i < r; i++)
            {
                int src = liveRows[i];
                float y = yTop - i * gap;
                TryAdd(garden, board, src * 2, new Vector3(-x, y, 0f), views, fromPos, fromS, toPos);
                TryAdd(garden, board, src * 2 + 1, new Vector3(x, y, 0f), views, fromPos, fromS, toPos);
            }

            int n = views.Count;
            if (n <= 0) yield break;
            if (instant)
            {
                for (int i = 0; i < n; i++)
                    views[i].Fit(toPos[i], scale);
                yield break;
            }

            float u = 0f;
            const float dur = 0.42f;
            while (u < 1f)
            {
                u += Time.deltaTime / dur;
                float k = u * u * (3f - 2f * u);
                for (int i = 0; i < n; i++)
                    views[i].Fit(Vector3.Lerp(fromPos[i], toPos[i], k), Vector3.Lerp(fromS[i], scale, k));
                yield return null;
            }
            for (int i = 0; i < n; i++)
                views[i].Fit(toPos[i], scale);
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
