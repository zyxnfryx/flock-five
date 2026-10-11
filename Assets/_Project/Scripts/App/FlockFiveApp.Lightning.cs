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

        static void MuteShotAudio()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

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

        IEnumerator RecordClip(string frames, string mp4, float seconds, string stillPath)
        {
            try { if (Directory.Exists(frames)) Directory.Delete(frames, true); } catch { }
            Directory.CreateDirectory(frames);
            int n = 0;
            float t0 = Time.unscaledTime;
            float next = t0;
            const float step = 1f / 12f;
            while (Time.unscaledTime - t0 < seconds)
            {
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
                if (Time.unscaledTime < next) continue;
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
                next = Time.unscaledTime + step;
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
    }
}
