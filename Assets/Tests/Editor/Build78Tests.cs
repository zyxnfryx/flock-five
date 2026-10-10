#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 78. AppLovin and Meta stay out. The Settings Do Not Sell toggle
    // is the stored CCPA flag, applied before LevelPlay init.
    static class Build78Tests
    {
        [MenuItem("Flock Five/Build 78 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build78] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckDoNotSell(Check);
            CheckBeforeBoot(Check);
            CheckNetworks(Check);
            CheckSettings(Check);

            Debug.Log("[build78] " + (fail == 0 ? "BUILD78_OK " + pass : "BUILD78_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD78_FAIL " + fail + " passed " + pass);
        }

        static void CheckDoNotSell(System.Action<string, bool, string> Check)
        {
            bool had = PlayerPrefs.HasKey(AdConsent.DoNotSellKey);
            int raw = PlayerPrefs.GetInt(AdConsent.DoNotSellKey, 0);
            try
            {
                PlayerPrefs.DeleteKey(AdConsent.DoNotSellKey);
                Check("default-off",
                    !AdConsent.DoNotSell && AdConsent.DoNotSellMeta(AdConsent.DoNotSell) == "false",
                    "missing key");

                AdConsent.DoNotSell = true;
                bool on = AdConsent.DoNotSell
                    && PlayerPrefs.GetInt(AdConsent.DoNotSellKey, 0) == 1
                    && AdConsent.DoNotSellMeta(true) == "true"
                    && AdConsent.DoNotSellMeta(AdConsent.DoNotSell) == "true";
                Check("toggle-on-persists", on, "key=" + PlayerPrefs.GetInt(AdConsent.DoNotSellKey, -1));

                AdConsent.DoNotSell = false;
                bool off = !AdConsent.DoNotSell
                    && PlayerPrefs.HasKey(AdConsent.DoNotSellKey)
                    && PlayerPrefs.GetInt(AdConsent.DoNotSellKey, 1) == 0
                    && AdConsent.DoNotSellMeta(false) == "false"
                    && AdConsent.DoNotSellMeta(AdConsent.DoNotSell) == "false";
                Check("toggle-off-persists", off, "key=" + PlayerPrefs.GetInt(AdConsent.DoNotSellKey, -1));
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(AdConsent.DoNotSellKey, raw);
                else PlayerPrefs.DeleteKey(AdConsent.DoNotSellKey);
                PlayerPrefs.Save();
            }

            bool safe = true;
            try
            {
                AdConsent.ApplyStoredDoNotSell();
                AdConsent.ApplyToLevelPlay(default(AdConsent.Plan));
            }
            catch (System.Exception e)
            {
                safe = false;
                Check("apply-null-safe", false, e.GetType().Name + " " + e.Message);
            }
            if (safe) Check("apply-null-safe", true, "missing sdk and empty plan");
        }

        static void CheckBeforeBoot(System.Action<string, bool, string> Check)
        {
            string path = Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs");
            string src = File.ReadAllText(path);
            int run = src.IndexOf("static IEnumerator Run");
            int apply = run < 0 ? -1 : src.IndexOf("ApplyStoredDoNotSell()", run);
            int boot = run < 0 ? -1 : src.IndexOf("host.Boot()", run);
            int meta = src.IndexOf("SetMetaData(\"do_not_sell\", meta)");
            int hardcoded = src.IndexOf("SetMetaData(\"do_not_sell\", \"false\")");
            int ccpa = src.IndexOf("LevelPlayPrivacySettings.SetCCPA(optedOut)");
            int lookup = src.IndexOf("GetMethod(");
            bool before = run >= 0 && apply >= 0 && boot > apply && meta > 0 && hardcoded < 0 && ccpa > 0 && lookup > 0;
            Check("do-not-sell-before-boot", before,
                "apply=" + apply + " boot=" + boot + " meta=" + meta + " hardcoded=" + hardcoded);
        }

        static void CheckNetworks(System.Action<string, bool, string> Check)
        {
            string privacyPath = Path.Combine(Application.dataPath, "Plugins/iOS/PrivacyInfo.xcprivacy");
            string privacy = File.Exists(privacyPath) ? File.ReadAllText(privacyPath).ToLowerInvariant() : "";
            int domainsAt = privacy.IndexOf("nsprivacytrackingdomains");
            int next = domainsAt < 0 ? -1 : privacy.IndexOf("</array>", domainsAt);
            string domains = domainsAt >= 0 && next > domainsAt ? privacy.Substring(domainsAt, next - domainsAt) : privacy;
            bool domainClean = domains.IndexOf("applovin") < 0
                && domains.IndexOf("applvn") < 0
                && domains.IndexOf("facebook") < 0
                && domains.IndexOf("audience") < 0
                && domains.IndexOf("supersonicads.com") >= 0
                && domains.IndexOf("unityads.unity3d.com") >= 0;

            string gradlePath = Path.Combine(Application.dataPath, "Plugins/Android/mainTemplate.gradle");
            string gradle = File.Exists(gradlePath) ? File.ReadAllText(gradlePath).ToLowerInvariant() : "";
            bool gradleClean = gradle.IndexOf("applovin") < 0
                && gradle.IndexOf("facebook") < 0
                && gradle.IndexOf("audience-network") < 0
                && gradle.IndexOf("com.unity3d.ads:unity-ads:") >= 0
                && gradle.IndexOf("com.unity3d.ads-mediation:mediation-sdk:") >= 0
                && gradle.IndexOf("com.unity3d.ads-mediation:unityads-adapter:") >= 0;

            string editor = Path.Combine(Application.dataPath, "LevelPlay/Editor");
            bool noXml = !File.Exists(Path.Combine(editor, "ISAppLovinAdapterDependencies.xml"))
                && !File.Exists(Path.Combine(editor, "ISFacebookAdapterDependencies.xml"));
            bool podsClean = PodsClean(editor);

            Check("no-applovin-meta", domainClean && gradleClean && noXml && podsClean,
                "domains=" + domainClean + " gradle=" + gradleClean + " xml=" + noXml + " pods=" + podsClean);
        }

        static bool PodsClean(string editor)
        {
            if (!Directory.Exists(editor)) return false;
            string[] files = Directory.GetFiles(editor, "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string body = File.ReadAllText(files[i]).ToLowerInvariant();
                if (body.IndexOf("applovin") >= 0 || body.IndexOf("facebook") >= 0 || body.IndexOf("audience") >= 0)
                    return false;
            }
            return true;
        }

        static void CheckSettings(System.Action<string, bool, string> Check)
        {
            var inner = new Rect(24f, 80f, 360f, 400f);
            var rows = SettingsSheet.Place(inner, 1f);
            float gap = rows.Toggle.yMin - rows.PrivacyLink.yMax;
            bool nextTo = rows.PrivacyLink.xMin == inner.xMin
                && rows.Label.xMin == inner.xMin
                && rows.Toggle.xMax <= inner.xMax + 0.5f
                && rows.Toggle.xMin > rows.Label.xMax
                && rows.Toggle.yMin >= rows.PrivacyLink.yMax
                && gap <= SettingsSheet.RowGap + rows.Toggle.height
                && rows.Toggle.width >= 44f
                && rows.PrivacyLink.width > 100f
                && SettingsSheet.DoNotSellLabel.IndexOf("Do Not Sell or Share My Personal Information") >= 0
                && SettingsSheet.PrivacyLabel == "Privacy Policy";

            // Draw order in the sheet, not the first mention of AdConsent.DoNotSell.
            // The method comment names that flag above the privacy link, so IndexOf
            // on the symbol reports the toggle before the link even though Place
            // seats the switch on the row under it and DrawSettingsSheet paints it.
            string ui = Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs");
            string src = File.Exists(ui) ? File.ReadAllText(ui) : "";
            int link = src.IndexOf("StampOutlined(rows.PrivacyLink, SettingsSheet.PrivacyLabel");
            int label = src.IndexOf("rows.Label, SettingsSheet.DoNotSellLabel");
            int read = src.IndexOf("bool on = AdConsent.DoNotSell");
            int toggle = src.IndexOf("DrawCheckbox(rows.Toggle");
            int mark = src.IndexOf("DrawCheckMark(Inset(rows.Toggle");
            int assign = src.IndexOf("AdConsent.DoNotSell = !on");
            string appPath = Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs");
            string app = File.Exists(appPath) ? File.ReadAllText(appPath) : "";
            bool shown = app.IndexOf("if (_settingsOpen) DrawSettingsSheet(s);") >= 0;
            bool drawn = link >= 0 && label > link && read > label && toggle > read
                && mark > toggle && assign > toggle && shown;

            Check("settings-toggle-next-to-privacy", nextTo && drawn,
                "gap=" + gap + " drawn=" + drawn + " link=" + link + " toggle=" + toggle);
        }
    }
}
#endif
