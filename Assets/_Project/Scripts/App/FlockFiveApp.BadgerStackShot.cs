#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b130 badger-stack evidence (editor only, scratch worktree). Drop /tmp/ff-badger-stack-shot (text = output dir)
        // and enter play mode without -batchmode. Per device: opening step, 'x2 and x3' step, scoring-column step,
        // normal power row, frozen power row (+ ice-block badger), win banner.
        public IEnumerator ShotBadgerStack()
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
            string dir = "/tmp/ff-badger-stack-stills";
            try { var t = File.ReadAllText("/tmp/ff-badger-stack-dir").Trim(); if (t.Length > 0) dir = t; } catch { }
            Directory.CreateDirectory(dir);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            int[][] res = { new[] { 1170, 2532 }, new[] { 750, 1334 }, new[] { 1290, 2796 }, new[] { 2048, 2732 } };
            string[] tag = { "390x844", "375x667", "430x932", "ipad1024x1366" };
            bool wasOn = BadgerSchedule.Enabled;
            BadgerSchedule.Enabled = true;
            try
            {
                for (int d = 0; d < res.Length; d++)
                {
                    if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]);
                    yield return new WaitForSecondsRealtime(1.2f);
                    string T = tag[d];

                    // Lesson steps 0-2 on a fresh fight (lesson forced on; the coach stamp is not touched).
                    yield return OpenRepeatFight(log);
                    BeginBadgerLesson();
                    string[] names = { "1-opening", "2-x2x3-boost", "3-scoring-columns" };
                    for (int step = 0; step < 3; step++)
                    {
                        _bgLessonStep = step;
                        _bgLessonStepAt = AdPause.Unscaled;
                        RefreshBadgerLessonAim();
                        for (float w = 0f; w < 1.0f; w += Kick()) { _coachFade = 1f; yield return null; }
                        yield return new WaitForEndOfFrame();
                        SaveFrame(dir + "/" + names[step] + "-" + T + ".png");
                        File.AppendAllText(log, T + " " + names[step] + " stage " + _bgStage + " step " + _bgLessonStep + " screen " + PhoneArea.W + "x" + PhoneArea.H + " " + BadgerSpotLine() + "\n");
                    }
                    _bgLessonLive = false;

                    // Normal and frozen power rows on a repeat fight.
                    yield return OpenRepeatFight(log);
                    foreach (bool frozen in new[] { false, true })
                    {
                        SetFrozen(frozen);
                        for (float w = 0f; w < 1.2f; w += Kick()) { _bgLine = frozen ? BadgerCopy.FreezeSprayFrozen : BadgerCopy.YourPick; yield return null; }
                        yield return new WaitForEndOfFrame();
                        string nm = frozen ? "5-power-row-frozen" : "4-power-row-normal";
                        SaveFrame(dir + "/" + nm + "-" + T + ".png");
                        File.AppendAllText(log, T + " " + nm + " frozen " + (_bgFight != null && _bgFight.BossFrozenNow) + "\n");
                    }
                    SetFrozen(false);

                    // Win banner.
                    if (_bgFight != null)
                    {
                        typeof(BadgerFight).GetField("<Result>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_bgFight, BadgerResult.PlayerWon);
                        ShowBadgerResult();
                        for (float w = 0f; w < 2.0f; w += Kick()) yield return null;
                        yield return new WaitForEndOfFrame();
                        SaveFrame(dir + "/6-win-" + T + ".png");
                        File.AppendAllText(log, T + " 6-win stage " + _bgStage + " coins " + _bgWinCoins + " line " + (_bgLine ?? "").Replace("\n", "/") + "\n");
                    }
                    LeaveBadger();
                }
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

        string BadgerSpotLine()
        {
            var L = BadgerLayout();
            var sp = BadgerActorNow(L);
            var box = BadgerArenaRects.Split(L.Arena);
            return "arena " + L.Arena + " you " + box.YouFighter + " mid " + box.Middle.center.x + " bird " + sp.Bird + " icon " + sp.Icon + " birdBox " + sp.BirdBox + " foot " + sp.Foot + " colL " + L.ColL + " colR " + L.ColR + " frame " + sp.Frame;
        }
    }
}
#endif
