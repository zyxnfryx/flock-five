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
        // b128 evidence: a repeat badger fight at 'Your pick.' (caption + power row), then the same beat with the badger
        // iced (frozen Hot Sauce / Freeze buttons), at 375x667, 390x844 and 430x932 (device pixels). Drop
        // /tmp/flock-five-badger-power (its text = the output folder) and enter play mode.
        public IEnumerator ShotBadgerPower()
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
            string dir = "/tmp/ff-badger-power-shots";
            try { var t = File.ReadAllText("/tmp/flock-five-badger-power-dir").Trim(); if (t.Length > 0) dir = t; } catch { }
            Directory.CreateDirectory(dir);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            int[][] res = { new[] { 750, 1334 }, new[] { 1170, 2532 }, new[] { 1290, 2796 } };
            string[] tag = { "375x667", "390x844", "430x932" };
            try
            {
                for (int d = 0; d < res.Length; d++)
                {
                    if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]);
                    yield return new WaitForSecondsRealtime(1.0f);
                    if (_home == HomeFace.Badger) LeaveBadger();
                    yield return new WaitForSecondsRealtime(0.5f);
                    bool was = BadgerSchedule.Enabled;
                    BadgerSchedule.Enabled = true;
                    OpenBadgerFight(20); // appearance 2: a repeat fight, no lesson
                    BadgerSchedule.Enabled = was;
                    float w = 0f;
                    while (w < 30f && !(_bgStage == BadgerStage.YourPick && _bgLine == BadgerCopy.YourPick && _bgT > 1.2f))
                    { w += AdPause.UnscaledDelta; yield return null; }
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return new WaitForEndOfFrame();
                    File.AppendAllText(log, tag[d] + " " + Screen.width + "x" + Screen.height + " stage " + _bgStage + " line " + (_bgLine ?? "").Replace("\n", "/") + " waited " + w.ToString("0.0") + "\n");
                    SaveFrame(dir + "/badger-pick-" + tag[d] + ".png");

                    // Frozen beat: the badger iced (as after a Freeze Spray), same 'Your pick.' turn.
                    if (_bgFight != null)
                    {
                        var ty = typeof(BadgerFight);
                        var fl = BindingFlags.Instance | BindingFlags.NonPublic;
                        ty.GetField("_bossSkipLeft", fl)?.SetValue(_bgFight, 1);
                        ty.GetField("_freezeRounds", fl)?.SetValue(_bgFight, 1);
                        yield return new WaitForSecondsRealtime(0.8f);
                        yield return new WaitForEndOfFrame();
                        File.AppendAllText(log, tag[d] + " frozen " + _bgFight.BossFrozenNow + "\n");
                        SaveFrame(dir + "/badger-frozen-" + tag[d] + ".png");
                    }
                }
                File.WriteAllText(dir + "/done", "OK\n");
            }
            finally
            {
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif
