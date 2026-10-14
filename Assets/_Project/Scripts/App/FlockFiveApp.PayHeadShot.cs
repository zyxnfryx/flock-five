#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 89: open pay table header stills, iPhone, iPad, Android (each its own size,
        // no shared screen-scale shortcut), plus a geometry line per device.
        internal static PayHeadLayout.Bands _payHeadDbg;
        internal static Rect _payHeadPanelDbg, _payHeadTabDbg;
        internal static Vector2Int _payHeadPxDbg;
        internal static Vector2 _payHeadTitleSzDbg, _payHeadBetSzDbg, _payHeadRowsDbg;
        internal static float _payHeadFloorDbg;
        public static string PayHeadShotDir = "/Users/zfxgames/wkspaces/wingout/b89-payhead";

        public IEnumerator ShotPayHead()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int poker = PlayerPrefs.GetInt(CoachPokerKey, 0);
            int welcome = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            const string log = "/tmp/b89-payhead-shot.txt";
            try
            {
                Directory.CreateDirectory(PayHeadShotDir);
                File.WriteAllText(log, "");
                PlayerPrefs.SetInt("flockfive.next", 2);
                PlayerPrefs.SetInt(CoachPokerKey, 1);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                PlayerPrefs.Save();
                yield return ShotPayHeadAt(1179, 2556, "iphone", log);
                yield return ShotPayHeadAt(1536, 2048, "ipad", log);
                yield return ShotPayHeadAt(1080, 2400, "android", log);
                File.WriteAllText("/tmp/flock-five-b89-payhead-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachPokerKey, poker);
                PlayerPrefs.SetInt(WelcomeBonusKey, welcome);
                PlayerPrefs.Save();
                _pokerPayOpen = false; _pokerPayAnim = 0f;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        IEnumerator ShotPayHeadAt(int w, int h, string tag, string log)
        {
            if (EditorGameSize != null) EditorGameSize(w, h);
            Screen.SetResolution(w, h, false);
            yield return new WaitForSecondsRealtime(0.6f);
            MuteShotAudio();
            ShowSplash();
            _dailyIntro = false; _dailyIntroLive = false;
            _hiveIntro = false; _hiveIntroLive = false;
            _adoptLive = false; _welcomeOpen = false; _welcomeQueued = false; _welcomeGlove = false;
            _dailyOpen = false; _dailyAskOpen = false;
            // Let the warm cover / studio card clear before opening the table.
            for (int calm = 0, f = 0; calm < 8 && f < 60 * 45; f++)
            {
                calm = WarmCoverLive ? 0 : calm + 1;
                MuteShotAudio();
                yield return null;
            }
            _home = HomeFace.Poker; _splash = true;
            _pokerPayOpen = true; _pokerPayAnim = 1f;
            yield return new WaitForSecondsRealtime(1.4f);
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            SaveFrame(PayHeadShotDir + "/paytable-" + tag + ".png");
            var b = _payHeadDbg;
            var p = _payHeadPanelDbg;
            var t = _payHeadTabDbg;
            float rowsEnd = _payHeadRowsDbg.x + _payHeadRowsDbg.y * BirdPoker.PayTableRows.Length;
            bool titleFits = _payHeadTitleSzDbg.x <= b.Title.width && _payHeadTitleSzDbg.y <= b.Title.height + 1f;
            bool betFits = _payHeadBetSzDbg.x <= b.Bet.width && _payHeadBetSzDbg.y <= b.Bet.height + 1f;
            string line = tag + " " + Screen.width + "x" + Screen.height
                + " titlePx " + _payHeadPxDbg.x + " betPx " + _payHeadPxDbg.y
                + " title " + b.Title + " tsz " + _payHeadTitleSzDbg
                + " bet " + b.Bet + " bsz " + _payHeadBetSzDbg
                + " rule " + b.RuleY.ToString("0") + " rowsTop " + _payHeadRowsDbg.x.ToString("0")
                + " rowH " + _payHeadRowsDbg.y.ToString("0.0") + " rowsEnd " + rowsEnd.ToString("0")
                + " panel " + p + " tab " + t + " floor " + _payHeadFloorDbg.ToString("0")
                + " titleFits " + titleFits + " betFits " + betFits
                + " clearTab " + (p.yMin >= t.yMax) + " rowsInPanel " + (rowsEnd <= p.yMax)
                + " aboveFloor " + (p.yMax <= _payHeadFloorDbg + 0.5f);
            File.AppendAllText(log, line + "\n");
            Debug.Log("[payhead] " + line);
            _pokerPayOpen = false; _pokerPayAnim = 0f;
        }
    }
}
#endif
