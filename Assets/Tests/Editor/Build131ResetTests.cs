#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b131 (Brandon): the garden Restart button lives in the top bar. Its hit rect never intersects any
    // branch / bird / perch-pad hit reach (CornerAnchor.PlayfieldTopGui), never the bottom ad row, stays inside
    // the safe area and keeps a 44 pt tap target, on tall and short iPhones and the iPad phone-aspect area.
    // Batch: -executeMethod FlockFive.Editor.Build131ResetTests.Run
    public static class Build131ResetTests
    {
        const string Out = "/tmp/flock-five-b131-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build131] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // w, h, safe top, safe bottom. Tall iPhones, short (SE / 9:16), Android tall, and the iPad
            // phone-aspect (9:16) area at 11" and 13" heights.
            var devs = new[]
            {
                new Vector4(1179f, 2556f, 177f, 102f), new Vector4(1290f, 2796f, 204f, 102f), new Vector4(1170f, 2532f, 141f, 102f),
                new Vector4(750f, 1334f, 40f, 0f), new Vector4(640f, 1136f, 40f, 0f), new Vector4(1080f, 2400f, 88f, 48f),
                new Vector4(1328f, 2360f, 48f, 40f), new Vector4(1536f, 2732f, 48f, 40f), new Vector4(1159f, 2064f, 48f, 40f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0") + " asp " + (w / h).ToString("0.00");
                DeviceFit.TestWindow = w / h;
                var safe = new Rect(0f, bot, w, h - bot - top);
                CornerAnchor.Garden(w, h, safe, out _, out float hudTop, out _, out var rs, out var hv);
                float field = CornerAnchor.PlayfieldTopGui(w, h);
                CornerAnchor.AdRowBand(w, h, out var sl, out var sr, out var bl, out var br, out _, out _);
                bool offField = rs.yMax <= field && rs.yMax <= hudTop;
                bool offAd = !rs.Overlaps(sl) && !rs.Overlaps(sr) && !rs.Overlaps(bl) && !rs.Overlaps(br);
                bool inSafe = rs.yMin >= h - safe.yMax - 0.01f && rs.xMin >= safe.xMin && rs.xMax <= safe.xMax + 0.01f;
                bool bigEnough = rs.width >= CornerAnchor.MinTapPx(0f) && rs.height >= CornerAnchor.MinTapPx(0f);
                bool offHive = !rs.Overlaps(hv);
                Check("restart off playfield " + tag, offField && offAd && inSafe && bigEnough && offHive,
                    "restart " + rs.xMin.ToString("0") + "," + rs.yMin.ToString("0") + " " + rs.width.ToString("0") + "px yMax " + rs.yMax.ToString("0")
                    + " fieldTop " + field.ToString("0") + " hudTop " + hudTop.ToString("0"));

                // Hit order (HitHud then HitBranch / HitFeeder), pure geometry at this size.
                float tall = CornerAnchor.TallFor(w, h);
                float topNorm = (h - safe.yMax) / h;
                // A tap on Restart's centre reaches Restart.
                Check("tap reaches restart " + tag, FlockFiveApp.HudClaim(rs.center, rs, hv, true, hudTop) == FlockFiveApp.HudTap.Restart, rs.center.ToString());
                // Feeders, 2 and 3: Restart clear of every bulb, and a bulb tap is not Restart.
                float fy = WorldBuilder.FeederYFor(tall, topNorm);
                for (int count = 2; count <= 3; count++)
                {
                    bool fok = true; string fd = "";
                    for (int i = 0; i < count; i++)
                    {
                        float fx = FeederRow.X(i, count);
                        var a = CornerAnchor.WorldToGui(w, h, fx - FeederRow.BulbHalf, fy + 1.34f);
                        var b = CornerAnchor.WorldToGui(w, h, fx + FeederRow.BulbHalf, fy - 1.47f);
                        var bulb = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                        if (bulb.Overlaps(rs) || FlockFiveApp.HudClaim(bulb.center, rs, hv, true, hudTop) == FlockFiveApp.HudTap.Restart)
                        { fok = false; fd += " feeder" + i + " " + bulb; }
                    }
                    Check("restart clear of " + count + " feeders " + tag, fok, "restart " + rs + fd);
                }
                // Selected birds sent to the lower-left empty branch above the ad row, its right mirror,
                // and both ad branches: the tap is not claimed by the HUD and the nearest branch wood is
                // the target, within HitBranch's sending reach (3.25; 3.45 for an empty branch).
                float limbX = WorldBuilder.EdgeX(null, 1f);
                int low = WorldBuilder.Rows - 1;
                float rowY = WorldBuilder.RowY0For(tall) - low * WorldBuilder.RowGap;
                var woods = new System.Collections.Generic.List<Rect>();
                var names = new System.Collections.Generic.List<string>();
                for (int r = 0; r < WorldBuilder.Rows; r++)
                {
                    float y = WorldBuilder.RowY0For(tall) - r * WorldBuilder.RowGap;
                    woods.Add(BranchHit.WoodRect(new Vector2(-limbX, y), false, false)); names.Add("L" + r);
                    woods.Add(BranchHit.WoodRect(new Vector2(limbX, y), true, false)); names.Add("R" + r);
                }
                woods.Add(BranchHit.WoodRect(new Vector2(-limbX, WorldBuilder.GiftY), false, true)); names.Add("adL");
                woods.Add(BranchHit.WoodRect(new Vector2(limbX, WorldBuilder.GiftY), true, true)); names.Add("adR");
                string[] want = { "L" + low, "R" + low, "adL", "adR" };
                foreach (var target in want)
                {
                    var wood = woods[names.IndexOf(target)];
                    // Centre, outer (screen-edge) end, inner end, and a perch tap just above the wood.
                    var taps = new[] { wood.center, new Vector2(Mathf.Lerp(wood.xMin, wood.xMax, target.EndsWith("R") || target == "adR" ? 0.85f : 0.15f), wood.center.y),
                        new Vector2(Mathf.Lerp(wood.xMin, wood.xMax, target.EndsWith("R") || target == "adR" ? 0.15f : 0.85f), wood.center.y), new Vector2(wood.center.x, wood.center.y + 0.25f) };
                    bool ok = true; string why = "";
                    foreach (var p in taps)
                    {
                        var gui = CornerAnchor.WorldToGui(w, h, p.x, p.y);
                        var claim = FlockFiveApp.HudClaim(gui, rs, hv, true, hudTop);
                        int best = -1; float bd = float.MaxValue;
                        for (int k = 0; k < woods.Count; k++) { float d = BranchHit.RectDist(woods[k], p); if (d < bd) { bd = d; best = k; } }
                        if (claim != FlockFiveApp.HudTap.None || best < 0 || names[best] != target || bd > 3.25f)
                        { ok = false; why += " tap " + p + " claim " + claim + " nearest " + (best >= 0 ? names[best] : "-") + " d " + bd.ToString("0.00"); }
                    }
                    Check("send reaches " + target + " " + tag, ok, why);
                }
            }
            DeviceFit.TestWindow = null;
            Debug.Log("[build131] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif
