#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b129 evidence (editor only). Drop /tmp/flock-five-b129-badger and enter play mode (Unity without -batchmode).
        // v4: the fight page at 1170x2532 (390x844 @3x), badger normal and frozen, each with the measured gap between
        // the fighter rect (the ice block when frozen) and the pick-comb hex, and the gap to the right HUD column.
        public IEnumerator ShotB129()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            string dir = "/Users/zfxgames/wkspaces/ffb129-out";
            try { var t = File.ReadAllText("/tmp/flock-five-b129-dir").Trim(); if (t.Length > 0) dir = t; } catch { }
            Directory.CreateDirectory(dir);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            bool wasOn = BadgerSchedule.Enabled;
            BadgerSchedule.Enabled = true;
            try
            {
                File.AppendAllText(log, "START\n");
                if (EditorGameSize != null) EditorGameSize(1170, 2532);
                yield return new WaitForSecondsRealtime(1.2f);
                yield return OpenRepeatFight(log);
                foreach (bool frozen in new[] { false, true })
                {
                    SetFrozen(frozen);
                    for (float w = 0f; w < 1.0f; w += Kick()) { _bgLine = frozen ? BadgerCopy.FreezeSprayFrozen : BadgerCopy.YourPick; yield return null; }
                    yield return new WaitForEndOfFrame();
                    string tag = frozen ? "frozen" : "normal";
                    SaveFrame(dir + "/" + tag + "-1170x2532.png");
                    File.AppendAllText(log, B129Gap(tag, frozen));
                }
                SetFrozen(false);
                File.WriteAllText(dir + "/done", "OK\n");
            }
            finally
            {
                BadgerSchedule.Enabled = wasOn;
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        IEnumerator OpenRepeatFight(string log)
        {
            if (_home == HomeFace.Badger) LeaveBadger();
            yield return new WaitForSecondsRealtime(0.5f);
            bool opened = OpenBadgerFight(20); // appearance 2: a repeat fight, no lesson
            File.AppendAllText(log, "OPEN " + opened + " home " + _home + "\n");
            float w = 0f;
            while (w < 30f && !(_bgStage == BadgerStage.YourPick && _bgLine == BadgerCopy.YourPick && _bgT > 1.2f))
            { w += Kick(); yield return null; }
            File.AppendAllText(log, "FIGHT stage " + _bgStage + " waited " + w.ToString("0.0") + "\n");
        }

        // Keeps the shot moving: no ad hold or pause may freeze the clocks; returns real seconds.
        static float Kick()
        {
            if (AdPause.Active) AdPause.End();
            GamePause.Reset();
            return Mathf.Max(Time.unscaledDeltaTime, 1f / 120f);
        }

        void SetFrozen(bool on)
        {
            if (_bgFight == null) return;
            var ty = typeof(BadgerFight);
            var fl = BindingFlags.Instance | BindingFlags.NonPublic;
            ty.GetField("_bossSkipLeft", fl)?.SetValue(_bgFight, on ? 1 : 0);
            ty.GetField("_freezeRounds", fl)?.SetValue(_bgFight, on ? 1 : 0);
        }

        static string R(Rect r) => string.Format(CultureInfo.InvariantCulture, "({0:0.0},{1:0.0},{2:0.0},{3:0.0})", r.x, r.y, r.width, r.height);

        // Rest rects measured the way the page draws them: the idle fighter (or the ice block, same unit and right
        // clamp as DrawBadgerIceBlock) against the painted pick-comb hex and the right HUD column.
        string B129Gap(string tag, bool frozen)
        {
            var L = BadgerLayout();
            var spot = BadgerActorNow(L);
            Rect fighter;
            if (frozen)
            {
                var spr = SpriteCatalog.BadgerArt(Badger129.FrozenBlockArt);
                float idleH = BadgerArtHeight("badger_idle_b", spot.Unit);
                float unit = idleH * Badger129.FrozenBlockHeight / Mathf.Max(1f, spr.rect.height);
                var foot = spot.Foot;
                foot.x = BadgerFighterPlace.ClampFootX(foot.x, spr.rect.width * unit, L.ColR.x - 2f);
                fighter = SpriteAnim.Dest(Badger129.FrozenBlockArt, spr.rect.width, spr.rect.height, foot, unit, 1f, 1f);
            }
            else
            {
                var spr = SpriteCatalog.BadgerArt(spot.Frame);
                fighter = SpriteAnim.Dest(spot.Frame, spr.rect.width, spr.rect.height, spot.Foot, spot.Unit, spot.Sx, spot.Sy);
            }
            var comb = _bgBossSlotNow;
            float k = 390f / PhoneArea.W;
            var ci = CultureInfo.InvariantCulture;
            return tag + " screen " + PhoneArea.W + "x" + PhoneArea.H + " frame " + spot.Frame
                + " fighter " + R(fighter) + " comb " + R(comb) + " colR " + R(L.ColR)
                + " combGapPx " + (fighter.xMin - comb.xMax).ToString("0.0", ci)
                + " combGapPt390 " + ((fighter.xMin - comb.xMax) * k).ToString("0.0", ci)
                + " hudGapPx " + (L.ColR.xMin - fighter.xMax).ToString("0.0", ci) + "\n";
        }

        // In-fight stills: the real duel page (bird, combs, columns, boss box) at three
        // sizes and four wear stages. Drop /tmp/ff-badger-fight-shot and enter play
        // without -batchmode. Output dir is /tmp/ff-badger-fight-dir, or the default below.
        public IEnumerator ShotBadgerFight()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            string dir = "/Users/zfxgames/wkspaces/badger_job_fight";
            try { var t = File.ReadAllText("/tmp/ff-badger-fight-dir").Trim(); if (t.Length > 0) dir = t; } catch { }
            string stillDir = Path.Combine(dir, "stills");
            Directory.CreateDirectory(stillDir);
            string log = Path.Combine(dir, "stills.log");
            File.WriteAllText(log, "");
            var json = new System.Text.StringBuilder();
            json.Append("{\n  \"note\": \"Body centre, margins, foot and overlaps are the live duel layout on the captured frame (BadgerActorNow + SpriteAnim + BadgerWearArt.PlacedEdges). Screen X adds the pillar offset. Foot Y is the planted opaque foot. Boss foot Y is the boss-box bottom. Overlap px is the horizontal overlap when the rects also overlap vertically. Tint and halo alpha come from BadgerWearArt. Body mean is a chest patch of opaque body pixels. body_luma is the Rec.709 mean (0.2126 R + 0.7152 G + 0.0722 B, 0-255) of screenshot pixels whose drawn body alpha is above 200, skipping honey-gold token pixels. body_luma_vs_prev is that mean minus the previous stage at the same size (null on fresh). Wrecked uses the crouch pose, so that step mixes pose and tint. same_pose repeats the tint on one sprite with no scene and no halo. Token order is the paint serial (token after badger) plus a honey-pixel check where the token covers opaque body.\",\n  \"stills\": [\n");
            int[][] res = { new[] { 402, 874 }, new[] { 820, 1180 }, new[] { 412, 915 } };
            string[] stages = { "fresh", "scuffed", "bruised", "wrecked" };
            bool wasOn = BadgerSchedule.Enabled;
            BadgerSchedule.Enabled = true;
            bool first = true;
            var freshMean = new Color[res.Length];
            var freshHave = new bool[res.Length];
            var prevLuma = new float[res.Length];
            var prevHave = new bool[res.Length];
            try
            {
                File.AppendAllText(log, "START\n");
                for (int d = 0; d < res.Length; d++)
                {
                    File.AppendAllText(log, "SIZE " + res[d][0] + "x" + res[d][1] + "\n");
                    if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]);
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return OpenRepeatFight(log);
                    yield return WaitBossIdle(log);
                    int target = _bgFight != null ? _bgFight.PlayerTarget : 11;
                    int[] scores =
                    {
                        0,
                        Mathf.Max(1, (target - 1) / 3),
                        Mathf.Max(2, (target * 2 - 1) / 3),
                        Mathf.Min(target - 1, Mathf.Max(3, (target * 2 + 2) / 3)),
                    };
                    for (int i = 0; i < stages.Length; i++)
                    {
                        SetBadgerScore(scores[i]);
                        var want = BadgerPolish.WearOf(scores[i], target);
                        // Stamp the wear change, then wait out the 0.12s punch and the 0.18s lean.
                        // Scale 1 at age 0 is the start of the punch, so the clock has to pass the window.
                        BadgerActorNow(BadgerLayout());
                        float waited = 0f;
                        while (waited < 0.22f)
                        {
                            waited += Kick();
                            yield return null;
                        }
                        float extra = 0f;
                        while (extra < 0.40f)
                        {
                            var probe = BadgerActorNow(BadgerLayout());
                            float now = PlayClock.Now;
                            float punchAge = _bgPunchInit ? now - _bgPunchAt : 0f;
                            float leanAge = _bgLeanInit ? now - _bgLeanAt : 0f;
                            if (punchAge >= BadgerWearArt.PunchSeconds - 0.001f
                                && leanAge >= BadgerPolish.LeanEaseSeconds - 0.001f
                                && probe.Punch <= 1.001f)
                                break;
                            extra += Kick();
                            yield return null;
                        }
                        yield return new WaitForEndOfFrame();
                        string name = res[d][0] + "x" + res[d][1] + "-fight-" + stages[i] + ".png";
                        string path = Path.Combine(stillDir, name);
                        SaveFrame(path);
                        string entry = FightMeasureEntry(path, name, stages[i], want, res[d][0], res[d][1], d, freshMean, freshHave, prevLuma, prevHave);
                        if (!first) json.Append(",\n");
                        first = false;
                        json.Append(entry);
                        File.AppendAllText(log, name + " screen " + Screen.width + "x" + Screen.height
                            + " area " + PhoneArea.W + "x" + PhoneArea.H + " want " + want
                            + " score " + scores[i] + "/" + target + "\n" + entry + "\n");
                    }
                    LeaveBadger();
                }
                json.Append("\n  ],\n");
                try { json.Append(SamePoseSection()); }
                catch (System.Exception ex)
                {
                    json.Append("  \"same_pose_error\": \"" + ex.GetType().Name + "\"\n");
                    File.AppendAllText(log, "SAME_POSE " + ex.Message + "\n");
                }
                json.Append("}\n");
                File.WriteAllText(Path.Combine(dir, "measured.json"), json.ToString());
                File.WriteAllText(Path.Combine(dir, "done"), "OK\n");
                File.AppendAllText(log, "DONE\n");
            }
            finally
            {
                BadgerSchedule.Enabled = wasOn;
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        IEnumerator WaitBossIdle(string log)
        {
            float w = 0f;
            while (w < 8f)
            {
                bool boss = _bgBossIx >= 0;
                float age = boss ? AdPause.Unscaled - _bgBossAt : 0f;
                if (_bgStage == BadgerStage.YourPick && boss && age > BadgerDuel.LungeSeconds + 0.2f)
                    break;
                w += Kick();
                yield return null;
            }
            File.AppendAllText(log, "IDLE stage " + _bgStage + " boss " + _bgBossIx + " waited " + w.ToString("0.0", CultureInfo.InvariantCulture) + "\n");
        }

        void SetBadgerScore(int score)
        {
            if (_bgFight == null) return;
            var prop = typeof(BadgerFight).GetProperty("PlayerScore");
            var set = prop != null ? prop.GetSetMethod(true) : null;
            if (set != null) set.Invoke(_bgFight, new object[] { score });
        }

        static float OverlapW(Rect a, Rect b)
        {
            if (a.width < 1f || a.height < 1f || b.width < 1f || b.height < 1f) return 0f;
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            if (w <= 0.5f || h <= 0.5f) return 0f;
            return w;
        }

        string FightMeasureEntry(string path, string file, string stage, BadgerWear want, int reqW, int reqH, int sizeIx, Color[] freshMean, bool[] freshHave, float[] prevLuma, bool[] prevHave)
        {
            var ci = CultureInfo.InvariantCulture;
            var L = BadgerLayout();
            var spot = BadgerActorNow(L);
            var box = BadgerArenaRects.Split(L.Arena);
            var spr = SpriteCatalog.BadgerArt(spot.Frame);
            float srcW = spr != null ? spr.rect.width : 438f;
            float srcH = spr != null ? spr.rect.height : 640f;
            var drawn = SpriteAnim.Dest(spot.Frame, srcW, srcH, spot.Foot, spot.Unit, spot.Sx, spot.Sy);
            BadgerWearArt.PlacedEdges(spot.Frame, drawn, srcW, srcH,
                out float bodyL, out float bodyR, out float bodyCx, out float contentL, out float contentR, out _);
            int off = PhoneArea.OffX;
            float screenW = Mathf.Max(1f, Screen.width);
            float screenCx = bodyCx + off;
            float left = contentL + off;
            float right = screenW - (contentR + off);
            var content = new Rect(contentL, drawn.y, Mathf.Max(1f, contentR - contentL), drawn.height);
            var pair = BadgerPickComb(L.Arena, L.ColR, L.S);
            var bossComb = _bgBossSlotNow.width > 2f ? _bgBossSlotNow : pair.Comb;
            var youComb = _bgYouSlotNow.width > 2f ? _bgYouSlotNow : pair.You;
            float bird = OverlapW(content, spot.BirdBox);
            float boss = OverlapW(content, bossComb);
            float you = OverlapW(content, youComb);
            float colL = OverlapW(content, L.ColL);
            float colR = OverlapW(content, L.ColR);
            BadgerSlam.EyeUv(spot.Frame, out float eu, out float ev);
            float eyeX = drawn.x + drawn.width * eu;
            float eyeY = drawn.y + drawn.height * ev;
            bool eyeIn = eyeX >= bodyL - 2f && eyeX <= bodyR + 2f && eyeY >= drawn.y - 2f && eyeY <= drawn.yMax + 2f;
            float rad = spot.Degrees * Mathf.Deg2Rad;
            float dx = bodyCx - spot.Foot.x;
            float dy = drawn.center.y - spot.Foot.y;
            float rotX = spot.Foot.x + dx * Mathf.Cos(rad) - dy * Mathf.Sin(rad);
            float lean = rotX - bodyCx;
            float footDelta = spot.Foot.y - box.BossFighter.yMax;
            var tint = BadgerWearArt.BodyTint(want);
            float tintPct = BadgerWearArt.TintAmount(want) * 100f;
            float haloA = BadgerWearArt.HaloAlpha(want);
            string tintHex = TintHex(tint);
            var wearDest = BadgerWearArt.PaintWear(spot.Frame, drawn, srcW, srcH, want).Dest;
            int meanN = 0, opaqueOverlap = 0, goldPx = 0, lumaN = 0;
            float meanR = 0f, meanG = 0f, meanB = 0f, luma = 0f;
            string tokenPixels = "no_overlap";
            string measureProblem = "";
            try
            {
                MeasureBodyAndToken(path, spot, want, drawn, wearDest, bodyL, bodyR, bodyCx,
                    out meanN, out meanR, out meanG, out meanB,
                    out opaqueOverlap, out goldPx, out tokenPixels, out lumaN, out luma);
            }
            catch (System.Exception ex)
            {
                measureProblem = " measure " + ex.GetType().Name + ";";
            }
            if (stage == "fresh" && meanN > 0 && sizeIx >= 0 && sizeIx < freshMean.Length && !freshHave[sizeIx])
            {
                freshMean[sizeIx] = new Color(meanR / 255f, meanG / 255f, meanB / 255f, 1f);
                freshHave[sizeIx] = true;
            }
            float dR = 0f, dG = 0f, dB = 0f;
            bool haveFresh = sizeIx >= 0 && sizeIx < freshHave.Length && freshHave[sizeIx];
            if (haveFresh && meanN > 0 && stage != "fresh")
            {
                dR = meanR - freshMean[sizeIx].r * 255f;
                dG = meanG - freshMean[sizeIx].g * 255f;
                dB = meanB - freshMean[sizeIx].b * 255f;
            }
            bool tokenAfter = FightTokenSerial > FightBadgerSerial && FightBadgerSerial > 0;
            string problems = "";
            if (Mathf.Abs(screenCx - screenW * 0.5f) > 0.6f)
                problems += " body centre " + screenCx.ToString("0.00", ci) + " vs screen mid " + (screenW * 0.5f).ToString("0.00", ci) + ";";
            if (Mathf.Abs(footDelta) > 0.6f)
                problems += " foot Y " + spot.Foot.y.ToString("0.00", ci) + " vs boss " + box.BossFighter.yMax.ToString("0.00", ci) + ";";
            if (Screen.width != reqW || Screen.height != reqH)
                problems += " captured " + Screen.width + "x" + Screen.height + " wanted " + reqW + "x" + reqH + ";";
            if (want == BadgerWear.Wrecked && spot.Frame != "badger_crouch")
                problems += " wrecked frame " + spot.Frame + ";";
            if (Mathf.Abs(lean) > 1f)
                problems += " mood lean " + spot.Degrees.ToString("0.0", ci) + " deg shifts body centre " + lean.ToString("0.0", ci) + " px;";
            if (!eyeIn)
                problems += " eye " + eyeX.ToString("0.0", ci) + "," + eyeY.ToString("0.0", ci) + " outside body;";
            if (spot.Punch > 1.01f)
                problems += " punch " + spot.Punch.ToString("0.000", ci) + ";";
            if (!tokenAfter)
                problems += " token serial " + FightTokenSerial + " after badger " + FightBadgerSerial + ";";
            if (tokenPixels == "under")
                problems += " token pixels under badger where they overlap;";
            if (meanN < 4)
                problems += " body mean samples " + meanN + ";";
            if (lumaN < 8)
                problems += " body luma samples " + lumaN + ";";
            problems += measureProblem;
            string F(float v) => v.ToString("0.00", ci);
            string lumaVs = "null";
            if (lumaN > 0 && prevLuma != null && prevHave != null && sizeIx >= 0 && sizeIx < prevLuma.Length)
            {
                if (prevHave[sizeIx]) lumaVs = F(luma - prevLuma[sizeIx]);
                prevLuma[sizeIx] = luma;
                prevHave[sizeIx] = true;
            }
            return "    {\n"
                + "      \"file\": \"" + file + "\",\n"
                + "      \"size\": \"" + reqW + "x" + reqH + "\",\n"
                + "      \"stage\": \"" + stage + "\",\n"
                + "      \"pose\": \"" + spot.Frame + "\",\n"
                + "      \"wear\": \"" + want + "\",\n"
                + "      \"screen\": [" + Screen.width + ", " + Screen.height + "],\n"
                + "      \"area_w\": " + PhoneArea.W + ",\n"
                + "      \"body_center_x\": " + F(screenCx) + ",\n"
                + "      \"body_center_x_area\": " + F(bodyCx) + ",\n"
                + "      \"left_margin\": " + F(left) + ",\n"
                + "      \"right_margin\": " + F(right) + ",\n"
                + "      \"content_width\": " + F(contentR - contentL) + ",\n"
                + "      \"foot_y\": " + F(spot.Foot.y) + ",\n"
                + "      \"boss_foot_y\": " + F(box.BossFighter.yMax) + ",\n"
                + "      \"foot_y_delta\": " + F(footDelta) + ",\n"
                + "      \"lean_deg\": " + F(spot.Degrees) + ",\n"
                + "      \"lean_shift_x\": " + F(lean) + ",\n"
                + "      \"eye_in_body\": " + (eyeIn ? "true" : "false") + ",\n"
                + "      \"tint_pct\": " + F(tintPct) + ",\n"
                + "      \"tint_hex\": \"" + tintHex + "\",\n"
                + "      \"halo_alpha\": " + F(haloA) + ",\n"
                + "      \"punch_scale\": " + spot.Punch.ToString("0.000", ci) + ",\n"
                + "      \"body_mean\": [" + F(meanR) + ", " + F(meanG) + ", " + F(meanB) + "],\n"
                + "      \"body_mean_n\": " + meanN + ",\n"
                + "      \"body_mean_vs_fresh\": [" + F(dR) + ", " + F(dG) + ", " + F(dB) + "],\n"
                + "      \"body_luma\": " + F(luma) + ",\n"
                + "      \"body_luma_n\": " + lumaN + ",\n"
                + "      \"body_luma_vs_prev\": " + lumaVs + ",\n"
                + "      \"token_after_badger\": " + (tokenAfter ? "true" : "false") + ",\n"
                + "      \"badger_paint\": " + FightBadgerSerial + ",\n"
                + "      \"token_paint\": " + FightTokenSerial + ",\n"
                + "      \"token_opaque_overlap\": " + opaqueOverlap + ",\n"
                + "      \"token_gold_px\": " + goldPx + ",\n"
                + "      \"token_pixel_on_top\": \"" + tokenPixels + "\",\n"
                + "      \"overlaps\": [\n"
                + "        {\"element\": \"bird\", \"overlap_px\": " + F(bird) + "},\n"
                + "        {\"element\": \"boss_comb\", \"overlap_px\": " + F(boss) + "},\n"
                + "        {\"element\": \"you_comb\", \"overlap_px\": " + F(you) + "},\n"
                + "        {\"element\": \"column_left\", \"overlap_px\": " + F(colL) + "},\n"
                + "        {\"element\": \"column_right\", \"overlap_px\": " + F(colR) + "}\n"
                + "      ],\n"
                + "      \"problems\": \"" + problems.Trim() + "\"\n"
                + "    }";
        }

        static string TintHex(Color c)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            return "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        static bool HoneyGold(Color32 p)
            => p.r > 170 && p.g > 110 && p.b < 150 && p.r > p.b + 25;

        // Undo GUIUtility.RotateAroundPivot so a screenshot point lands on the unrotated sprite.
        static void UnrotateGui(float gx, float gy, Vector2 pivot, float deg, out float ux, out float uy)
        {
            float rad = -deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float dx = gx - pivot.x;
            float dy = gy - pivot.y;
            ux = pivot.x + dx * c - dy * s;
            uy = pivot.y + dx * s + dy * c;
        }

        // Offline Rec.709 of one sprite after each stage tint. Alpha above 200. No halo.
        static string SamePoseSection()
        {
            var ci = CultureInfo.InvariantCulture;
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            string TintSprite(string file)
            {
                string path = Path.Combine(Application.dataPath, "_Project/Art/Resources/Sprites/Badger/" + file + ".png");
                Color32[] px = null;
                Texture2D tex = null;
                try
                {
                    if (File.Exists(path))
                    {
                        tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(File.ReadAllBytes(path))) px = tex.GetPixels32();
                    }
                }
                finally { if (tex != null) UnityEngine.Object.Destroy(tex); }
                var sb = new System.Text.StringBuilder();
                sb.Append("{\n      \"sprite\": \"").Append(file).Append("\",\n      \"stages\": [\n");
                float prev = 0f;
                bool have = false;
                for (int i = 0; i < wears.Length; i++)
                {
                    var tint = BadgerWearArt.BodyTint(wears[i]);
                    double sum = 0;
                    int n = 0;
                    if (px != null)
                    {
                        for (int p = 0; p < px.Length; p++)
                        {
                            if (px[p].a <= 200) continue;
                            float r = px[p].r * tint.r;
                            float g = px[p].g * tint.g;
                            float b = px[p].b * tint.b;
                            sum += 0.2126 * r + 0.7152 * g + 0.0722 * b;
                            n++;
                        }
                    }
                    float luma = n > 0 ? (float)(sum / n) : 0f;
                    string delta = have && n > 0 ? (luma - prev).ToString("0.00", ci) : "null";
                    if (n > 0) { prev = luma; have = true; }
                    if (i > 0) sb.Append(",\n");
                    sb.Append("        {\"stage\": \"").Append(BadgerWearArt.Token(wears[i]))
                        .Append("\", \"tint_pct\": ").Append((BadgerWearArt.TintAmount(wears[i]) * 100f).ToString("0.00", ci))
                        .Append(", \"tint_hex\": \"").Append(TintHex(tint))
                        .Append("\", \"luma\": ").Append(luma.ToString("0.00", ci))
                        .Append(", \"n\": ").Append(n)
                        .Append(", \"delta_prev\": ").Append(delta).Append("}");
                }
                sb.Append("\n      ]\n    }");
                return sb.ToString();
            }
            string OwnWear(string pose)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("{\n      \"pose\": \"").Append(pose).Append("\",\n      \"stages\": [\n");
                float prev = 0f;
                bool have = false;
                for (int i = 0; i < wears.Length; i++)
                {
                    string file = BadgerWearArt.StageKey(pose, wears[i]);
                    string path = Path.Combine(Application.dataPath, "_Project/Art/Resources/Sprites/Badger/" + file + ".png");
                    var tint = BadgerWearArt.BodyTint(wears[i]);
                    double sum = 0;
                    int n = 0;
                    Texture2D tex = null;
                    try
                    {
                        if (File.Exists(path))
                        {
                            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(File.ReadAllBytes(path)))
                            {
                                var px = tex.GetPixels32();
                                for (int p = 0; p < px.Length; p++)
                                {
                                    if (px[p].a <= 200) continue;
                                    float r = px[p].r * tint.r;
                                    float g = px[p].g * tint.g;
                                    float b = px[p].b * tint.b;
                                    sum += 0.2126 * r + 0.7152 * g + 0.0722 * b;
                                    n++;
                                }
                            }
                        }
                    }
                    finally { if (tex != null) UnityEngine.Object.Destroy(tex); }
                    float luma = n > 0 ? (float)(sum / n) : 0f;
                    string delta = have && n > 0 ? (luma - prev).ToString("0.00", ci) : "null";
                    if (n > 0) { prev = luma; have = true; }
                    if (i > 0) sb.Append(",\n");
                    sb.Append("        {\"stage\": \"").Append(BadgerWearArt.Token(wears[i]))
                        .Append("\", \"sprite\": \"").Append(file)
                        .Append("\", \"tint_pct\": ").Append((BadgerWearArt.TintAmount(wears[i]) * 100f).ToString("0.00", ci))
                        .Append(", \"luma\": ").Append(luma.ToString("0.00", ci))
                        .Append(", \"n\": ").Append(n)
                        .Append(", \"delta_prev\": ").Append(delta).Append("}");
                }
                sb.Append("\n      ]\n    }");
                return sb.ToString();
            }
            return "  \"same_pose\": {\n"
                + "    \"note\": \"Rec.709 mean on 0-255 of pixels with alpha above 200, multiplied by BadgerWearArt.BodyTint. No halo and no scene. tint_only is one fresh sprite at every strength. idle_b_wear is each standing wear PNG with its own tint, so the wrecked step is not the crouch.\",\n"
                + "    \"alpha_min\": 200,\n"
                + "    \"formula\": \"0.2126 R + 0.7152 G + 0.0722 B\",\n"
                + "    \"tint_only_idle_b\": " + TintSprite("badger_idle_b_wear_fresh") + ",\n"
                + "    \"tint_only_crouch\": " + TintSprite("badger_crouch_wear_fresh") + ",\n"
                + "    \"idle_b_wear\": " + OwnWear("badger_idle_b") + "\n"
                + "  }\n";
        }

        // Chest mean of opaque body pixels, full-body Rec.709 luma, and whether honey pixels sit on the body.
        void MeasureBodyAndToken(string path, BadgerActorSpot spot, BadgerWear want, Rect drawn, Rect wearDest,
            float bodyL, float bodyR, float bodyCx,
            out int meanN, out float meanR, out float meanG, out float meanB,
            out int opaqueOverlap, out int goldPx, out string tokenPixels, out int lumaN, out float luma)
        {
            meanN = 0;
            meanR = meanG = meanB = 0f;
            opaqueOverlap = 0;
            goldPx = 0;
            tokenPixels = "no_overlap";
            lumaN = 0;
            luma = 0f;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            string key = BadgerWearArt.StageKey(spot.Frame, want);
            string bodyPath = Path.Combine(Application.dataPath, "_Project/Art/Resources/Sprites/Badger/" + key + ".png");
            Texture2D shot = null;
            Texture2D body = null;
            try
            {
                shot = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!shot.LoadImage(File.ReadAllBytes(path))) return;
                var spx = shot.GetPixels32();
                int sw = shot.width;
                int sh = shot.height;
                Color32[] bpx = null;
                int bw = 0, bh = 0;
                if (File.Exists(bodyPath))
                {
                    body = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (body.LoadImage(File.ReadAllBytes(bodyPath)))
                    {
                        bpx = body.GetPixels32();
                        bw = body.width;
                        bh = body.height;
                    }
                }
                float sx = sw / Mathf.Max(1f, Screen.width);
                float sy = sh / Mathf.Max(1f, Screen.height);
                Color32 At(float gx, float gy)
                {
                    int x = Mathf.Clamp(Mathf.RoundToInt((gx + PhoneArea.OffX) * sx), 0, sw - 1);
                    int top = Mathf.Clamp(Mathf.RoundToInt(gy * sy), 0, sh - 1);
                    return spx[(sh - 1 - top) * sw + x];
                }
                bool Covered(float gx, float gy, int minA)
                {
                    if (bpx == null || wearDest.width < 2f || wearDest.height < 2f) return false;
                    UnrotateGui(gx, gy, spot.Foot, spot.Degrees, out float ux, out float uy);
                    if (ux < wearDest.x || uy < wearDest.y || ux >= wearDest.xMax || uy >= wearDest.yMax) return false;
                    float u = (ux - wearDest.x) / wearDest.width;
                    float v = (uy - wearDest.y) / wearDest.height;
                    int tx = Mathf.Clamp(Mathf.FloorToInt(u * bw), 0, bw - 1);
                    int top = Mathf.Clamp(Mathf.FloorToInt(v * bh), 0, bh - 1);
                    return bpx[(bh - 1 - top) * bw + tx].a > minA;
                }

                float chestY = Mathf.Lerp(drawn.y, spot.Foot.y, 0.42f);
                int sumR = 0, sumG = 0, sumB = 0;
                for (int dy = -4; dy <= 4; dy++)
                {
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        float gx = bodyCx + dx;
                        float gy = chestY + dy;
                        if (gx < bodyL || gx > bodyR) continue;
                        if (!Covered(gx, gy, 40)) continue;
                        var p = At(gx, gy);
                        if (p.g > p.r + 12) continue;
                        if (HoneyGold(p)) continue;
                        sumR += p.r;
                        sumG += p.g;
                        sumB += p.b;
                        meanN++;
                    }
                }
                if (meanN > 0)
                {
                    meanR = sumR / (float)meanN;
                    meanG = sumG / (float)meanN;
                    meanB = sumB / (float)meanN;
                }

                // One sample per screenshot pixel. GUI y matches At(): row 0 of GetPixels32 is the bottom.
                double sumY = 0;
                if (bpx != null && sw > 0 && sh > 0)
                {
                    for (int row = 0; row < sh; row++)
                    {
                        float gy = (sh - 1 - row) / sy;
                        int rowAt = row * sw;
                        for (int col = 0; col < sw; col++)
                        {
                            float gx = col / sx - PhoneArea.OffX;
                            if (!Covered(gx, gy, 200)) continue;
                            var p = spx[rowAt + col];
                            if (HoneyGold(p)) continue;
                            sumY += 0.2126 * p.r + 0.7152 * p.g + 0.0722 * p.b;
                            lumaN++;
                        }
                    }
                }
                if (lumaN > 0) luma = (float)(sumY / lumaN);

                if (_bgTokenLive && _bgTokenRect.width > 2f)
                {
                    int x0 = Mathf.FloorToInt(_bgTokenRect.xMin);
                    int y0 = Mathf.FloorToInt(_bgTokenRect.yMin);
                    int x1 = Mathf.CeilToInt(_bgTokenRect.xMax);
                    int y1 = Mathf.CeilToInt(_bgTokenRect.yMax);
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            if (!Covered(x + 0.5f, y + 0.5f, 40)) continue;
                            opaqueOverlap++;
                            if (HoneyGold(At(x + 0.5f, y + 0.5f))) goldPx++;
                        }
                    }
                }
                if (opaqueOverlap < 4) tokenPixels = "no_overlap";
                else if (goldPx >= 4) tokenPixels = "on_top";
                else tokenPixels = "under";
            }
            finally
            {
                if (shot != null) UnityEngine.Object.Destroy(shot);
                if (body != null) UnityEngine.Object.Destroy(body);
            }
        }
    }
}
#endif
