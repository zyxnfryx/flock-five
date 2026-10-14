#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MuteCurtainShot()
        {
            if (!File.Exists("/tmp/flock-five-b88-curtain")) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        // Home, then the real LEVEL press. Sparkles build, wash, and tear.
        // Also one avatar crop while the far wing is up behind the crown.
        public IEnumerator ShotLevelCurtain()
        {
            ShotKeepAwake = true;
            UnityEngine.Debug.Log("CURTAIN_SHOT_BEGIN");
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;

            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coach = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leaf = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveGarden = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            int bee = PlayerPrefs.GetInt(CoachBeeKey, 0);
            bool hadBee = PlayerPrefs.HasKey(CoachBeeKey);
            int sparrow = PlayerPrefs.GetInt(CoachSparrowKey, 0);
            bool hadSparrow = PlayerPrefs.HasKey(CoachSparrowKey);
            int hawk = PlayerPrefs.GetInt(CoachHawkKey, 0);
            bool hadHawk = PlayerPrefs.HasKey(CoachHawkKey);
            int ostrich = PlayerPrefs.GetInt(CoachOstrichKey, 0);
            bool hadOstrich = PlayerPrefs.HasKey(CoachOstrichKey);
            int colorKeep = PlayerPrefs.GetInt(AvatarPref, 0);
            bool hadColor = PlayerPrefs.HasKey(AvatarPref);
            string nameKeep = PlayerPrefs.GetString(AvatarNamePref, "");
            bool hadName = PlayerPrefs.HasKey(AvatarNamePref);
            int adoptedKeep = PlayerPrefs.GetInt(AvatarAdoptedPref, 0);
            bool hadAdopted = PlayerPrefs.HasKey(AvatarAdoptedPref);
            int kitKeep = PlayerPrefs.GetInt(AvatarKitPref, 0);
            bool hadKit = PlayerPrefs.HasKey(AvatarKitPref);
            int namedKeep = PlayerPrefs.GetInt(AvatarNamedPref, 0);
            bool hadNamed = PlayerPrefs.HasKey(AvatarNamedPref);
            int badgerKeep = PlayerPrefs.GetInt(BadgerSave.Pref, 0);
            bool hadBadger = PlayerPrefs.HasKey(BadgerSave.Pref);

            const string mp4 = "/Users/zfxgames/wkspaces/wingout/b88-level-curtain.mp4";
            const string wingPng = "/Users/zfxgames/wkspaces/wingout/b88-wing-avatar.png";
            const string frames = "/tmp/curtain-frames";
            var phases = new StringBuilder();
            try
            {
                Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);

                PlayerPrefs.SetInt("flockfive.next", 3);
                PlayerPrefs.SetInt(CoachKey, 1);
                PlayerPrefs.SetInt(CoachLeafKey, 1);
                PlayerPrefs.SetInt(CoachHiveGardenKey, 1);
                PlayerPrefs.SetInt(CoachBeeKey, 1);
                PlayerPrefs.SetInt(CoachSparrowKey, 1);
                PlayerPrefs.SetInt(CoachHawkKey, 1);
                PlayerPrefs.SetInt(CoachOstrichKey, 1);
                PlayerPrefs.SetInt(AvatarPref, (int)BirdColor.Ruby);
                PlayerPrefs.SetString(AvatarNamePref, "Ruby");
                PlayerPrefs.SetInt(AvatarAdoptedPref, 1);
                PlayerPrefs.SetInt(AvatarNamedPref, 1);
                PlayerPrefs.SetInt(AvatarKitPref, 2);
                BadgerSave.Clear();
                PlayerPrefs.Save();

                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                ShowSplash();
                yield return new WaitForSecondsRealtime(0.8f);
                MuteShotAudio();
                _adoptLive = false;
                _settingsOpen = false;
                yield return null;
                yield return new WaitForEndOfFrame();

                bool winged = false;
                for (int i = 0; i < 24; i++)
                {
                    yield return null;
                    PinCurtainAvatar();
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    SpriteCatalog.FlapPair(_avatarCol, BirdSex.Neutral, _avatarClock * AvatarFlapRate, true,
                        out var body, out _, out _);
                    if (body != null && SpriteCatalog.TrySplitFarWing(body, out _, out _) && _avatarDrawR.width > 8f)
                    {
                        SaveAvatarCrop(wingPng);
                        winged = true;
                        break;
                    }
                }
                if (!winged && _avatarDrawR.width > 8f)
                    SaveAvatarCrop(wingPng);
                phases.Append("wing ").Append(winged ? "split" : "miss").Append(' ')
                    .Append(_avatarDrawR.width.ToString("0")).Append('\n');

                ParkHomeAvatar();
                _avatarCol = BirdColor.Ruby;
                yield return null;
                yield return new WaitForEndOfFrame();

                Time.captureDeltaTime = 1f / 60f;
                int lead = 8;
                int play = 64;
                int tail = 14;
                int w = 0, h = 0;
                bool armed = false;
                bool pressed = false;
                int total = lead + play + tail;
                for (int f = 0; f < total; f++)
                {
                    yield return null;
                    MuteShotAudio();
                    if (!armed && f >= lead)
                    {
                        // Pointer-down. The sheet builds on the home. Load waits
                        // until the wash is closed, so the tear is what reveals the garden.
                        LevelCurtain.Aim(FlowerPlayRect());
                        LevelCurtain.Arm(AdPause.Unscaled);
                        armed = true;
                    }
                    if (armed && !pressed)
                    {
                        LevelCurtain.Aim(FlowerPlayRect());
                        float ageNow = LevelCurtain.VisualAge(AdPause.Unscaled);
                        if (ageNow + 0.0001f >= LevelCurtain.Flood)
                        {
                            PressLevelButton();
                            pressed = true;
                        }
                    }
                    if (!pressed)
                    {
                        ParkHomeAvatar();
                        _avatarCol = BirdColor.Ruby;
                    }
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    w = tex.width;
                    h = tex.height;
                    File.WriteAllBytes(frames + "/f" + f.ToString("0000") + ".jpg", tex.EncodeToJPG(86));
                    Object.Destroy(tex);
                    float age = LevelCurtain.Armed ? LevelCurtain.VisualAge(AdPause.Unscaled) : -1f;
                    string beat = !armed || age < 0f ? "home" : LevelCurtain.BeatName(age);
                    phases.Append(f.ToString("000")).Append(' ').Append(beat).Append(' ')
                        .Append(age.ToString("0.000")).Append(' ')
                        .Append(LevelCurtain.LoadStarted ? "load" : "idle").Append('\n');
                }
                Time.captureDeltaTime = 0f;
                File.WriteAllText("/tmp/curtain-clip-phases.txt", phases.ToString());
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 60 -i \"" + frames + "/f%04d.jpg\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -r 60 \"" + mp4 + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null) proc.WaitForExit(120000);
                }
                File.WriteAllText("/tmp/flock-five-b88-curtain-done", "CURTAIN_CLIP " + w + "x" + h + " frames " + (lead + play + tail) + " wing " + (winged ? "1" : "0") + "\n");
                UnityEngine.Debug.Log("CURTAIN_CLIP_DONE " + w + "x" + h + " wing " + winged);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                LevelCurtain.Clear();
                if (hadNext) PlayerPrefs.SetInt("flockfive.next", next);
                else PlayerPrefs.DeleteKey("flockfive.next");
                if (hadCoach) PlayerPrefs.SetInt(CoachKey, coach);
                else PlayerPrefs.DeleteKey(CoachKey);
                if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leaf);
                else PlayerPrefs.DeleteKey(CoachLeafKey);
                if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveGarden);
                else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
                if (hadBee) PlayerPrefs.SetInt(CoachBeeKey, bee);
                else PlayerPrefs.DeleteKey(CoachBeeKey);
                if (hadSparrow) PlayerPrefs.SetInt(CoachSparrowKey, sparrow);
                else PlayerPrefs.DeleteKey(CoachSparrowKey);
                if (hadHawk) PlayerPrefs.SetInt(CoachHawkKey, hawk);
                else PlayerPrefs.DeleteKey(CoachHawkKey);
                if (hadOstrich) PlayerPrefs.SetInt(CoachOstrichKey, ostrich);
                else PlayerPrefs.DeleteKey(CoachOstrichKey);
                if (hadColor) PlayerPrefs.SetInt(AvatarPref, colorKeep);
                else PlayerPrefs.DeleteKey(AvatarPref);
                if (hadName) PlayerPrefs.SetString(AvatarNamePref, nameKeep);
                else PlayerPrefs.DeleteKey(AvatarNamePref);
                if (hadAdopted) PlayerPrefs.SetInt(AvatarAdoptedPref, adoptedKeep);
                else PlayerPrefs.DeleteKey(AvatarAdoptedPref);
                if (hadKit) PlayerPrefs.SetInt(AvatarKitPref, kitKeep);
                else PlayerPrefs.DeleteKey(AvatarKitPref);
                if (hadNamed) PlayerPrefs.SetInt(AvatarNamedPref, namedKeep);
                else PlayerPrefs.DeleteKey(AvatarNamedPref);
                if (hadBadger) PlayerPrefs.SetInt(BadgerSave.Pref, badgerKeep);
                else PlayerPrefs.DeleteKey(BadgerSave.Pref);
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        // Off the perch, crown on, clock parked on the raised far-wing pose.
        void PinCurtainAvatar()
        {
            _adoptLive = false;
            _adoptStep = AdoptStep.Off;
            _adoptOwed = false;
            _avatarRename = false;
            _avatarHold = false;
            _avatarCrossing = false;
            _avatarGliding = false;
            _avatarHappy = 0f;
            _avatarPreenT = 0f;
            _avatarPlaced = true;
            _avatarPose = AvatarPose.Flying;
            _avatarCol = BirdColor.Ruby;
            _avatarFaceLeft = false;
            _avatarClock = 0.115f;
            _avatarPos = new Vector2(Screen.width * 0.50f, Screen.height * 0.40f);
        }
    }
}
#endif
