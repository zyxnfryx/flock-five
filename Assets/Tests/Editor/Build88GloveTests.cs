#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 88. Every coach glove shares one facing rule: the approach line is
    // locked before the first pixel, and the tap uses that same heading.
    public static class Build88GloveTests
    {
        const string Out = "/tmp/flock-five-b88-glove-tests.txt";
        const string WrongPath = "/tmp/gaudit-wrong.txt";
        const float PhoneW = 1179f;
        const float PhoneH = 2556f;
        const float PadW = 2048f;
        const float PadH = 2732f;

        struct Spot
        {
            public string Name;
            public int Group;
            public Vector2 Aim;
            public float Lift;
            public bool FromLeft;
            public float Approach;
            public Rect Keep;
        }

        struct Sample
        {
            public Spot Spot;
            public Vector2 Perch;
            public Vector2 Planned;
            public float Angle;
            public bool Mirror;
            public bool Same;
            public bool LegacyWrong;
            public float Legacy0;
            public float Legacy1;
        }

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                UnityEngine.Debug.Log("[build88glove] " + line);
            }
            var prevWindow = DeviceFit.TestWindow;
            try
            {
                FlockFiveApp.GloveAuditTexture();
                var phone = Audit(PhoneW, PhoneH);
                var pad = Audit(PadW, PadH);
                var wrong = new List<string>();
                for (int i = 0; i < phone.Count; i++)
                {
                    var s = phone[i];
                    Check("phone-" + s.Spot.Name,
                        s.Same,
                        "frame0 " + s.Angle.ToString("0.0") + " mirror " + s.Mirror
                        + (s.LegacyWrong ? "  WAS " + s.Legacy0.ToString("0") + " -> " + s.Legacy1.ToString("0") : ""));
                    if (s.LegacyWrong)
                        wrong.Add(s.Spot.Name + "  " + s.Legacy0.ToString("0") + " -> " + s.Legacy1.ToString("0"));
                }
                for (int i = 0; i < pad.Count; i++)
                {
                    var s = pad[i];
                    Check("pad-" + s.Spot.Name, s.Same, s.Angle.ToString("0.0"));
                }
                File.WriteAllText(WrongPath, wrong.Count == 0 ? "(none)\n" : string.Join("\n", wrong.ToArray()) + "\n");
                Check("wrong-list", true, wrong.Count + " before-fix");
                bool sheet = WriteSheet(phone, "/Users/zfxgames/wkspaces/wingout/b88-glove-audit.png");
                Check("contact-sheet", sheet, "/Users/zfxgames/wkspaces/wingout/b88-glove-audit.png");
                Sample daily = default;
                bool found = false;
                for (int i = 0; i < phone.Count; i++)
                    if (phone[i].Spot.Name == "daily-claim") { daily = phone[i]; found = true; break; }
                bool clip = found && WriteDailyClip(daily, PhoneW, PhoneH);
                Check("daily-clip", clip, "/Users/zfxgames/wkspaces/wingout/b88-daily-glove.mp4");
                sb.AppendLine("pass " + pass + " fail " + fail);
                File.WriteAllText(Out, sb.ToString());
                if (fail > 0) throw new System.Exception("build88 glove fail " + fail);
            }
            finally
            {
                DeviceFit.TestWindow = prevWindow;
            }
        }

        static List<Sample> Audit(float w, float h)
        {
            DeviceFit.TestWindow = w / h;
            float s = DeviceFit.Ui(w, h);
            float dh = FlockFiveApp.GloveAuditDh(s);
            float dw = FlockFiveApp.GloveAuditDw(dh);
            var spots = Spots(w, h, s);
            var list = new List<Sample>(spots.Count);
            for (int i = 0; i < spots.Count; i++)
                list.Add(Measure(spots[i], s, dh, dw, w, h));
            return list;
        }

        static Sample Measure(Spot spot, float s, float dh, float dw, float w, float h)
        {
            Plan(spot, s, w, out var planned, out float legacyAng, out bool legacyMirror);
            Vector2 perch = planned;
            FlockFiveApp.CommitApproachFace(ref perch, spot.Aim, dh, dw, w, h, legacyMirror, out float ang, out bool mirror);
            float a0 = FlockFiveApp.ResolveDrawAngle(ang, mirror, true, false, spot.Approach);
            bool m0 = mirror;
            Vector2 p0 = perch;
            FlockFiveApp.DrawnGlovePose(ref p0, ref a0, ref m0, dw, dh, w, h, true);
            float a1 = FlockFiveApp.ResolveDrawAngle(ang, mirror, true, false, spot.Approach);
            bool m1 = mirror;
            Vector2 p1 = spot.Aim;
            FlockFiveApp.DrawnGlovePose(ref p1, ref a1, ref m1, dw, dh, w, h, true);
            bool same = Mathf.Abs(Mathf.DeltaAngle(a0, a1)) < 1f && m0 == m1;
            if ((spot.Aim - perch).sqrMagnitude > 16f)
                same = same && Mathf.Abs(Mathf.DeltaAngle(a0, FlockFiveApp.GloveApproachDeg(perch, spot.Aim))) < 1.5f;
            bool was = LegacySwings(spot, planned, legacyAng, legacyMirror, s, dh, dw, w, h, out float l0, out float l1);
            return new Sample
            {
                Spot = spot,
                Perch = perch,
                Planned = planned,
                Angle = a0,
                Mirror = m0,
                Same = same && m0 == mirror,
                LegacyWrong = was,
                Legacy0 = l0,
                Legacy1 = l1
            };
        }

        // The old draw flipped anywhere on the arc, not only at the perch. Sample the
        // approach the way TapArc did and compare each heading to the tap.
        static bool LegacySwings(Spot spot, Vector2 planned, float legacyAng, bool legacyMirror,
            float s, float dh, float dw, float w, float h, out float frame0, out float tap)
        {
            float arch = Mathf.Max(18f * s, dh * 0.20f);
            bool angled = !float.IsNaN(spot.Approach);
            var angs = new float[5];
            var mirs = new bool[5];
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                Vector2 pos;
                if (angled)
                    pos = Vector2.Lerp(planned, spot.Aim, t);
                else
                {
                    float high = Mathf.Min(planned.y, spot.Aim.y);
                    float apexY = high - arch;
                    var ctrl = new Vector2(spot.Aim.x, apexY);
                    float u = 1f - t;
                    pos = planned * (u * u) + ctrl * (2f * u * t) + spot.Aim * (t * t);
                }
                float a = legacyAng;
                bool m = legacyMirror;
                LegacyDraw(ref pos, ref a, ref m, dw, dh, w, h);
                angs[i] = a;
                mirs[i] = m;
            }
            frame0 = angs[0];
            tap = angs[4];
            bool wrong = false;
            for (int i = 0; i < 4; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(angs[i], tap)) > 8f || mirs[i] != mirs[4])
                    wrong = true;
            return wrong;
        }

        // Pre-b88 GloveOnScreen: clamp the pivot, then search a flipped mirror and a swung angle.
        static void LegacyDraw(ref Vector2 pivot, ref float ang, ref bool mirror, float dw, float dh, float w, float h)
        {
            float m = FlockFiveApp.GloveScreenMargin;
            pivot.x = Mathf.Clamp(pivot.x, m, w - m);
            pivot.y = Mathf.Clamp(pivot.y, m, h - m);
            float best = FlockFiveApp.GloveQuadOverflow(pivot, ang, mirror, dw, dh, w, h);
            if (best <= 0.5f) return;
            float bestAng = ang;
            bool bestMir = mirror;
            for (int k = 0; k < 2; k++)
            {
                bool mir = k == 0 ? !mirror : mirror;
                for (int step = 0; step <= 12; step++)
                {
                    for (int sign = 0; sign < 2; sign++)
                    {
                        float a = (k == 0 ? -ang : ang) + (sign == 0 ? 1f : -1f) * step * 15f;
                        float o = FlockFiveApp.GloveQuadOverflow(pivot, a, mir, dw, dh, w, h);
                        if (o + 0.5f < best) { best = o; bestAng = a; bestMir = mir; }
                    }
                    if (best <= 0.5f) break;
                }
                if (best <= 0.5f) break;
            }
            ang = bestAng;
            mirror = bestMir;
        }

        static void Plan(Spot spot, float s, float w, out Vector2 perch, out float legacyAng, out bool legacyMirror)
        {
            float dh = FlockFiveApp.GloveAuditDh(s);
            if (!float.IsNaN(spot.Approach))
            {
                float rad = spot.Approach * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
                var away = -point;
                if (away.sqrMagnitude < 0.0001f) away = new Vector2(0.7071f, 0.7071f);
                away.Normalize();
                float gap = 64f * s;
                perch = spot.Aim + away * (gap + dh * 0.9f);
                legacyAng = spot.Approach;
                legacyMirror = false;
                return;
            }
            bool fromRight = !spot.FromLeft && spot.Aim.x < w * 0.5f;
            var side = new Vector2(fromRight ? 1f : -1f, 0f);
            legacyMirror = spot.FromLeft || spot.Aim.x >= w * 0.5f;
            legacyAng = legacyMirror ? 90f : -90f;
            float lift = float.IsNaN(spot.Lift) ? Mathf.Max(28f * s, dh * 0.34f) : spot.Lift;
            float sideGap = 48f * s;
            if (spot.Keep.width > 8f)
            {
                float outside = side.x > 0f ? spot.Keep.xMax - spot.Aim.x : spot.Aim.x - spot.Keep.x;
                if (outside < 0f) outside = 0f;
                float need = outside + dh * 0.62f;
                if (sideGap < need) sideGap = need;
            }
            perch = spot.Aim + side * sideGap + new Vector2(0f, -lift);
        }

        static List<Spot> Spots(float w, float h, float s)
        {
            var list = new List<Spot>();
            float size = FlockFiveApp.SplashRailSize(w);
            float gap = Mathf.Max(24f, size * 0.30f);
            float railY = h * 0.335f;
            var right = new Rect(w - 20f - size, railY, size, size);
            float top = right.y - size - gap;
            var hive = new Rect(right.center.x - size * 0.5f, top + size + gap, size, size);
            var poker = new Rect(right.center.x - size * 0.5f, top + (size + gap) * 2f, size, size);
            var daily = new Rect(20f + size * 0.5f - size * 0.5f, top, size, size);
            Add(list, "hive-home", 0, hive.center, float.NaN, false, float.NaN, default);
            AddAlbum(list, w, h);
            float[] lx = { 0.50f, 0.32f, 0.70f, 0.18f, 0.80f, 0.40f };
            float[] ly = { 0.48f, 0.52f, 0.44f, 0.46f, 0.42f, 0.36f };
            for (int i = 0; i < 6; i++)
                Add(list, "level-" + (i + 1), 2, new Vector2(w * lx[i], h * ly[i]), float.NaN, false, float.NaN, default);
            CornerAnchor.Garden(w, h, new Rect(0f, 0f, w, h), out _, out _, out _, out _, out var gardenHive);
            Add(list, "hive-garden", 3, gardenHive.center, float.NaN, false, float.NaN, default);
            AddDailyClaim(list, w, h, s);
            Add(list, "daily-rail", 3, daily.center, float.NaN, false, float.NaN, default);
            Add(list, "poker-intro", 3, new Vector2(poker.x + poker.width * 0.56f, poker.y + poker.height * 0.58f), float.NaN, false, -45f, default);
            Add(list, "poker-deal", 3, new Vector2(w * 0.72f, h * 0.62f), float.NaN, false, float.NaN, default);
            Add(list, "poker-back", 3, new Vector2(w * 0.22f, h * 0.78f), float.NaN, false, float.NaN, default);
            Add(list, "gift-watch", 3, new Vector2(w * 0.50f, h * 0.72f), float.NaN, false, float.NaN, default);
            Add(list, "badger-pick", 3, new Vector2(w * 0.33f, h * 0.40f), float.NaN, false, float.NaN, default);
            Add(list, "badger-powers", 3, new Vector2(w * 0.66f, h * 0.55f), float.NaN, false, float.NaN, default);
            Add(list, "adopt-look", 3, new Vector2(w * 0.28f, h * 0.62f), 40f * s, false, float.NaN, default);
            Add(list, "pest-sparrow", 3, new Vector2(w * 0.55f, h * 0.40f), float.NaN, false, float.NaN, default);
            Add(list, "pest-hawk", 3, new Vector2(w * 0.48f, h * 0.36f), float.NaN, false, float.NaN, default);
            Add(list, "pest-bee", 3, new Vector2(w * 0.62f, h * 0.44f), float.NaN, false, float.NaN, default);
            return list;
        }

        static void Add(List<Spot> list, string name, int group, Vector2 aim, float lift, bool fromLeft, float approach, Rect keep)
        {
            list.Add(new Spot
            {
                Name = name,
                Group = group,
                Aim = aim,
                Lift = lift,
                FromLeft = fromLeft,
                Approach = approach,
                Keep = keep
            });
        }

        static void AddDailyClaim(List<Spot> list, float w, float h, float s)
        {
            float inset = 12f * s;
            float span = Mathf.Max(8f, w - inset * 2f);
            float cardW = Mathf.Min(span, Mathf.Min(w * 0.88f, 600f * s));
            if (cardW < 240f) cardW = Mathf.Min(span, 240f);
            float flower = cardW * 0.62f;
            float cap = w * 0.58f;
            if (flower > cap) flower = cap;
            float floor = 120f * s;
            if (flower < floor) flower = floor;
            float cardX = inset + (span - cardW) * 0.5f;
            float fx = cardX + cardW * 0.5f - flower * 0.5f;
            float fy = h * 0.56f;
            var disc = new Rect(fx, fy, flower, flower);
            var aim = new Vector2(disc.x + disc.width * 0.12f, disc.center.y + disc.height * 0.10f);
            Add(list, "daily-claim", 3, aim, float.NaN, true, float.NaN, disc);
        }

        static void AddAlbum(List<Spot> list, float w, float h)
        {
            Rect card;
            try
            {
                var grid = FlockFiveApp.CardText.GridOf(w, h, new Rect(0f, 0f, w, h), h * 0.12f);
                float cx = grid.Gx0 + grid.CellW * 0.5f;
                float cy = grid.Gy0 + grid.CellH * 0.5f;
                card = new Rect(cx - grid.CardW * 0.5f, cy - grid.CardH * 0.5f, grid.CardW, grid.CardH);
                var pager = new Vector2(w * 0.5f, grid.PageBottom + grid.PagerHit * 0.35f);
                Add(list, "album-page", 1, pager, float.NaN, false, float.NaN, default);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[build88glove] grid " + ex.Message);
                card = new Rect(w * 0.08f, h * 0.28f, w * 0.26f, h * 0.18f);
                Add(list, "album-page", 1, new Vector2(w * 0.5f, h * 0.90f), float.NaN, false, float.NaN, default);
            }
            var inspect = new Rect(w * 0.16f, h * 0.20f, w * 0.68f, h * 0.50f);
            Album(list, 1, card, "Tap a card you have\nto enlarge it.", "album-tap", w, h);
            Album(list, 2, inspect, "Tap again to flip.", "album-flip", w, h);
            Album(list, 3, inspect, CaptionLayout.AlbumHoneyBlock, "album-honey", w, h);
            Add(list, "album-upgrade", 1, new Vector2(w * 0.72f, h * 0.70f), float.NaN, false, float.NaN, default);
        }

        static void Album(List<Spot> list, int step, Rect focus, string line, string name, float w, float h)
        {
            try
            {
                var pose = FlockFiveApp.PoseLesson(step, focus, w, h, line, step == 3 ? 5 : 0);
                Add(list, name, 1, pose.Aim, float.NaN, false, pose.Approach, default);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[build88glove] " + name + " " + ex.Message);
                Add(list, name, 1, focus.center, float.NaN, false, -90f, default);
            }
        }

        static bool WriteSheet(List<Sample> spots, string path)
        {
            int cols = 2;
            int rows = (spots.Count + cols - 1) / cols;
            int cellW = 520;
            int cellH = 250;
            int sheetW = cols * cellW;
            int sheetH = rows * cellH;
            var px = new Color32[sheetW * sheetH];
            var bg = new Color32(28, 48, 36, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            DeviceFit.TestWindow = PhoneW / PhoneH;
            float gloveDh = FlockFiveApp.GloveAuditDh(DeviceFit.Ui(PhoneW, PhoneH));
            var tex = FlockFiveApp.GloveAuditTexture();
            var src = tex.GetPixels32();
            var tip = FlockFiveApp.GloveAuditTip();
            for (int i = 0; i < spots.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int x0 = col * cellW;
                int y0 = row * cellH;
                PaintCell(px, sheetW, sheetH, x0, y0, cellW, cellH, spots[i], src, tex.width, tex.height, tip, gloveDh);
            }
            return SavePng(px, sheetW, sheetH, path);
        }

        static void PaintCell(Color32[] px, int sheetW, int sheetH, int x0, int y0, int cellW, int cellH,
            Sample sample, Color32[] src, int sw, int sh, Vector2 tip, float dh)
        {
            Fill(px, sheetW, sheetH, x0 + 4, y0 + 4, cellW - 8, 28, new Color32(18, 32, 24, 255));
            var ink = new Color32(236, 228, 196, 255);
            DrawString(px, sheetW, sheetH, sample.Spot.Name, 18, x0 + 12, y0 + 6, ink);
            DrawString(px, sheetW, sheetH, "frame 0", 14, x0 + 16, y0 + 32, ink);
            DrawString(px, sheetW, sheetH, "tap", 14, x0 + cellW / 2 + 8, y0 + 32, ink);
            float dw = dh * (sw / (float)Mathf.Max(1, sh));
            float viewW = cellW * 0.5f - 12f;
            float viewH = cellH - 58f;
            Frame(sample.Perch, sample.Spot.Aim, dh, viewW, viewH, out var origin, out float scale);
            var line = new Color32(210, 186, 120, 255);
            var gold = new Color32(255, 210, 80, 255);
            PaintPose(px, sheetW, sheetH, src, sw, sh, tip, new Vector2(x0 + 6, y0 + 50), origin, scale,
                sample.Perch, sample.Spot.Aim, sample.Angle, sample.Mirror, dw, dh, line, gold, true);
            PaintPose(px, sheetW, sheetH, src, sw, sh, tip, new Vector2(x0 + cellW * 0.5f + 2, y0 + 50), origin, scale,
                sample.Perch, sample.Spot.Aim, sample.Angle, sample.Mirror, dw, dh, line, gold, false);
        }

        static void PaintPose(Color32[] px, int sheetW, int sheetH, Color32[] src, int sw, int sh, Vector2 tip,
            Vector2 view, Vector2 origin, float scale, Vector2 perch, Vector2 aim, float ang, bool mirror,
            float dw, float dh, Color32 line, Color32 gold, bool atPerch)
        {
            var a = view + origin + perch * scale;
            var b = view + origin + aim * scale;
            Stroke(px, sheetW, sheetH, a, b, line);
            var at = atPerch ? a : b;
            Stamp(px, sheetW, sheetH, src, sw, sh, tip, at, ang, mirror, dw * scale, dh * scale, 1f);
            Dot(px, sheetW, sheetH, b, gold);
        }

        static void Frame(Vector2 perch, Vector2 aim, float pad, float viewW, float viewH, out Vector2 origin, out float scale)
        {
            float minX = Mathf.Min(perch.x, aim.x) - pad;
            float maxX = Mathf.Max(perch.x, aim.x) + pad;
            float minY = Mathf.Min(perch.y, aim.y) - pad;
            float maxY = Mathf.Max(perch.y, aim.y) + pad;
            float bw = Mathf.Max(8f, maxX - minX);
            float bh = Mathf.Max(8f, maxY - minY);
            scale = Mathf.Min(viewW / bw, viewH / bh);
            origin = new Vector2((viewW - bw * scale) * 0.5f - minX * scale, (viewH - bh * scale) * 0.5f - minY * scale);
        }

        static bool WriteDailyClip(Sample daily, float phoneW, float phoneH)
        {
            const int W = 390;
            const int H = 844;
            string dir = "/tmp/gaudit-daily-frames";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var tex = FlockFiveApp.GloveAuditTexture();
            var src = tex.GetPixels32();
            var tip = FlockFiveApp.GloveAuditTip();
            float sc = W / phoneW;
            float dh = FlockFiveApp.GloveAuditDh(DeviceFit.Ui(phoneW, phoneH)) * sc;
            float dw = dh * (tex.width / (float)Mathf.Max(1, tex.height));
            var aim = daily.Spot.Aim * sc;
            var perch = daily.Perch * sc;
            var keep = new Rect(daily.Spot.Keep.x * sc, daily.Spot.Keep.y * sc, daily.Spot.Keep.width * sc, daily.Spot.Keep.height * sc);
            const int frames = 72;
            for (int f = 0; f < frames; f++)
            {
                float t = f / 60f;
                float alpha;
                float travel;
                if (t < 0.18f)
                {
                    alpha = t / 0.18f;
                    travel = 0f;
                }
                else
                {
                    alpha = 1f;
                    float u = Mathf.Clamp01((t - 0.18f) / 0.70f);
                    travel = u * u * (3f - 2f * u);
                }
                var px = new Color32[W * H];
                PaintPhone(px, W, H);
                Fill(px, W, H, (int)keep.x, (int)keep.y, (int)keep.width, (int)keep.height, new Color32(196, 132, 42, 255));
                DrawString(px, W, H, "Tap to claim!", 22, W / 2, (int)(keep.yMax + 18f), new Color32(255, 244, 220, 255));
                var at = Vector2.Lerp(perch, aim, travel);
                Stamp(px, W, H, src, tex.width, tex.height, tip, at, daily.Angle, daily.Mirror, dw, dh, alpha);
                if (!SavePng(px, W, H, dir + "/f" + f.ToString("0000") + ".png")) return false;
            }
            string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
            string mp4 = "/Users/zfxgames/wkspaces/wingout/b88-daily-glove.mp4";
            Directory.CreateDirectory(Path.GetDirectoryName(mp4));
            var psi = new ProcessStartInfo
            {
                FileName = ff,
                Arguments = "-y -framerate 60 -i " + dir + "/f%04d.png -c:v libx264 -pix_fmt yuv420p -movflags +faststart " + mp4,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                if (p == null || !p.WaitForExit(20000) || p.ExitCode != 0) return false;
            }
            try { Directory.Delete(dir, true); } catch { }
            return File.Exists(mp4) && new FileInfo(mp4).Length > 8000;
        }

        static void PaintPhone(Color32[] px, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                byte g = (byte)(40 + 50 * y / h);
                var c = new Color32(24, g, 32, 255);
                int row = y * w;
                for (int x = 0; x < w; x++) px[row + x] = c;
            }
        }

        static void Stamp(Color32[] dst, int dw, int dh, Color32[] src, int sw, int sh, Vector2 tip,
            Vector2 pivot, float ang, bool mirror, float gloveW, float gloveH, float alpha)
        {
            if (alpha < 0.02f || gloveW < 2f || gloveH < 2f) return;
            float rad = ang * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            float widen = mirror ? -1f : 1f;
            float u0 = pivot.x - tip.x * gloveW;
            float v0 = pivot.y - (1f - tip.y) * gloveH;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(pivot.x - gloveW - gloveH));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(pivot.y - gloveW - gloveH));
            int x1 = Mathf.Min(dw - 1, Mathf.CeilToInt(pivot.x + gloveW + gloveH));
            int y1 = Mathf.Min(dh - 1, Mathf.CeilToInt(pivot.y + gloveW + gloveH));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float qx = x - pivot.x;
                    float qy = y - pivot.y;
                    float lx = qx * c + qy * sn;
                    float ly = -qx * sn + qy * c;
                    lx /= widen;
                    float tx = (pivot.x + lx - u0) / gloveW;
                    float ty = (pivot.y + ly - v0) / gloveH;
                    if (tx < 0f || ty < 0f || tx >= 1f || ty >= 1f) continue;
                    int sx = Mathf.Clamp(Mathf.FloorToInt(tx * sw), 0, sw - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(ty * sh), 0, sh - 1);
                    var p = src[sy * sw + sx];
                    if (p.a < 8) continue;
                    float a = (p.a / 255f) * alpha;
                    int i = y * dw + x;
                    var d = dst[i];
                    byte r = (byte)(p.r * a + d.r * (1f - a));
                    byte g = (byte)(p.g * a + d.g * (1f - a));
                    byte b = (byte)(p.b * a + d.b * (1f - a));
                    dst[i] = new Color32(r, g, b, 255);
                }
            }
        }

        static void Dot(Color32[] px, int w, int h, Vector2 p, Color32 c)
        {
            int cx = Mathf.RoundToInt(p.x);
            int cy = Mathf.RoundToInt(p.y);
            for (int y = -3; y <= 3; y++)
                for (int x = -3; x <= 3; x++)
                    if (x * x + y * y <= 10) Put(px, w, h, cx + x, cy + y, c);
        }

        static void Stroke(Color32[] px, int w, int h, Vector2 a, Vector2 b, Color32 c)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                Put(px, w, h, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), c);
            }
        }

        static void Fill(Color32[] px, int w, int h, int x, int y, int rw, int rh, Color32 c)
        {
            for (int yy = 0; yy < rh; yy++)
                for (int xx = 0; xx < rw; xx++)
                    Put(px, w, h, x + xx, y + yy, c);
        }

        static void Put(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            px[y * w + x] = c;
        }

        // 3x5 glyphs, five rows of three bits. Batchmode font textures are not readable.
        static readonly string GlyphOrder = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-!";
        static readonly string[] GlyphRows =
        {
            "010101111101101", "110101110101110", "011100100100011", "110101101101110", "111100110100111",
            "111100110100100", "011100101101011", "101101111101101", "111010010010111", "001001001101010",
            "101110110101101", "100100100100111", "101111101101101", "110101101101101", "010101101101010",
            "110101110100100", "010101101011011", "110101110101101", "011100010001110", "111010010010010",
            "101101101101011", "101101101101010", "101101101111101", "101101010101101", "101101010010010",
            "111001010100111", "111101101101111", "010110010010111", "110001010100111", "110001110001110",
            "101101111001001", "111100110001110", "011100110101010", "111001010010010", "010101010101010",
            "010101011001110", "000000111000000", "010010010000010"
        };

        static void DrawString(Color32[] px, int w, int h, string text, int size, int x, int y, Color32 col)
        {
            if (string.IsNullOrEmpty(text)) return;
            int scale = Mathf.Max(1, size / 6);
            int pen = x;
            string up = text.ToUpperInvariant();
            for (int i = 0; i < up.Length; i++)
            {
                if (up[i] == ' ') { pen += 4 * scale; continue; }
                int gi = GlyphOrder.IndexOf(up[i]);
                if (gi < 0) { pen += 4 * scale; continue; }
                string rows = GlyphRows[gi];
                for (int ry = 0; ry < 5; ry++)
                    for (int rx = 0; rx < 3; rx++)
                        if (rows[ry * 3 + rx] == '1')
                            for (int sy = 0; sy < scale; sy++)
                                for (int sx = 0; sx < scale; sx++)
                                    Put(px, w, h, pen + rx * scale + sx, y + ry * scale + sy, col);
                pen += 4 * scale;
            }
        }

        static bool SavePng(Color32[] topDown, int w, int h, string path)
        {
            var flipped = new Color32[topDown.Length];
            for (int y = 0; y < h; y++)
                System.Array.Copy(topDown, y * w, flipped, (h - 1 - y) * w, w);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(flipped);
            tex.Apply(false, false);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return File.Exists(path) && new FileInfo(path).Length > 1000;
        }
    }
}
#endif
