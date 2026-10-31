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
            json.Append("{\n  \"note\": \"Body centre, margins, foot and overlaps are the live duel layout on the captured frame (BadgerActorNow + SpriteAnim + BadgerWearArt.PlacedEdges). Screen X adds the pillar offset. Foot Y is the planted opaque foot. Boss foot Y is the boss-box bottom. Overlap px is the horizontal overlap when the rects also overlap vertically.\",\n  \"stills\": [\n");
            int[][] res = { new[] { 402, 874 }, new[] { 820, 1180 }, new[] { 412, 915 } };
            string[] stages = { "fresh", "scuffed", "bruised", "wrecked" };
            bool wasOn = BadgerSchedule.Enabled;
            BadgerSchedule.Enabled = true;
            bool first = true;
            try
            {
                File.AppendAllText(log, "START\n");
                for (int d = 0; d < res.Length; d++)
                {
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
                        for (float w = 0f; w < 0.4f; w += Kick()) yield return null;
                        yield return new WaitForEndOfFrame();
                        string name = res[d][0] + "x" + res[d][1] + "-fight-" + stages[i] + ".png";
                        string path = Path.Combine(stillDir, name);
                        SaveFrame(path);
                        string entry = FightMeasureEntry(name, stages[i], want, res[d][0], res[d][1]);
                        if (!first) json.Append(",\n");
                        first = false;
                        json.Append(entry);
                        File.AppendAllText(log, name + " screen " + Screen.width + "x" + Screen.height
                            + " area " + PhoneArea.W + "x" + PhoneArea.H + " want " + want
                            + " score " + scores[i] + "/" + target + "\n" + entry + "\n");
                    }
                    LeaveBadger();
                }
                json.Append("\n  ]\n}\n");
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

        string FightMeasureEntry(string file, string stage, BadgerWear want, int reqW, int reqH)
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
            string F(float v) => v.ToString("0.00", ci);
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
    }
}
#endif
