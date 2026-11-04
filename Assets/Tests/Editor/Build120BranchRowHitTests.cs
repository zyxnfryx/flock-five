#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R7 (Readability): a tap on a perched bird picks the bird's own limb, never the limb above;
    // a tap on the wood still picks that wood. Pure geometry on BranchHit.RowRect / PickRow.
    public static class Build120BranchRowHitTests
    {
        const string Out = "/tmp/flock-five-b120-branch-row-tests.txt";
        static readonly float[] SeatX = { 220f, 458f, 695f, 933f, 1148f };
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-rowhit] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            foreach (bool right in new[] { false, true })
            {
                float x = right ? 3.21f : -3.21f;
                const float pitch = WorldBuilder.RowGap;
                var woods = new List<Rect>(); var rows = new List<Rect>();
                for (int r = 0; r < 4; r++)
                {
                    var c = new Vector2(x, 2f - r * pitch);
                    var wood = BranchHit.WoodRect(c, right, false);
                    woods.Add(wood); rows.Add(BranchHit.RowRect(wood, c.y + BranchHit.BirdRowTopLocal));
                }
                string side = right ? "R" : "L";
                for (int r = 0; r < 4; r++)
                {
                    float cy = 2f - r * pitch;
                    for (int s = 0; s < 5; s++)
                    {
                        float sx = (SeatX[s] - 640f) * WorldBuilder.WoodScaleX / 140f;
                        float bx = x + (right ? -sx : sx);
                        // Bird body: seat + RestLift, its head ~0.3 above, its feet at the seat.
                        foreach (float up in new[] { 0.34f, 0.75f, 0.95f })
                        {
                            var p = new Vector2(bx, cy + up);
                            int got = BranchHit.PickRow(p, woods, rows);
                            Check("bird tap picks own row " + side + r + " seat" + s + " up" + up, got == r, "got " + got);
                        }
                        // Wood tap (middle of the limb art) picks the limb.
                        var w = new Vector2(bx, cy - 0.2f);
                        Check("wood tap picks own row " + side + r + " seat" + s, BranchHit.PickRow(w, woods, rows) == r, "got " + BranchHit.PickRow(w, woods, rows));
                    }
                }
                Check("far tap misses " + side, BranchHit.PickRow(new Vector2(0f, 9f), woods, rows) == -1, "");
            }
            Debug.Log("[build120-rowhit] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif
