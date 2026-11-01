#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b119 Five of a Kind board stills (editor only). Drop /tmp/flock-five-b119-fives (text = output dir)
        // and enter play mode without -batchmode: FlockFive.Editor.Build119FivesStills.Capture.
        // Per device: entry (chip left of PAY TABLE) + open board, on a sample record. Prefs restored after.
        public IEnumerator ShotFives()
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
            string dir = "/tmp/ff-b119-fives";
            try { var t = File.ReadAllText("/tmp/flock-five-b119-fives").Trim(); if (t.Length > 0) dir = t; File.Delete("/tmp/flock-five-b119-fives"); } catch { }
            Directory.CreateDirectory(dir);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int poker = PlayerPrefs.GetInt(CoachPokerKey, 0);
            int welcome = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            bool hadF = PlayerPrefs.HasKey(BirdPoker.PrefFives);
            string oldF = PlayerPrefs.GetString(BirdPoker.PrefFives, "");
            int oldFx = PlayerPrefs.GetInt(BirdPoker.PrefFives + ".x", 0);
            int[][] res = { new[] { 1206, 2622 }, new[] { 1640, 2360 }, new[] { 1080, 2340 } };
            string[] tag = { "iphone18pro-1206x2622", "ipad-a16-1640x2360", "samsung-s25-1080x2340" };
            try
            {
                PlayerPrefs.SetInt("flockfive.next", 2);
                PlayerPrefs.SetInt(CoachPokerKey, 1);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                var rec = new BirdPoker.FiveRecord[BirdPoker.PunchKinds];
                int today = BirdPoker.TodayDay();
                void Hit(BirdColor c, BirdSex x, int n, int ago, bool nat)
                {
                    int k = BirdPoker.KindId(c, x);
                    rec[k] = new BirdPoker.FiveRecord { Count = n, FirstDay = ago < 0 ? BirdPoker.FiveDayEarlier : today - ago, Natural = nat };
                }
                Hit(BirdColor.Ruby, BirdSex.Neutral, 3, 17, false);
                Hit(BirdColor.Gold, BirdSex.Neutral, 2, 11, false);
                Hit(BirdColor.Gold, BirdSex.Female, 1, -1, false);
                Hit(BirdColor.Teal, BirdSex.Male, 1, 2, false);
                Hit(BirdColor.Violet, BirdSex.Female, 5, 20, true);
                Hit(BirdColor.Peach, BirdSex.Neutral, 1, 1, false);
                BirdPoker.SetFivesForShot(rec);
                for (int d = 0; d < res.Length; d++)
                {
                    if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]);
                    Screen.SetResolution(res[d][0], res[d][1], false);
                    for (float w = 0f; w < 0.8f; w += Kick()) yield return null;
                    MuteShotAudio();
                    ShowSplash();
                    _dailyIntro = false; _dailyIntroLive = false;
                    _hiveIntro = false; _hiveIntroLive = false;
                    _adoptLive = false; _welcomeOpen = false; _welcomeQueued = false; _welcomeGlove = false;
                    _dailyOpen = false; _dailyAskOpen = false;
                    float t0 = Time.realtimeSinceStartup;
                    int calm = 0;
                    for (float w = 0f; calm < 8 && w < 20f; w += Kick())
                    {
                        calm = WarmCoverLive ? 0 : calm + 1;
                        MuteShotAudio();
                        yield return null;
                    }
                    _home = HomeFace.Poker; _splash = true;
                    _fivesOpen = false; _fivesAnim = 0f;
                    for (float w = 0f; w < 1.4f; w += Kick()) yield return null;
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/entry-" + tag[d] + ".png");
                    File.AppendAllText(log, tag[d] + " entry screen " + PhoneArea.W + "x" + PhoneArea.H + " phase " + BirdPoker.PhaseNow + " chipLive " + FivesChipLive() + " pay " + _fivesPayLast + " icon " + _fivesIconLast + " row " + _pokerRowLast + " safe " + PhoneArea.Safe + " payRightInset " + (PhoneArea.W - _fivesPayLast.xMax) + " iconRightInset " + (PhoneArea.W - _fivesIconLast.xMax) + " payTopGap " + (_fivesPayLast.y - PhoneArea.Safe.yMin) + " payToIconGap " + (_fivesIconLast.y - _fivesPayLast.yMax) + " iconToRowGap " + (_pokerRowLast.y - _fivesIconLast.yMax) + "\n");
                    _fivesOpen = true;
                    for (float w = 0f; w < 0.9f; w += Kick()) yield return null;
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/board-" + tag[d] + ".png");
                    File.AppendAllText(log, tag[d] + " board found " + BirdPoker.FivesFound() + " hits " + BirdPoker.FivesHits() + " modal " + OpenModal() + "\n");
                    _fivesOpen = false; _fivesAnim = 0f;
                }
                File.WriteAllText(dir + "/done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachPokerKey, poker);
                PlayerPrefs.SetInt(WelcomeBonusKey, welcome);
                if (hadF) { PlayerPrefs.SetString(BirdPoker.PrefFives, oldF); PlayerPrefs.SetInt(BirdPoker.PrefFives + ".x", oldFx); }
                else { PlayerPrefs.DeleteKey(BirdPoker.PrefFives); PlayerPrefs.DeleteKey(BirdPoker.PrefFives + ".x"); }
                PlayerPrefs.Save();
                BirdPoker.ReloadFivesForTests();
                ShotKeepAwake = false;
                EditorShotLive = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif
