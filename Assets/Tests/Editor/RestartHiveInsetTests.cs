#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: the top-right garden Restart must share the hive's right padding. The hit
    // plate sits InsetRef inside safe.xMax. DrawHiveGlow paints HiveGlowPadFrac past that
    // plate, and Restart's right edge is that halo (HiveArtRight) — the same seat when the
    // hive is not drawn, so the button does not jump when the hive appears. Judged sizes:
    // iPhone 402 and 412, iPad 820, Samsung, and the pixel sizes those devices lay out.
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
                    var alone = CornerAnchor.TopBarRestart(d.W, d.H, safe, sc, top, hv);

                    float edge = CornerAnchor.RightEdge(d.W, safe, sc);
                    float art = CornerAnchor.HiveArtRight(hv);
                    float halo = hv.width * CornerAnchor.HiveGlowPadFrac;
                    float inset = safe.xMax - rs.xMax;
                    float glass = d.W - rs.xMax;
                    float oldPad = 6f * sc;
                    float restartGlyph = CornerAnchor.RestartGlyphRight(rs);
                    float hiveGlyph = CornerAnchor.HiveGlyphRight(hv);
                    // Plate stays on RightEdge. Restart's rect is the full halo quad.
                    // When that quad is wider than the glyph pull, the counted edges
                    // (restart rim inside the rect, hive glow drawn with the pull)
                    // agree within 2px.
                    bool glyphs = halo + 0.01f < CornerAnchor.HiveGlowGlyphPull
                        || Mathf.Abs(restartGlyph - hiveGlyph) <= 2f;
                    bool aligned = Mathf.Abs(rs.xMax - art) <= 0.51f
                        && Mathf.Abs(hv.xMax - edge) <= 0.51f
                        && Mathf.Abs(art - (hv.xMax + halo)) <= 0.51f
                        && halo > 4f
                        && Mathf.Abs(inset - (CornerAnchor.InsetRef * sc - halo)) <= 0.51f
                        && glyphs;
                    bool sameWhenHidden = Mathf.Abs(alone.xMax - rs.xMax) <= 0.51f
                        && Mathf.Abs(alone.x - rs.x) <= 0.51f
                        && Mathf.Abs(alone.width - rs.width) <= 0.51f
                        && Mathf.Abs(alone.y - rs.y) <= 0.51f;
                    // Halo trims the old 40-ref inset, and still leaves the glass well clear of the old 6*scale pad.
                    bool notFlush = glass >= 24f && inset > oldPad + 12f;
                    Check("right edge matches hive halo " + d.Name, aligned && sameWhenHidden && notFlush,
                        "restartX " + rs.xMax.ToString("0.0") + " hivePlate " + hv.xMax.ToString("0.0")
                        + " halo " + art.ToString("0.0") + " pad " + halo.ToString("0.0")
                        + " glyphs " + restartGlyph.ToString("0.0") + "/" + hiveGlyph.ToString("0.0")
                        + " d " + (restartGlyph - hiveGlyph).ToString("0.0")
                        + " inset " + inset.ToString("0.0") + " glass " + glass.ToString("0.0")
                        + " oldPad " + oldPad.ToString("0.0") + " hiddenX " + alone.xMax.ToString("0.0"));

                    float field = CornerAnchor.PlayfieldTopGui(d.W, d.H);
                    float feeder = CornerAnchor.FeederRightGui(d.W, d.H);
                    float clear = Mathf.Max(0f, art - feeder);
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

            try
            {
                // Inspector viewports (point size). iPad is wider than 9:16, so the game lays
                // the HUD out in the centered phone column and the screenshot x adds OffX.
                // Restart's screenshot xMax is the halo, not the plate (a98a07d6 measured the
                // plate: 676/360/368 against a halo near 695/372/380).
                void Inspector(string name, float screenW, float screenH)
                {
                    float col = screenW;
                    float off = 0f;
                    if (screenW / screenH > DeviceFit.PhoneAspect + 0.03f)
                    {
                        col = Mathf.Min(screenW, Mathf.Round(screenH * DeviceFit.PhoneAspect));
                        off = Mathf.Floor((screenW - col) * 0.5f);
                    }
                    DeviceFit.TestWindow = col / screenH;
                    var full = new Rect(0f, 0f, col, screenH);
                    CornerAnchor.Garden(col, screenH, full, out _, out _, out _, out var rs, out var hv);
                    float art = CornerAnchor.HiveArtRight(hv);
                    float pad = art - hv.xMax;
                    float restartGlyph = off + CornerAnchor.RestartGlyphRight(rs);
                    float hiveGlyph = off + CornerAnchor.HiveGlyphRight(hv);
                    float glyphDelta = restartGlyph - hiveGlyph;
                    bool ok = Mathf.Abs(rs.xMax - art) <= 0.51f
                        && pad > 8f
                        && Mathf.Abs(pad - hv.width * CornerAnchor.HiveGlowPadFrac) <= 0.51f
                        && rs.xMax <= col + 0.01f
                        && Mathf.Abs(glyphDelta) <= 2f
                        && Mathf.Abs(CornerAnchor.HiveGlowGlyphPull - 12f) <= 0.01f;
                    Check("inspector halo " + name, ok,
                        "screenRestart " + (off + rs.xMax).ToString("0.0")
                        + " screenPlate " + (off + hv.xMax).ToString("0.0")
                        + " glyphs " + restartGlyph.ToString("0.0") + "/" + hiveGlyph.ToString("0.0")
                        + " d " + glyphDelta.ToString("0.0")
                        + " pad " + pad.ToString("0.0")
                        + " col " + col.ToString("0") + " off " + off.ToString("0"));
                }
                Inspector("ipad 820", 820f, 1180f);
                Inspector("iphone 402", 402f, 874f);
                Inspector("iphone 412", 412f, 915f);
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }

            string app = System.IO.File.ReadAllText(System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath),
                "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
            Check("glow pull is the drawn outset",
                app.IndexOf("CornerAnchor.HiveGlowOutset", System.StringComparison.Ordinal) >= 0
                && app.IndexOf("hive.width * 0.18f", System.StringComparison.Ordinal) < 0
                && app.IndexOf("hive.width * CornerAnchor.HiveGlowPadFrac", System.StringComparison.Ordinal) < 0,
                "DrawHiveGlow");

            Debug.Log("[restart-inset] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif
