#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Sound lives in Settings. No mute button on home, garden, or poker.
    public static class Build88MuteTests
    {
        const string Out = "/tmp/flock-five-b88-mute-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88mute] " + line);
            }

            float? keepWindow = DeviceFit.TestWindow;
            bool hadVol = PlayerPrefs.HasKey(AudioGuard.VolumeKey);
            float oldVol = PlayerPrefs.GetFloat(AudioGuard.VolumeKey, 1f);
            bool hadMute = PlayerPrefs.HasKey(AudioGuard.MuteKey);
            int oldMute = PlayerPrefs.GetInt(AudioGuard.MuteKey, 0);
            float oldListener = 1f;
            bool oldPause = false;
            int shows = 0;
            try
            {
                oldListener = AudioListener.volume;
                oldPause = AudioListener.pause;

                string root = Application.dataPath;
                string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
                string settings = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
                string daily = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
                string vip = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.VipOffer.cs"));
                bool chromeGone = !File.Exists(Path.Combine(root, "_Project/Scripts/App/MuteChrome.cs"))
                    && app.IndexOf("MuteChrome") < 0
                    && app.IndexOf("PollMuteChrome") < 0
                    && app.IndexOf("PaintMuteChrome") < 0
                    && app.IndexOf("DrawSpeakerIcon") < 0
                    && settings.IndexOf("MuteChrome") < 0
                    && settings.IndexOf("DrawSpeakerIcon") < 0
                    && settings.IndexOf("HomeMuteRect") < 0;
                Check("no-onscreen-mute", chromeGone, "home garden poker have no speaker");

                int sheet = settings.IndexOf("void DrawSettingsSheet");
                int toggle = settings.IndexOf("AudioGuard.ToggleMuted()", sheet);
                int slider = settings.IndexOf("AudioGuard.SetMasterVolume", sheet);
                int words = settings.IndexOf("SettingsSheet.SoundOnLabel", sheet);
                Check("settings-wires-sound",
                    sheet >= 0 && toggle > sheet && slider > sheet && words > sheet
                    && settings.IndexOf("DrawSoundRow(") > sheet,
                    "toggle " + toggle + " slider " + slider);

                var phoneSafe = new Rect(0f, 102f, 1179f, 2556f - 102f - 34f);
                var padSafe = new Rect(0f, 20f, 1536f, 2048f - 40f);
                Sheet(Check, "iphone", 1179f, 2556f, phoneSafe, 220f);
                Sheet(Check, "ipad", 1536f, 2048f, padSafe, 80f);

                var card = new Rect(90f, 420f, 980f, 1200f);
                var x = FlockFiveApp.DailyCloseInFrame(card, 2.6f);
                var glyph = FlockFiveApp.CloseGlyphBox(x);
                bool dailyIn = Holds(card, x) && Holds(card, glyph)
                    && x.center.x > card.center.x
                    && x.y < card.y + card.height * 0.28f;
                var small = new Rect(40f, 200f, 640f, 860f);
                var x2 = FlockFiveApp.DailyCloseInFrame(small, 1.4f);
                var g2 = FlockFiveApp.CloseGlyphBox(x2);
                bool dailySmall = Holds(small, x2) && Holds(small, g2);
                bool dailySrc = daily.IndexOf("DailyCloseInFrame(card, s)") >= 0
                    && daily.IndexOf("CornerCloseRect") < 0
                    && vip.IndexOf("CornerCloseRect") >= 0
                    && app.IndexOf("card.xMax - xSz - 6f") >= 0;
                Check("daily-x-inside-frame", dailyIn && dailySmall && dailySrc,
                    "x " + x + " glyph " + glyph + " small " + x2);

                AudioGuard.ResetForTests();
                GamePause.Reset();
                AdPause.ResetForTest();
                AudioListener.volume = 1f;
                AudioListener.pause = false;

                float listener = AudioListener.volume;
                AudioGuard.SetMasterVolume(0.33f, true);
                AudioGuard.SetMuted(true, true);
                bool stored = PlayerPrefs.GetInt(AudioGuard.MuteKey, 0) == 1
                    && Mathf.Abs(PlayerPrefs.GetFloat(AudioGuard.VolumeKey, -1f) - 0.33f) < 0.001f
                    && Mathf.Abs(AudioListener.volume - listener) < 0.001f;
                AudioGuard.ResetForTests();
                AudioGuard.LoadMuted();
                AudioGuard.LoadVolume();
                bool loaded = AudioGuard.Muted
                    && Mathf.Abs(AudioGuard.MasterVolume - 0.33f) < 0.001f
                    && AudioGuard.MusicScale == 0f
                    && AudioGuard.SfxScale == 0f;
                AudioGuard.SetMuted(false, false);
                bool restored = !AudioGuard.Muted
                    && Mathf.Abs(AudioGuard.MasterVolume - 0.33f) < 0.001f
                    && Mathf.Abs(AudioGuard.MusicScale - 0.33f) < 0.001f
                    && Mathf.Abs(AudioGuard.SfxScale - 0.33f) < 0.001f;
                Check("slider-persists-and-unmute-restores", stored && loaded && restored,
                    "stored=" + stored + " loaded=" + loaded + " back=" + AudioGuard.MusicScale.ToString("0.00"));

                AudioGuard.ResetForTests();
                AudioGuard.SetMasterVolume(0.8f, false);
                AudioGuard.SetMuted(false, false);
                AudioGuard.PushDuck("ad");
                float ducked = AudioGuard.MusicScale;
                float gain = MixDesk.MasterGain;
                bool mul = Mathf.Abs(ducked - 0.8f * AudioGuard.DuckGain) < 0.001f
                    && Mathf.Abs(gain - 0.8f * AudioGuard.DuckGain) < 0.001f
                    && Mathf.Abs(AudioGuard.MasterVolume - 0.8f) < 0.001f
                    && Mathf.Abs(ducked - 0.8f) > 0.05f;
                AudioGuard.SetMuted(true, false);
                bool off = AudioGuard.MusicScale == 0f && Mathf.Abs(AudioGuard.MasterVolume - 0.8f) < 0.001f;
                AudioGuard.SetMuted(false, false);
                bool duckBack = Mathf.Abs(AudioGuard.MusicScale - ducked) < 0.001f;
                Check("duck-multiplies-slider", mul && off && duckBack,
                    "scale=" + ducked.ToString("0.000") + " gain=" + gain.ToString("0.000"));

                AudioGuard.ResetForTests();
                AudioGuard.SetMasterVolume(1f, false);
                AudioGuard.SetMuted(false, false);
                GamePause.Reset();
                AdPause.ResetForTest();
                Ads.BeginShow();
                shows++;
                bool silent = Ads.IsShowing
                    && MixDesk.MasterGain == 0f
                    && AudioGuard.MusicScale == 0f
                    && AudioGuard.SfxScale == 0f
                    && Mathf.Abs(AudioGuard.MasterVolume - 1f) < 0.001f
                    && AudioListener.pause;
                Ads.EndShow();
                shows--;
                bool afterAd = !Ads.IsShowing
                    && Mathf.Abs(MixDesk.MasterGain - 1f) < 0.001f
                    && Mathf.Abs(AudioGuard.MusicScale - 1f) < 0.001f
                    && Mathf.Abs(AudioGuard.MasterVolume - 1f) < 0.001f;
                Check("ad-mute-silences-over-slider", silent && afterAd,
                    "silent=" + silent + " after=" + AudioGuard.MusicScale.ToString("0.00"));
            }
            catch (System.Exception ex)
            {
                Check("mute", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                while (shows > 0)
                {
                    Ads.EndShow();
                    shows--;
                }
                if (Ads.IsShowing) Ads.EndShow();
                GamePause.Reset();
                AdPause.ResetForTest();
                Time.timeScale = 1f;
                AudioListener.pause = oldPause;
                AudioListener.volume = oldListener;
                if (hadVol) PlayerPrefs.SetFloat(AudioGuard.VolumeKey, oldVol);
                else PlayerPrefs.DeleteKey(AudioGuard.VolumeKey);
                if (hadMute) PlayerPrefs.SetInt(AudioGuard.MuteKey, oldMute);
                else PlayerPrefs.DeleteKey(AudioGuard.MuteKey);
                PlayerPrefs.Save();
                AudioGuard.ResetForTests();
                AudioGuard.LoadMuted();
                AudioGuard.LoadVolume();
                DeviceFit.TestWindow = keepWindow;
            }

            string end = fail == 0 ? "BUILD88MUTE_OK " + pass : "BUILD88MUTE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88mute] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Sheet(System.Action<string, bool, string> check, string tag, float w, float h, Rect safe, float top)
        {
            var frame = SettingsSheet.Layout(w, h, safe, top);
            var b = frame.Body;
            bool row = b.SoundToggle.width >= 44f
                && b.SoundSlider.width >= 44f
                && b.SoundToggle.height >= 44f
                && b.SoundSlider.xMin >= b.SoundToggle.xMax - 0.5f
                && Mathf.Abs(b.SoundToggle.y - b.SoundSlider.y) < 0.5f
                && !b.SoundToggle.Overlaps(b.SoundSlider)
                && b.SoundToggle.xMin >= frame.Content.x - 0.5f
                && b.SoundSlider.xMax <= frame.Content.xMax + 0.5f
                && b.SoundToggle.yMax <= b.PrivacyLink.y + 0.5f
                && !b.SoundToggle.Overlaps(b.PrivacyLink)
                && b.AnimateLabel.yMax <= frame.Content.yMax + 0.5f
                && frame.Card.yMax <= h + 0.5f
                && frame.Card.yMin >= -0.5f;
            bool x = frame.Close.width >= 44f
                && frame.Close.center.x > frame.Card.center.x
                && frame.Close.yMin >= frame.Card.y
                && frame.Close.xMax <= frame.Card.xMax + 0.5f
                && frame.Close.yMax <= frame.Title.y + 0.5f
                && !frame.Close.Overlaps(frame.Title)
                && !frame.Close.Overlaps(b.SoundToggle)
                && !frame.Close.Overlaps(b.SoundSlider);
            check(tag + "-sound-row-and-close",
                row && x && SettingsSheet.SoundOnLabel == "Sound On" && SettingsSheet.SoundOffLabel == "Sound Off",
                "toggle " + b.SoundToggle + " slider " + b.SoundSlider + " close " + frame.Close + " card " + frame.Card);
        }

        static bool Holds(Rect frame, Rect r)
        {
            return r.width > 8f
                && r.xMin >= frame.xMin - 0.5f
                && r.yMin >= frame.yMin - 0.5f
                && r.xMax <= frame.xMax + 0.5f
                && r.yMax <= frame.yMax + 0.5f;
        }
    }
}
#endif
