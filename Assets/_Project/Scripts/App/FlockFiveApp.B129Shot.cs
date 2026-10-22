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
    }
}
#endif
