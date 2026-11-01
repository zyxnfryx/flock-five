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
        // b119 / PR 221 evidence: the repainted spring garden, a burning branch mid-strike (bolt, then flame tongues),
        // and the frozen-badger captions. Drop /tmp/flock-five-pr221 and enter play mode (Pr221Stills.Capture).
        // Frames go to /tmp/ff-pr221-stills. Player prefs it touches are put back.
        public IEnumerator ShotPr221()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            const string dir = "/tmp/ff-pr221-stills";
            Directory.CreateDirectory(dir);
            string log = dir + "/log.txt";
            File.WriteAllText(log, "");
            string[] keys = { "flockfive.next", CoachKey, CoachLeafKey, CoachHiveGardenKey, "flockfive.season.seen" };
            var had = new bool[keys.Length];
            var ints = new int[keys.Length];
            string seen = PlayerPrefs.GetString("flockfive.season.seen", "");
            for (int i = 0; i < keys.Length; i++) { had[i] = PlayerPrefs.HasKey(keys[i]); if (i < 4) ints[i] = PlayerPrefs.GetInt(keys[i], 0); }
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int bolt = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            try
            {
                LightningStrike.Enabled = false; // only our own strike
                if (EditorGameSize != null) EditorGameSize(1170, 2532);
                yield return new WaitForSecondsRealtime(0.8f);

                // 1) spring garden
                int level = GardenSeason.SpringFrom;
                SeasonReveal.Mark(level);
                PrepGardenShot(level - 1);
                HoldGardenQuiet();
                yield return new WaitForSecondsRealtime(2.5f);
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                File.AppendAllText(log, "spring scene " + GardenSeason.ForLevel(level) + "\n");
                SaveFrame(dir + "/01-spring-garden.png");

                // 2) burning branch mid-strike, on the same garden: bolt first, ignite IgniteDelay later.
                if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
                yield return new WaitForSecondsRealtime(1.2f);
                int idx = LightningStrike.Target(_board, _bonusOn, _offerBurned);
                var view = idx >= 0 ? ViewFor(idx) : null;
                if (view == null && _garden.Branches != null)
                    foreach (var v in _garden.Branches) if (v != null && v.Wood != null) { view = v; break; }
                File.AppendAllText(log, "strike target " + idx + " view " + (view != null) + "\n");
                if (view != null)
                {
                    var aim = LightningStrike.AimPoint(view.transform.position, view.Wood != null, view.Wood != null ? view.Wood.bounds : default);
                    GardenStorm.StrikeAt(aim);
                    StartCoroutine(IgniteAfterBolt(view, _garden.Root));
                    float t0 = AdPause.Unscaled;
                    float[] at = { 0.06f, 0.3f, 0.7f, 1.1f };
                    string[] nm = { "02-strike-bolt", "03-strike-bolt-fire", "04-fire-tongues", "05-fire-late" };
                    for (int k = 0; k < at.Length; k++)
                    {
                        while (AdPause.Unscaled - t0 < at[k]) yield return null;
                        yield return new WaitForEndOfFrame();
                        SaveFrame(dir + "/" + nm[k] + ".png");
                        File.AppendAllText(log, nm[k] + " t=" + (AdPause.Unscaled - t0).ToString("0.00") + "\n");
                    }
                }
                yield return new WaitForSecondsRealtime(2.5f);

                // 3) frozen badger captions on a repeat fight
                bool was = BadgerSchedule.Enabled;
                BadgerSchedule.Enabled = true;
                OpenBadgerFight(20);
                BadgerSchedule.Enabled = was;
                float w = 0f;
                while (w < 30f && !(_bgStage == BadgerStage.YourPick && _bgLine == BadgerCopy.YourPick && _bgT > 1.2f))
                { w += AdPause.UnscaledDelta; yield return null; }
                if (_bgFight != null)
                {
                    var ty = typeof(BadgerFight);
                    var fl = BindingFlags.Instance | BindingFlags.NonPublic;
                    ty.GetField("_bossSkipLeft", fl)?.SetValue(_bgFight, 1);
                    ty.GetField("_freezeRounds", fl)?.SetValue(_bgFight, 1);
                }
                string[] lines = { BadgerCopy.FrozenSolid, BadgerCopy.BreakFree };
                string[] files = { "06-frozen-caption", "07-thawing-caption" };
                for (int k = 0; k < lines.Length; k++)
                {
                    _bgLine = lines[k];
                    yield return new WaitForSecondsRealtime(0.8f);
                    _bgLine = lines[k];
                    yield return new WaitForEndOfFrame();
                    File.AppendAllText(log, files[k] + " stage " + _bgStage + " line " + (_bgLine ?? "").Replace("\n", "/") + " frozen " + (_bgFight != null && _bgFight.BossFrozenNow) + "\n");
                    SaveFrame(dir + "/" + files[k] + ".png");
                }
                File.WriteAllText(dir + "/done", "OK\n");
            }
            finally
            {
                if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, bolt); else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
                for (int i = 0; i < 4; i++) { if (had[i]) PlayerPrefs.SetInt(keys[i], ints[i]); else PlayerPrefs.DeleteKey(keys[i]); }
                if (had[4]) PlayerPrefs.SetString(keys[4], seen); else PlayerPrefs.DeleteKey(keys[4]);
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif
