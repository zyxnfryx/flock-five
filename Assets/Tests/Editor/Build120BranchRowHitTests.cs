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

                // Playtest #240 4a: empty limbs reserve no bird band, so the whole painted wood of the limb
                // above (down to its bottom edge) picks that limb. Rows 1 and 3 empty, rows 0 and 2 occupied.
                var rows2 = new List<Rect>(); var birds = new List<Rect>(); var birdRow = new List<int>();
                for (int r = 0; r < 4; r++)
                {
                    float cy = 2f - r * pitch; bool occ = r % 2 == 0;
                    rows2.Add(BranchHit.RowRect(woods[r], occ ? cy + BranchHit.BirdRowTopLocal : woods[r].yMax));
                    if (occ) for (int s = 0; s < 5; s++)
                    {
                        float sx = (SeatX[s] - 640f) * WorldBuilder.WoodScaleX / 140f;
                        float bx = x + (right ? -sx : sx);
                        birds.Add(Rect.MinMaxRect(bx - 0.28f, cy + 0.2f, bx + 0.28f, cy + BranchHit.BirdRowTopLocal)); birdRow.Add(r);
                    }
                }
                for (int r = 0; r < 3; r++)
                {
                    var wd = woods[r];
                    for (int s = 0; s < 5; s++)
                    {
                        float sx = (SeatX[s] - 640f) * WorldBuilder.WoodScaleX / 140f;
                        float bx = Mathf.Clamp(x + (right ? -sx : sx), wd.xMin + 0.05f, wd.xMax - 0.05f);
                        float cy = 2f - r * pitch;
                        foreach (float y in new[] { wd.yMin + 0.03f, cy - 0.5f, wd.yMin + (wd.yMax - wd.yMin) * 0.15f })
                        {
                            if (y < wd.yMin || y > wd.yMax) continue;
                            var p = new Vector2(bx, y);
                            bool belowEmpty = (r + 1) % 2 == 1;
                            if (!belowEmpty)
                            {
                                // Lower limb occupied: only a tap inside a drawn bird may go below.
                                bool onBird = false;
                                for (int k = 0; k < birds.Count; k++) if (birdRow[k] == r + 1 && birds[k].Contains(p)) onBird = true;
                                if (onBird) continue;
                            }
                            int got = BranchHit.PickRow(p, woods, rows2, birds, birdRow);
                            Check("bottom-of-wood tap picks own row " + side + r + " seat" + s + " y" + (y - cy).ToString("0.00") + (belowEmpty ? " (below empty)" : " (below occupied)"), got == r, "got " + got);
                        }
                    }
                    var e = BranchHit.PickRow(new Vector2((wd.xMin + wd.xMax) * 0.5f, wd.yMin + 0.01f), woods, rows2);
                    Check("bottom edge, no bird list " + side + r, e == r, "got " + e);
                }
                // Empty limb itself: a tap on its own wood still picks it.
                for (int r = 1; r < 4; r += 2)
                {
                    var wd = woods[r]; var c = new Vector2((wd.xMin + wd.xMax) * 0.5f, (wd.yMin + wd.yMax) * 0.5f);
                    Check("empty limb wood picks itself " + side + r, BranchHit.PickRow(c, woods, rows2, birds, birdRow) == r, "");
                }
            }
            Debug.Log("[build120-rowhit] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif
