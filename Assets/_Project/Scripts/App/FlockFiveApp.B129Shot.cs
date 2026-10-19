#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b129 evidence (editor only). Drop /tmp/flock-five-b129-badger and enter play mode (Unity without -batchmode).
        // Frozen before/after at 390x844, battle size 1.00 / 1.15 / 1.30 at 390x844 (+ iPad at 1.30) with a rect
        // overlap report, and the garden-to-fight leap frame by frame at 60 fps, old vs new.
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
            foreach (var sub in new[] { "frozen", "size", "transition", "freeze-thaw" }) Directory.CreateDirectory(dir + "/" + sub);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            var ci = CultureInfo.InvariantCulture;
            bool wasOn = BadgerSchedule.Enabled;
            BadgerSchedule.Enabled = true;
            try
            {
                File.AppendAllText(log, "START\n");
                // ---------- 1) frozen ----------
                if (EditorGameSize != null) EditorGameSize(1170, 2532);
                yield return new WaitForSecondsRealtime(1.2f);
                yield return OpenRepeatFight(log);
                SetFrozen(true);
                foreach (bool legacy in new[] { true, false })
                {
                    Badger129.LegacyFrozen = legacy;
                    for (float w = 0f; w < 0.9f; w += Kick()) { _bgLine = BadgerCopy.FreezeSprayFrozen; yield return null; }
                    yield return new WaitForEndOfFrame();
                    string tag = legacy ? "before" : "after";
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(dir + "/frozen/frozen-" + tag + "-390x844.png", tex.EncodeToPNG());
                    var comb = _bgBossSlotNow;
                    WriteCrop(tex, comb, 768, 0.10f, dir + "/frozen/frozen-comb-" + tag + "-zoom.png");
                    var spot = BadgerActorNow(BadgerLayout());
                    var fighter = new Rect(spot.Foot.x - Screen.width * 0.16f, spot.Foot.y - Screen.height * 0.17f, Screen.width * 0.32f, Screen.height * 0.19f);
                    WriteCrop(tex, fighter, 768, 0.05f, dir + "/frozen/frozen-fighter-" + tag + "-zoom.png");
                    Object.Destroy(tex);
                    File.AppendAllText(log, "FROZEN " + tag + " frozenNow " + (_bgFight != null && _bgFight.BossFrozenNow) + " comb " + R(comb) + " line " + (_bgLine ?? "").Replace("\n", " / ") + " screen " + Screen.width + "x" + Screen.height + "\n");
                }
                Badger129.LegacyFrozen = false;
                SetFrozen(false);

                // ---------- 2) size ----------
                float[] scales = { 1f, 1.15f, 1.30f };
                string[] stag = { "100", "115", "130" };
                for (int i = 0; i < scales.Length; i++)
                {
                    Badger129.BattleScale = scales[i];
                    for (float w = 0f; w < 0.7f; w += Kick()) { _bgLine = BadgerCopy.YourPick; yield return null; }
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/size/size-" + stag[i] + "-390x844.png");
                    File.AppendAllText(log, Overlap("SIZE " + stag[i] + " 390x844"));
                }
                if (EditorGameSize != null) EditorGameSize(2048, 2732);
                yield return new WaitForSecondsRealtime(1.2f);
                for (int i = 0; i < scales.Length; i++)
                {
                    Badger129.BattleScale = scales[i];
                    for (float w = 0f; w < 0.7f; w += Kick()) { _bgLine = BadgerCopy.YourPick; yield return null; }
                    yield return new WaitForEndOfFrame();
                    if (i == 2) SaveFrame(dir + "/size/size-130-ipad-1024x1366.png");
                    File.AppendAllText(log, Overlap("SIZE " + stag[i] + " ipad"));
                }
                Badger129.BattleScale = Badger129.BattleScaleDefault;

                // ---------- 3) transition ----------
                if (EditorGameSize != null) EditorGameSize(1170, 2532);
                yield return new WaitForSecondsRealtime(1.2f);
                foreach (bool legacy in new[] { true, false })
                {
                    Badger129.LegacyLeap = legacy;
                    string tag = legacy ? "before" : "after";
                    string fdir = dir + "/transition/" + tag + "-frames";
                    Directory.CreateDirectory(fdir);
                    if (_home == HomeFace.Badger) LeaveBadger();
                    yield return new WaitForSecondsRealtime(0.4f);
                    PrepGardenShot(3);
                    HoldGardenQuiet();
                    for (float w = 0f; w < 1.5f; w += Kick()) { HoldGardenQuiet(); MuteShotAudio(); Kick(); yield return null; }
                    var sb = new StringBuilder();
                    sb.Append("frame,t_s,leap_live,leap_T,beat,wash,home,stage,washOut,art,dest_x,dest_y,dest_w,dest_h\n");
                    Time.captureDeltaTime = 1f / 60f;
                    StartCoroutine(B129ClearToFight());
                    for (int f = 0; f < 240; f++)
                    {
                        ShotBadgerDest = default; ShotBadgerArt = ""; Kick();
                        yield return new WaitForEndOfFrame();
                        MuteShotAudio();
                        var tex = ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(fdir + "/f" + f.ToString("000") + ".jpg", tex.EncodeToJPG(82));
                        Object.Destroy(tex);
                        float lt = _bgLeap.T;
                        sb.Append(f).Append(',').Append((f / 60f).ToString("0.000", ci)).Append(',').Append(_bgLeap.Live ? 1 : 0).Append(',')
                          .Append(lt.ToString("0.000", ci)).Append(',').Append(_bgLeap.Live ? BadgerLeap.BeatAt(lt).ToString() : "-").Append(',')
                          .Append(_bgLeap.Live ? BadgerLeap.WashAlpha(lt).ToString("0.00", ci) : "-").Append(',')
                          .Append(_home).Append(',').Append(_bgStage).Append(',').Append(_bgWashOut.ToString("0.000", ci)).Append(',')
                          .Append(ShotBadgerArt).Append(',').Append(ShotBadgerDest.x.ToString("0.0", ci)).Append(',').Append(ShotBadgerDest.y.ToString("0.0", ci)).Append(',')
                          .Append(ShotBadgerDest.width.ToString("0.0", ci)).Append(',').Append(ShotBadgerDest.height.ToString("0.0", ci)).Append('\n');
                        yield return null;
                    }
                    Time.captureDeltaTime = 0f;
                    File.WriteAllText(dir + "/transition/" + tag + "-frames.csv", sb.ToString());
                    File.AppendAllText(log, "TRANSITION " + tag + " frames 240\n");
                    yield return new WaitForSecondsRealtime(1.0f);
                }
                Badger129.LegacyLeap = false;

                // ---------- 4) freeze and thaw at 390x844 ----------
                File.AppendAllText(log, "FREEZE_THAW start\n");
                yield return OpenRepeatFight(log);
                SetFrozen(false);
                for (float w = 0f; w < 0.6f; w += Kick()) yield return null;
                string ft = dir + "/freeze-thaw";
                Directory.CreateDirectory(ft + "/freeze-frames");
                Directory.CreateDirectory(ft + "/thaw-frames");
                var fsb = new StringBuilder("phase,frame,freeze_age,thaw_age,frozen,line\n");
                Time.captureDeltaTime = 1f / 60f;
                SetFrozen(true);
                for (int f = 0; f < 42; f++)
                {
                    Kick();
                    yield return new WaitForEndOfFrame();
                    SaveFrameJpg(ft + "/freeze-frames/f" + f.ToString("000") + ".jpg");
                    fsb.Append("freeze,").Append(f).Append(',').Append(BadgerIceAnim.FreezeAge(PlayClock.Now).ToString("0.000", ci)).Append(",-,")
                       .Append(_bgFight != null && _bgFight.BossFrozenNow).Append(',').Append((_bgLine ?? "").Replace("\n", " / ")).Append('\n');
                    yield return null;
                }
                for (int f = 0; f < 20; f++) { Kick(); yield return null; }
                BadgerIceAnim.Thaw(PlayClock.Now);
                for (int f = 0; f < 84; f++)
                {
                    Kick();
                    float b = BadgerIceAnim.ThawAge(PlayClock.Now);
                    if (b >= BadgerIceAnim.WobbleSeconds && _bgFight != null && _bgFight.BossFrozenNow) SetFrozen(false);
                    yield return new WaitForEndOfFrame();
                    SaveFrameJpg(ft + "/thaw-frames/f" + f.ToString("000") + ".jpg");
                    fsb.Append("thaw,").Append(f).Append(",-,").Append(b.ToString("0.000", ci)).Append(',')
                       .Append(_bgFight != null && _bgFight.BossFrozenNow).Append(',').Append((_bgLine ?? "").Replace("\n", " / ")).Append('\n');
                    yield return null;
                }
                Time.captureDeltaTime = 0f;
                File.WriteAllText(ft + "/freeze-thaw.csv", fsb.ToString());
                File.AppendAllText(log, "FREEZE_THAW done\n");
                File.WriteAllText(dir + "/done", "OK\n");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                BadgerSchedule.Enabled = wasOn;
                Badger129.LegacyFrozen = false;
                Badger129.LegacyLeap = false;
                Badger129.BattleScale = Badger129.BattleScaleDefault;
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        // Same order as the garden clear: leap over the live garden, then the splash and the fight.
        IEnumerator B129ClearToFight()
        {
            yield return BadgerClearLeap();
            ShowSplash();
            if (!OpenBadgerFight(20)) _bgLeap.Release();
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

        static void SaveFrameJpg(string path)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return;
            File.WriteAllBytes(path, tex.EncodeToJPG(82));
            Object.Destroy(tex);
        }

        static string R(Rect r) => string.Format(CultureInfo.InvariantCulture, "({0:0},{1:0},{2:0},{3:0})", r.x, r.y, r.width, r.height);

        // The badger's drawn sprite rect against every HUD / comb / power / caption rect on the page.
        string Overlap(string head)
        {
            var L = BadgerLayout();
            var spot = BadgerActorNow(L);
            var spr = SpriteCatalog.BadgerArt(spot.Frame);
            var art = spr != null ? SpriteAnim.Dest(spot.Frame, spr.rect.width, spr.rect.height, spot.Foot, spot.Unit, spot.Sx, spot.Sy) : default;
            var boxes = BadgerArenaRects.Split(L.Arena);
            var names = new System.Collections.Generic.List<string>();
            var rects = new System.Collections.Generic.List<Rect>();
            void Add(string n, Rect r) { names.Add(n); rects.Add(r); }
            Add("hud.back", L.Back); Add("hud.title", L.Title); Add("hud.need", L.Need); Add("hud.coin", L.Coin);
            Add("score.meterL", L.ColL); Add("score.meterR", L.ColR);
            Add("score.bossGrid", L.BossGrid); Add("score.playerGrid", L.PlayerGrid);
            // v2: slot tiles straight from this frame's live layout (the cached painted rect went stale across resizes)
            Add("score.bossSlot", BadgerSlotTile(boxes.BossSlot));
            Add("score.youSlot", BadgerSlotTile(boxes.YouSlot));
            Add("power.row", L.Power);
            for (int k = 0; k < 4; k++) Add("power." + k, BadgerPowerRect(L, k));
            Add("caption.band", L.Caption);
            var sb = new StringBuilder();
            sb.Append(head).Append(" scale ").Append(Badger129.BattleScale.ToString("0.00", CultureInfo.InvariantCulture))
              .Append(" screen ").Append(Screen.width).Append('x').Append(Screen.height)
              .Append(" bossSlotLive ").Append(R(BadgerSlotTile(boxes.BossSlot))).Append(" bossSlotPainted ").Append(R(_bgBossSlotNow))
              .Append(" frame ").Append(spot.Frame).Append(" art ").Append(R(art)).Append(" arena ").Append(R(L.Arena)).Append('\n');
            for (int i = 0; i < rects.Count; i++)
            {
                var o = Rect.MinMaxRect(Mathf.Max(art.xMin, rects[i].xMin), Mathf.Max(art.yMin, rects[i].yMin), Mathf.Min(art.xMax, rects[i].xMax), Mathf.Min(art.yMax, rects[i].yMax));
                bool hit = o.width > 0.5f && o.height > 0.5f;
                sb.Append("  ").Append(hit ? "HIT  " : "clear").Append(' ').Append(names[i]).Append(' ').Append(R(rects[i]));
                if (hit) sb.Append(" overlap ").Append(R(o));
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
#endif
