#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    // Bird Care v1 review stills on iPhone (1170x2532): tutorial glove, feeding moment, fed,
    // hungry and nap. Touch /tmp/flock-five-bird-care with the editor in play. Every pref it
    // touches is put back afterwards.
    public sealed partial class FlockFiveApp
    {
        const string CareShotDir = "/Users/zfxgames/wkspaces/bird-care-out";

        static readonly string[] CareShotKeys =
        {
            "flockfive.next", AvatarPref, AvatarNamePref, AvatarAdoptedPref, AvatarNamedPref, AvatarKitPref,
            CareHungerPref, CareLastPref, CareFirstPref, CareFeedAtPref, CareAdTakenPref, CareNectarPref, CareTutorPref, CareBornPref,
            CareNotifyYesPref, CareParentOkPref, CareIgnoredPref,
        };

        void CareShotQuiet()
        {
            _hiveIntro = false; _hiveIntroLive = false;
            _pokerIntro = false; _pokerIntroLive = false;
            _dailyIntro = false; _dailyIntroLive = false;
            _dailyOpen = false; _dailyAskOpen = false; _welcomeOpen = false; _welcomeGlove = false;
            _streakSlide = -1f; _adHand = false; _settingsOpen = false; _adoptLive = false; _adoptStep = AdoptStep.Off;
            _avatarRename = false;
            MuteShotAudio();
        }

        IEnumerator CareShotSettle(float secs)
        {
            float t = 0f;
            while (t < secs) { CareShotQuiet(); t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForEndOfFrame();
        }

        void CareShotState(double hunger, int nectar, bool firstUsed)
        {
            LoadCare();
            _care.Hunger = hunger;
            _care.LastUnix = CareNow();
            _care.FirstFeedUsed = firstUsed;
            _careNectar = nectar;
            _careFeedT = -1f;
            SaveCare();
        }

        public IEnumerator ShotBirdCare()
        {
            ShotKeepAwake = true;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            var keep = new Dictionary<string, string>();
            var kind = new Dictionary<string, int>();
            foreach (var k in CareShotKeys)
            {
                if (!PlayerPrefs.HasKey(k)) continue;
                // Type unknown: record all three readings, restore the one that round-trips.
                string sv = PlayerPrefs.GetString(k, "\u0001");
                if (sv != "\u0001") { keep[k] = sv; kind[k] = 0; continue; }
                int iv = PlayerPrefs.GetInt(k, int.MinValue);
                if (iv != int.MinValue) { keep[k] = iv.ToString(); kind[k] = 1; continue; }
                keep[k] = PlayerPrefs.GetFloat(k, 0f).ToString("R"); kind[k] = 2;
            }
            var log = new System.Text.StringBuilder();
            try
            {
                Directory.CreateDirectory(CareShotDir);
                BirdCareFlag.EditorForce = true;
                PlayerPrefs.SetInt("flockfive.next", 3);
                PlayerPrefs.SetInt(AvatarPref, (int)BirdColor.Gold);
                PlayerPrefs.SetString(AvatarNamePref, "Goldie");
                PlayerPrefs.SetInt(AvatarAdoptedPref, 1);
                PlayerPrefs.SetInt(AvatarNamedPref, 1);
                PlayerPrefs.SetInt(CareTutorPref, 0);
                PlayerPrefs.Save();
                if (EditorGameSize != null) EditorGameSize(1170, 2532);
                yield return new WaitForSecondsRealtime(1.0f);
                ShowSplash();
                _careLoaded = false;
                yield return CareShotSettle(1.5f);

                // 1. Tutorial: glove on Feed (first feed is free).
                CareShotState(20.0, 0, false);
                PlayerPrefs.SetInt(CareTutorPref, 0);
                _careTutorLive = false;
                _careTutorT = 0f;
                yield return CareShotSettle(2.2f);
                log.Append("tutorial live=").Append(_careTutorLive).Append(" step=").Append(LiveCoachStep())
                   .Append(" feed=").Append(_careFeedR).Append(" glovePivot=").Append(_careGlovePivot).Append(" mirror=").Append(_careGloveMirror).Append('\n');
                SaveFrame(CareShotDir + "/bird-care-tutorial-glove.png");
                yield return null;

                // 2. Feeding moment (the tutorial's tap).
                CareFeed();
                yield return CareShotSettle(0.75f);
                log.Append("feed tutorDone=").Append(PlayerPrefs.GetInt(CareTutorPref, 0)).Append(" hunger=").Append(_care.Hunger.ToString("0")).Append('\n');
                SaveFrame(CareShotDir + "/bird-care-feeding-moment.png");
                yield return CareShotSettle(2.2f);

                // 3. Fed.
                CareShotState(92.0, 4, true);
                yield return CareShotSettle(1.2f);
                log.Append("fed mood=").Append(CareMood()).Append('\n');
                SaveFrame(CareShotDir + "/bird-care-fed.png");

                // 4. Hungry ("snack time?").
                CareShotState(18.0, 3, true);
                yield return CareShotSettle(1.2f);
                log.Append("hungry mood=").Append(CareMood()).Append('\n');
                SaveFrame(CareShotDir + "/bird-care-hungry.png");

                // 5. Nap (low state; no nectar yet -> "Play for nectar").
                CareShotState(3.0, 0, true);
                yield return CareShotSettle(1.6f);
                log.Append("nap mood=").Append(CareMood()).Append('\n');
                SaveFrame(CareShotDir + "/bird-care-nap.png");
                yield return null;
            }
            finally
            {
                foreach (var k in CareShotKeys)
                {
                    if (!keep.ContainsKey(k)) { PlayerPrefs.DeleteKey(k); continue; }
                    if (kind[k] == 0) PlayerPrefs.SetString(k, keep[k]);
                    else if (kind[k] == 1) PlayerPrefs.SetInt(k, int.Parse(keep[k]));
                    else PlayerPrefs.SetFloat(k, float.Parse(keep[k]));
                }
                PlayerPrefs.Save();
                BirdCareFlag.EditorForce = false;
                _careTutorLive = false;
                _careLoaded = false;
                EditorShotLive = false;
                ShotKeepAwake = false;
                File.WriteAllText(CareShotDir + "/log.txt", log.ToString());
                File.WriteAllText("/tmp/flock-five-bird-care-done", "OK\n");
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif
