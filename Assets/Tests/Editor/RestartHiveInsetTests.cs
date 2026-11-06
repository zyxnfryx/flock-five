#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: the top-right garden Restart hugs the glass. Its right edge must match the
    // hive (InsetRef inside safe.xMax) on the sizes he judges — iPhone 402 and 412, iPad 820,
    // Samsung — and on the pixel sizes those devices lay out. The inset is the same when the
    // hive is not drawn, so Restart does not jump inward the moment the hive appears.
    // Batch: -executeMethod FlockFive.Editor.RestartHiveInsetTests.Run
    public static class RestartHiveInsetTests
    {
        struct Dev
        {
            public string Name;
            public float W, H, Top, Bot, Side;
        }

        [MenuItem("Flock Five/Restart Hive Inset Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[restart-inset] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            // Point widths Brandon named, then the pixel buffers the HUD actually lays out
            // (3x phones, 2x iPad). The iPad column is the 9:16 pillar PhoneArea plays in.
            var devs = new[]
            {
                new Dev { Name = "iphone 402", W = 402f, H = 874f, Top = 62f, Bot = 34f },
                new Dev { Name = "iphone 412", W = 412f, H = 915f, Top = 59f, Bot = 34f },
                new Dev { Name = "ipad 820", W = 820f, H = 1180f, Top = 24f, Bot = 20f },
                new Dev { Name = "ipad 820 column", W = 664f, H = 1180f, Top = 24f, Bot = 20f },
                new Dev { Name = "samsung 360", W = 360f, H = 780f, Top = 32f, Bot = 16f },
                new Dev { Name = "iphone 402 px", W = 1206f, H = 2622f, Top = 186f, Bot = 102f },
                new Dev { Name = "iphone 412 px", W = 1236f, H = 2745f, Top = 177f, Bot = 102f },
                new Dev { Name = "ipad 820 px", W = 1640f, H = 2360f, Top = 48f, Bot = 40f },
                new Dev { Name = "ipad 820 column px", W = 1328f, H = 2360f, Top = 48f, Bot = 40f },
                new Dev { Name = "samsung px", W = 1080f, H = 2340f, Top = 96f, Bot = 48f },
                // Curved-edge safe inset: still the hive's air inside safe.xMax, so the
                // button stays off the glass when the safe rect is already inset.
                new Dev { Name = "samsung px side safe", W = 1080f, H = 2340f, Top = 96f, Bot = 48f, Side = 24f },
            };

            var prev = DeviceFit.TestWindow;
            try
            {
                foreach (var d in devs)
                {
                    DeviceFit.TestWindow = d.W / d.H;
                    var safe = new Rect(0f, d.Bot, d.W - d.Side, d.H - d.Bot - d.Top);
                    // TopBarRestart is the seat whether or not the hive is drawn. Garden's
                    // restart rect is that call, so the inset cannot jump when the hive appears.
                    CornerAnchor.Garden(d.W, d.H, safe, out float sc, out float top, out _, out var rs, out var hv);
                    var alone = CornerAnchor.TopBarRestart(d.W, d.H, safe, sc, top);

                    float edge = CornerAnchor.RightEdge(d.W, safe, sc);
                    float inset = safe.xMax - rs.xMax;
                    float glass = d.W - rs.xMax;
                    float oldPad = 6f * sc;
                    bool aligned = Mathf.Abs(rs.xMax - hv.xMax) <= 0.51f
                        && Mathf.Abs(rs.xMax - edge) <= 0.51f
                        && Mathf.Abs(hv.xMax - edge) <= 0.51f
                        && Mathf.Abs(inset - CornerAnchor.InsetRef * sc) <= 0.51f;
                    bool sameWhenHidden = Mathf.Abs(alone.xMax - rs.xMax) <= 0.51f
                        && Mathf.Abs(alone.x - rs.x) <= 0.51f
                        && Mathf.Abs(alone.width - rs.width) <= 0.51f
                        && Mathf.Abs(alone.y - rs.y) <= 0.51f;
                    bool notFlush = glass >= 36f && inset > oldPad + 8f;
                    Check("right edge matches hive " + d.Name, aligned && sameWhenHidden && notFlush,
                        "restartX " + rs.xMax.ToString("0.0") + " hiveX " + hv.xMax.ToString("0.0")
                        + " inset " + inset.ToString("0.0") + " glass " + glass.ToString("0.0")
                        + " oldPad " + oldPad.ToString("0.0") + " hiddenX " + alone.xMax.ToString("0.0"));

                    float field = CornerAnchor.PlayfieldTopGui(d.W, d.H);
                    float feeder = CornerAnchor.FeederRightGui(d.W, d.H);
                    float clear = Mathf.Max(0f, edge - feeder);
                    bool above = rs.yMax <= field + 0.01f && rs.yMax <= top + 0.01f && rs.yMin + 0.01f >= d.H - safe.yMax;
                    bool square = Mathf.Abs(rs.width - rs.height) <= 0.01f && rs.width > 8f;
                    bool inGutter = rs.xMin + 0.51f >= feeder && rs.xMax <= safe.xMax + 0.01f;
                    float floor = Mathf.Min(CornerAnchor.MinTapPx(0f), clear);
                    bool tappable = rs.width + 0.51f >= floor && rs.width <= clear + 0.51f;
                    bool feedersClear = true;
                    string hit = "";
                    float tall = CornerAnchor.TallFor(d.W, d.H);
                    float fy = WorldBuilder.FeederYFor(tall, (d.H - safe.yMax) / d.H);
                    for (int count = 2; count <= 3 && feedersClear; count++)
                        for (int i = 0; i < count; i++)
                        {
                            float fx = FeederRow.X(i, count);
                            var a = CornerAnchor.WorldToGui(d.W, d.H, fx - FeederRow.BulbHalf, fy + 1.34f);
                            var b = CornerAnchor.WorldToGui(d.W, d.H, fx + FeederRow.BulbHalf, fy - 1.47f);
                            var bulb = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                            if (bulb.Overlaps(rs)) { feedersClear = false; hit = "feeder " + count + ":" + i; }
                        }
                    bool offHive = !rs.Overlaps(hv);
                    Check("top bar, clear of feeders " + d.Name,
                        above && square && inGutter && tappable && feedersClear && offHive,
                        "size " + rs.width.ToString("0.0") + " yMax " + rs.yMax.ToString("0")
                        + " field " + field.ToString("0") + " clear " + clear.ToString("0.0") + " " + hit);
                }
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }

            Debug.Log("[restart-inset] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif
