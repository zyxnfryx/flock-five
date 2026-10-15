#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 89: the Go VIP buy-disc sheen, a frame at the trace start and one mid-sweep,
        // on iPhone, iPad and Android. Full frame plus a crop of the disc.
        const string Dir89GoVipSheen = "/Users/zfxgames/wkspaces/birdshot/b89govip-sheen";
        internal static Rect Shot89BuyDisc;
        internal static Rect Shot89BuyFace;
        public const string GoVipSheenArmKey = "ff.b89.govip-sheen";

        // Env-armed so parallel editors sharing /tmp cannot start or swallow it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ArmGoVipSheenShot()
        {
            if (System.Environment.GetEnvironmentVariable("FF_GOVIP_SHEEN") != "1"
                && string.IsNullOrEmpty(SessionState.GetString(GoVipSheenArmKey, ""))) return;
            var host = new GameObject("ShotArm89GoVip").AddComponent<ShotHost83>();
            Object.DontDestroyOnLoad(host.gameObject);
            host.StartCoroutine(GoVipSheenBody());
        }

        static IEnumerator GoVipSheenBody()
        {
            float t0 = Time.realtimeSinceStartup;
            FlockFiveApp app = null;
            while (app == null && Time.realtimeSinceStartup - t0 < 60f)
            {
                app = Object.FindAnyObjectByType<FlockFiveApp>();
                yield return null;
            }
            var sb = new StringBuilder();
            string tag = SessionState.GetString(GoVipSheenArmKey, "");
            if (tag.Length == 0) tag = System.Environment.GetEnvironmentVariable("FF_GOVIP_TAG") ?? "after";
            SessionState.EraseString(GoVipSheenArmKey);
            Directory.CreateDirectory(Dir89GoVipSheen);
            int noads = PlayerPrefs.GetInt("flockfive.noads", 0);
            ShotKeepAwake = true;
            MuteShotAudio();
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            try
            {
                if (app == null) { sb.AppendLine("no app"); yield break; }
                SetNoAdsOwned(false);
                var devices = new[] { ("iphone", 1179, 2556), ("ipad", 2048, 2732), ("android", 1080, 2400) };
                foreach (var (dev, w, h) in devices)
                {
                    if (EditorGameSize != null) EditorGameSize(w, h);
                    yield return new WaitForSecondsRealtime(1.2f);
                    app.ShowSplash();
                    yield return new WaitForSecondsRealtime(0.8f);
                    VipOffer.Show();
                    yield return new WaitForSecondsRealtime(3.8f);
                    foreach (var (name, lo, hi) in new[] { ("start", 0.08f, 0.11f), ("mid", 0.48f, 0.54f) })
                    {
                        float waited = 0f;
                        float u = PushButton.SheenU(AdPause.Unscaled);
                        while (!(u >= lo && u <= hi) && waited < 6f)
                        {
                            waited += Time.unscaledDeltaTime;
                            MuteShotAudio();
                            yield return null;
                            u = PushButton.SheenU(AdPause.Unscaled);
                        }
                        yield return new WaitForEndOfFrame();
                        u = PushButton.SheenU(AdPause.Unscaled);
                        var tex = ScreenCapture.CaptureScreenshotAsTexture();
                        if (tex == null) { sb.AppendLine(dev + " " + name + " no texture"); continue; }
                        string stem = Dir89GoVipSheen + "/govip-sheen-" + dev + "-" + name + "-" + tag;
                        File.WriteAllBytes(stem + ".png", tex.EncodeToPNG());
                        var d = Shot89BuyDisc;
                        if (d.width > 4f) WriteCrop(tex, d, 768, 0.10f, stem + "-crop.png");
                        sb.AppendLine(dev + " " + name + " u=" + u.ToString("0.000") + " " + tex.width + "x" + tex.height
                            + " disc=" + d.x.ToString("0") + "," + d.y.ToString("0") + " " + d.width.ToString("0") + "x" + d.height.ToString("0")
                            + " edgeGapPx=" + (PushButton.OvalEdgeGap * d.width).ToString("0.0")
                            + " oldEdgeGapPx=" + ((0.5f - 0.47f * Mathf.Sqrt(0.9f)) * d.width).ToString("0.0")
                            + " spillPx=" + SpillAt(Shot89BuyFace, 0, u));
                        Object.Destroy(tex);
                    }
                    VipOffer.Close();
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                sb.AppendLine("GOVIP_SHEEN_DONE");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.noads", noads);
                PlayerPrefs.Save();
                SetNoAdsOwned(noads == 1);
                ShotKeepAwake = false;
                File.WriteAllText(Dir89GoVipSheen + "/result-" + tag + ".txt", sb.ToString());
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        // Sheen pixels outside the traced face at this sweep position, over the drawn fit rect.
        static string SpillAt(Rect drawn, int tex2, float u)
        {
            var bloom = SpriteCatalog.PlayFlower;
            if (drawn.width < 8f || bloom == null || bloom.texture == null) return "n/a";
            var fit = PushButton.FitRect(drawn, bloom.texture.width, bloom.texture.height);
            int bad = PushButton.FaceSpillPixels(fit.width, fit.height, u);
            return bad + " (fit " + fit.width.ToString("0") + "x" + fit.height.ToString("0") + ")";
        }
    }
}
#endif
