using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FlockFive
{
    // Storm strike on an unused gift. The roll lives here; the rules live on
    // LightningStrike. Capture is the same play session, muted.
    public sealed partial class FlockFiveApp
    {
        readonly bool[] _offerBurned = new bool[WorldBuilder.GiftCount];
        int _lightningStrikes;
        bool _lightningRolled;
        int _fireMisses;
        int _fireShown;
        bool _shot82e;
        bool _shot82eStill;

        void ResetLightning()
        {
            for (int i = 0; i < _offerBurned.Length; i++) _offerBurned[i] = false;
            _lightningStrikes = 0;
            _lightningRolled = false;
        }

        // One roll per wet window. Dry resets it. Force is the capture hook:
        // it still obeys the cap, the unused-gift rule, and the solvability gate.
        void TickLightning()
        {
            if (_shot82e) MuteShotAudio();
            if (_restarting || _won || _busy || _board == null) return;
            bool storming = GardenStorm.Wet > 0.45f;
            bool pending = LightningStrike.Force;
            if (!storming && !pending)
            {
                _lightningRolled = false;
                return;
            }
            if (_lightningRolled && !pending) return;
            bool force = LightningStrike.ConsumeForce();
            if (!force) _lightningRolled = true;

            int index = LightningStrike.TryPick(
                _board, _bonusOn, _offerBurned, storming, _lightningStrikes, Random.value, force);
            if (index < 0) return;
            var view = ViewFor(index);
            if (view == null) return;

            LightningStrike.Burn(_board.Branches[index]);
            int ord = BonusBranches.Ordinal(_board, index);
            if ((uint)ord < (uint)_offerBurned.Length) _offerBurned[ord] = true;
            _lightningStrikes++;
            LightningStrike.NotePlay();
            view.BeginScorch(_garden.Root);
            GardenStorm.StrikeAt(view.transform.position);
            RefreshBonusSigns();
            if (_shot82e) MuteShotAudio();
        }

        BranchView ViewFor(int index)
        {
            var views = _garden.Branches;
            if (views == null) return null;
            if ((uint)index < (uint)views.Length && views[index] != null && views[index].Index == index)
                return views[index];
            for (int i = 0; i < views.Length; i++)
                if (views[i] != null && views[i].Index == index) return views[i];
            return null;
        }

        void BindLightningFire() => GardenStorm.FireAim = OfferFire;

        void UnbindLightningFire()
        {
            if (GardenStorm.FireAim == OfferFire)
                GardenStorm.FireAim = null;
        }

        // One roll per storm bolt. Always on. A miss climbs the odds. A fire
        // aims the bolt and burns the limb. No legal limb: the bolt stays dry.
        bool OfferFire(out Vector3 aim)
        {
            aim = default;
            if (_splash || _restarting || _won || _busy || _board == null) return false;
            bool force = LightningFire.Force;
            if (!force && GardenStorm.Wet <= 0.45f) return false;
            force = LightningFire.ConsumeForce();
            int before = _fireMisses;
            var roll = LightningFire.Resolve(
                _board, _bonusOn, _offerBurned, before, Random.value, force);
            var view = roll.Fired ? ViewFor(roll.Index) : null;
            if (!roll.Fired || view == null)
            {
                _fireMisses = roll.Fired ? before + 1 : roll.Misses;
                return false;
            }
            _fireMisses = 0;
            ApplyFire(roll.Index, view);
            aim = view.transform.position;
            return true;
        }

        void ApplyFire(int index, BranchView view)
        {
            var st = _board.Branches[index];
            if (st != null && st.IsBonus)
            {
                int ord = BonusBranches.Ordinal(_board, index);
                LightningFire.Apply(st);
                if ((uint)ord < (uint)_offerBurned.Length) _offerBurned[ord] = true;
            }
            else
            {
                LightningFire.Apply(st);
            }
            _fireShown++;
            if (view != null) view.BeginScorch(_garden.Root);
            RefreshBonusSigns();
        }

        static void MuteShotAudio()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

#if UNITY_EDITOR
        public IEnumerator ShotBuild82e()
        {
            if (_shot82e) yield break;
            _shot82e = true;
            MuteShotAudio();
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
#endif
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b82out";
            Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coachKeep = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leafKeep = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            bool hadAnim = PlayerPrefs.HasKey(BackgroundMotion.PrefKey);
            int animKeep = PlayerPrefs.GetInt(BackgroundMotion.PrefKey, 1);
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int boltKeep = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            const string seenKey = "flockfive.season.seen";
            bool hadSeen = PlayerPrefs.HasKey(seenKey);
            string seenKeep = PlayerPrefs.GetString(seenKey, "");
            bool reduceKeep = BackgroundMotion.ReduceMotion;

            BackgroundMotion.ReduceMotion = false;
            BackgroundMotion.On = true;
            LightningStrike.Enabled = false;
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            GamePause.Reset();
            Screen.SetResolution(1179, 2556, false);

            yield return WaitForHomePaint();
            for (int i = 0; i < 3; i++)
            {
                PoseBuild79(true, false);
                MuteShotAudio();
                yield return null;
            }
            PoseBuild79(true, false);
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            SaveFrame(dir + "/b82-settings.png");
            PoseBuild79(false, false);

            yield return RecordClip("/tmp/b82e-home", dir + "/b82-bg-home.mp4", 10f, null);
            PoseBuild79(false, false);

            SeasonReveal.Mark(1);
            PrepGardenShot(0);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return RecordClip("/tmp/b82e-garden1", dir + "/b82-bg-garden1.mp4", 10f, null);

            SeasonReveal.Mark(15);
            PrepGardenShot(14);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return RecordClip("/tmp/b82e-garden2", dir + "/b82-bg-garden2.mp4", 10f, null);

            SeasonReveal.Mark(1);
            PrepGardenShot(0);
            yield return new WaitForSecondsRealtime(1.2f);
            if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
            int aim = LightningStrike.Target(_board, _bonusOn, _offerBurned);
            bool solvable = aim >= 0 && LightningStrike.KeepsSolvable(_board, aim);
            Debug.Log("b82e lightning aim=" + aim + " solvable=" + solvable + " wet=" + GardenStorm.Wet.ToString("0.00"));
            LightningStrike.Force = true;
            _shot82eStill = false;
            yield return RecordClip("/tmp/b82e-bolt", dir + "/b82-lightning.mp4", 10f, dir + "/b82-lightning.png");
            Debug.Log("b82e lightning still=" + _shot82eStill + " plays=" + LightningStrike.Plays);

            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadCoach) PlayerPrefs.SetInt(CoachKey, coachKeep);
            else PlayerPrefs.DeleteKey(CoachKey);
            if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leafKeep);
            else PlayerPrefs.DeleteKey(CoachLeafKey);
            if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveKeep);
            else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
            if (hadAnim) PlayerPrefs.SetInt(BackgroundMotion.PrefKey, animKeep);
            else PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
            if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, boltKeep);
            else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
            if (hadSeen) PlayerPrefs.SetString(seenKey, seenKeep);
            else PlayerPrefs.DeleteKey(seenKey);
            PlayerPrefs.Save();
            BackgroundMotion.ReduceMotion = reduceKeep;
            LightningStrike.Force = false;
            Debug.Log("B82E_STILLS_DONE");
#if UNITY_EDITOR
            EditorShotLive = false;
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        void PrepGardenShot(int index)
        {
            PlayerPrefs.SetInt("flockfive.next", index);
            PlayerPrefs.SetInt(CoachKey, 1);
            PlayerPrefs.SetInt(CoachLeafKey, 1);
            PlayerPrefs.SetInt(CoachHiveGardenKey, 1);
            PlayerPrefs.Save();
            Load(index);
            _coach = false;
            _leafIntro = false;
            _hiveLevelLive = false;
            _hiveLevelCue = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _coachFade = 0f;
            _splash = false;
            MuteShotAudio();
        }

        IEnumerator RecordClip(string frames, string mp4, float seconds, string stillPath, System.Action tick = null)
        {
            try { if (Directory.Exists(frames)) Directory.Delete(frames, true); } catch { }
            Directory.CreateDirectory(frames);
            int n = 0;
            float t0 = AdPause.Unscaled;
            float next = t0;
            const float step = 1f / 12f;
            while (AdPause.Unscaled - t0 < seconds)
            {
                if (tick != null) tick();
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                if (stillPath != null && !_shot82eStill)
                {
                    bool flash = GardenStorm.SkyFlash > 0.12f;
                    bool fx = UnityEngine.Object.FindAnyObjectByType<StrikeFx>() != null;
                    if (flash || fx)
                    {
                        SaveFrame(stillPath);
                        _shot82eStill = true;
                        Debug.Log("b82e bolt frame flash=" + flash + " fx=" + fx);
                    }
                }
                if (AdPause.Unscaled < next) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) continue;
                try
                {
                    File.WriteAllBytes(frames + "/" + n.ToString("D4") + ".jpg", tex.EncodeToJPG(74));
                    n++;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("b82e frame " + e.Message);
                }
                Destroy(tex);
                next = AdPause.Unscaled + step;
            }
            float fps = n / Mathf.Max(0.5f, seconds);
            if (fps < 4f) fps = 8f;
            EncodeClip(frames, mp4, fps);
            try { Directory.Delete(frames, true); } catch { }
            Debug.Log("b82e clip " + mp4 + " frames=" + n + " fps=" + fps.ToString("0.0"));
        }

        static void EncodeClip(string frames, string mp4, float fps)
        {
            string rate = fps.ToString("0.##", CultureInfo.InvariantCulture);
            string args = "-y -loglevel error -framerate " + rate
                + " -i \"" + frames + "/%04d.jpg\""
                + " -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -crf 20 -movflags +faststart \""
                + mp4 + "\"";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) return;
                    if (!p.WaitForExit(120000))
                        Debug.LogWarning("b82e ffmpeg timed out");
                    else if (p.ExitCode != 0)
                        Debug.LogWarning("b82e ffmpeg exit " + p.ExitCode);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("b82e ffmpeg " + e.Message);
            }
        }

        bool _shot82g;

        // Finish pass. Lessons the earlier shots never wrote, the badger caption,
        // one clip per garden season, the unused-gift bolt, and a Go VIP press.
        public IEnumerator ShotBuild82g()
        {
            if (_shot82g) yield break;
            _shot82g = true;
            MuteShotAudio();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
#endif
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b82out";
            Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coachKeep = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leafKeep = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            bool hadAnim = PlayerPrefs.HasKey(BackgroundMotion.PrefKey);
            int animKeep = PlayerPrefs.GetInt(BackgroundMotion.PrefKey, 1);
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int boltKeep = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            const string seenKey = "flockfive.season.seen";
            bool hadSeen = PlayerPrefs.HasKey(seenKey);
            string seenKeep = PlayerPrefs.GetString(seenKey, "");
            bool reduceKeep = BackgroundMotion.ReduceMotion;
            bool badgerKeep = BadgerSchedule.Enabled;
            int badgerFlag = PrefGuard.GetInt(BadgerSave.Pref, 0);
            bool hadBadgerFlag = PlayerPrefs.HasKey(BadgerSave.Pref);

            BackgroundMotion.ReduceMotion = false;
            BackgroundMotion.On = true;
            LightningStrike.Enabled = false;
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            GamePause.Reset();
            Screen.SetResolution(1179, 2556, false);
            MuteShotAudio();

            PlayerPrefs.SetInt("flockfive.next", 1);
            PlayerPrefs.Save();
            Load(0);
            _shotBeeLesson = true;
            _shotCardLesson = false;
            _shotBirdHome = false;
            _shotDailyCaption = false;
            yield return new WaitForSecondsRealtime(1.5f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-bee-lesson.png");

            _shotBeeLesson = false;
            _shotGlovePin = false;
            _hiveLevelLive = false;
            _hiveLevelCue = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            if (Hive.Found <= 0) Hive.GrantVisitor();
            _shotCardSlot = -1;
            _shotCardLesson = true;
            yield return new WaitForSecondsRealtime(1.6f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-card-lesson.png");

            _shotCardLesson = false;
            _shotGlovePin = false;
            _albumTutorOn = false;
            _hiveInspect = -1;
            _home = HomeFace.Splash;
            _splash = true;
            _shotBirdHome = true;
            yield return new WaitForSecondsRealtime(1.4f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-bird-followed.png");

            _shotBirdHome = false;
            _adoptLive = false;
            _shotDailyCaption = true;
            yield return new WaitForSecondsRealtime(1.3f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-daily-caption.png");
            _shotDailyCaption = false;
            _dailyIntroLive = false;
            _adoptLive = false;
            Debug.Log("b82g lessons written");

#if UNITY_EDITOR
            EditorShotLive = true;
            BadgerShotOverride = true;
#endif
            BadgerSchedule.Enabled = true;
            BadgerSave.Set(15, true);
            OpenBadgerFight(15);
            BeginBadgerFight();
            _bgStage = BadgerStage.Lesson;
            _bgLessonLive = true;
            _bgLessonStep = 2;
            _coachFade = 1f;
            _bgLine = BadgerCopy.LessonAt(2);
            yield return new WaitForSecondsRealtime(0.6f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82-badger-caption.png");
            _bgLessonLive = false;
            _home = HomeFace.Splash;
            _bgFight = null;
            _splash = true;
            Debug.Log("b82g badger caption written");

            yield return WaitForHomePaint();
            for (int i = 0; i < 3; i++)
            {
                PoseBuild79(true, false);
                MuteShotAudio();
                yield return null;
            }
            PoseBuild79(true, false);
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            SaveFrame(dir + "/b82-settings.png");
            PoseBuild79(false, false);

            yield return RecordClip("/tmp/b82g-home", dir + "/b82-bg-home.mp4", 6f, null);

            var scenes = new[]
            {
                GardenScene.Summer, GardenScene.Desert, GardenScene.Fall,
                GardenScene.Winter, GardenScene.Spring,
            };
            for (int i = 0; i < scenes.Length; i++)
            {
                int level = SeasonShotLevel(scenes[i]);
                string name = scenes[i].ToString().ToLowerInvariant();
                Debug.Log("b82g season " + name + " level " + level);
                SeasonReveal.Mark(level);
                PrepGardenShot(level - 1);
                yield return new WaitForSecondsRealtime(1.5f);
                MuteShotAudio();
                yield return RecordClip("/tmp/b82g-" + name, dir + "/b82-bg-" + name + ".mp4", 6f, null);
            }

            SeasonReveal.Mark(1);
            PrepGardenShot(0);
            yield return new WaitForSecondsRealtime(1.3f);
            if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
            int aim = LightningStrike.Target(_board, _bonusOn, _offerBurned);
            bool solvable = aim >= 0 && LightningStrike.KeepsSolvable(_board, aim);
            Debug.Log("b82g lightning aim=" + aim + " solvable=" + solvable);
            LightningStrike.Force = true;
            _shot82eStill = false;
            _shot82e = true;
            yield return RecordClip("/tmp/b82g-bolt", dir + "/b82-lightning.mp4", 9f, dir + "/b82-lightning.png");
            LightningStrike.Force = false;
            _shot82e = false;
            Debug.Log("b82g lightning still=" + _shot82eStill + " plays=" + LightningStrike.Plays);

            _home = HomeFace.Splash;
            _splash = true;
            _coach = false;
            _cueLine = null;
            _gloveVis = false;
            VipOffer.Show();
            yield return new WaitForSecondsRealtime(3.4f);
            MuteShotAudio();
            float pressAt = AdPause.Unscaled;
            PushButton.Force = true;
            yield return RecordClip("/tmp/b82g-vip", dir + "/b82-vip-press.mp4", 1.15f, dir + "/b82-vip-press.png", () =>
            {
                float age = AdPause.Unscaled - pressAt;
                PushButton.Force = false;
                var pose = age < 0.18f
                    ? PushButton.At(true, -1f)
                    : PushButton.At(false, age - 0.18f);
                PushButton.Forced = pose;
                PushButton.Force = true;
                MuteShotAudio();
            });
            PushButton.Force = false;
            VipOffer.Close();

            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadCoach) PlayerPrefs.SetInt(CoachKey, coachKeep);
            else PlayerPrefs.DeleteKey(CoachKey);
            if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leafKeep);
            else PlayerPrefs.DeleteKey(CoachLeafKey);
            if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveKeep);
            else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
            if (hadAnim) PlayerPrefs.SetInt(BackgroundMotion.PrefKey, animKeep);
            else PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
            if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, boltKeep);
            else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
            if (hadSeen) PlayerPrefs.SetString(seenKey, seenKeep);
            else PlayerPrefs.DeleteKey(seenKey);
            if (hadBadgerFlag) PrefGuard.SetInt(BadgerSave.Pref, badgerFlag);
            else
            {
                PlayerPrefs.DeleteKey(BadgerSave.Pref);
                PlayerPrefs.DeleteKey(BadgerSave.Pref + ".x");
            }
            PlayerPrefs.Save();
            BackgroundMotion.ReduceMotion = reduceKeep;
            BadgerSchedule.Enabled = badgerKeep;
            LightningStrike.Force = false;
            Debug.Log("B82G_STILLS_DONE");
#if UNITY_EDITOR
            EditorShotLive = false;
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        bool _shotFire;

        // 60 fps play-mode clip of one forced fire. The roll is still the real
        // target rules; Force only skips the dice.
        public IEnumerator ShotLightningFire()
        {
            if (_shotFire) yield break;
            _shotFire = true;
            ShotKeepAwake = true;
            EditorShotLive = true;
            MuteShotAudio();
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;

            const string frames = "/tmp/lightning-fire-frames";
            const string mp4 = "/Users/zfxgames/wkspaces/post88/lightning-fire.mp4";
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/post88");

            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coachKeep = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leafKeep = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int boltKeep = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);

            LightningStrike.Enabled = false;
            LightningStrike.Force = false;
            LightningFire.Force = false;
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            GamePause.Reset();
            if (EditorGameSize != null) EditorGameSize(1179, 2556);
            Screen.SetResolution(1179, 2556, false);

            yield return null;
            PrepGardenShot(0);
            MuteShotAudio();
            float wait = 0f;
            while (wait < 8f && (GardenStorm.Instance == null || !GardenStorm.Instance.BoltReady))
            {
                if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
                MuteShotAudio();
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
            int aim = _board != null ? LightningFire.Target(_board, _bonusOn, _offerBurned) : -1;
            Debug.Log("lightning-fire aim=" + aim + " wet=" + GardenStorm.Wet.ToString("0.00")
                + " bolts=" + (GardenStorm.Instance != null && GardenStorm.Instance.BoltReady));
            yield return new WaitForSecondsRealtime(0.35f);

            int shown = _fireShown;
            LightningFire.Force = true;
            if (GardenStorm.Instance != null) GardenStorm.Instance.CueBolt(0.45f);
            Time.captureFramerate = 60;
            try { if (Directory.Exists(frames)) Directory.Delete(frames, true); } catch { }
            Directory.CreateDirectory(frames);
            int n = 0;
            float t = 0f;
            const float seconds = 4.2f;
            while (t < seconds)
            {
                if (_fireShown == shown) LightningFire.Force = true;
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null)
                {
                    try
                    {
                        File.WriteAllBytes(frames + "/" + n.ToString("D4") + ".jpg", tex.EncodeToJPG(82));
                        n++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("lightning-fire frame " + e.Message);
                    }
                    Destroy(tex);
                }
                t += Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
            }
            Time.captureFramerate = 0;
            LightningFire.Force = false;
            EncodeFireClip(frames, mp4);
            Debug.Log("LIGHTNING_FIRE_CLIP frames=" + n + " shown=" + _fireShown + " misses=" + _fireMisses);
            File.WriteAllText("/tmp/lightning-fire-clip-done", n + " " + _fireShown + "\n");

            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadCoach) PlayerPrefs.SetInt(CoachKey, coachKeep);
            else PlayerPrefs.DeleteKey(CoachKey);
            if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leafKeep);
            else PlayerPrefs.DeleteKey(CoachLeafKey);
            if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveKeep);
            else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
            if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, boltKeep);
            else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
            PlayerPrefs.Save();
            EditorShotLive = false;
            ShotKeepAwake = false;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.Exit(0);
        }

        static void EncodeFireClip(string frames, string mp4)
        {
            string args = "-y -loglevel error -framerate 60 -i \"" + frames + "/%04d.jpg\""
                + " -r 60 -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -crf 18 -movflags +faststart \""
                + mp4 + "\"";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) return;
                    if (!p.WaitForExit(180000))
                        Debug.LogWarning("lightning-fire ffmpeg timed out");
                    else if (p.ExitCode != 0)
                        Debug.LogWarning("lightning-fire ffmpeg exit " + p.ExitCode);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("lightning-fire ffmpeg " + e.Message);
            }
        }

        static int SeasonShotLevel(GardenScene scene)
        {
            if (scene == GardenScene.Summer) return 1;
            if (scene == GardenScene.Desert) return GardenSeason.DesertFrom;
            if (scene == GardenScene.Winter) return GardenSeason.WinterFrom;
            if (scene == GardenScene.Spring) return GardenSeason.SpringFrom;
            for (int level = GardenSeason.RotateFrom; level < 96; level++)
            {
                if (GardenSeason.ForLevel(level) != GardenScene.Fall) continue;
                if (GardenSeason.IsBadgerLevel(level)) continue;
                return level;
            }
            return 48;
        }
#endif
    }
}
