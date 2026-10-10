#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 77, plus the build 78 hotfix. Release ads stay off the test suite.
    // Consent waits for the first rendered frame. Ads boot without waiting on
    // ATT. The native ATT request, when it is asked later, still waits until
    // the app is active. AppLovin and Meta stay out of the build.
    static class Build77AdsTests
    {
        [MenuItem("Flock Five/Build 77 Ads Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build77-ads] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Check("test-suite-off", !Ads.TestSuite, "TestSuite=" + Ads.TestSuite);
            CheckDevOnly(Check);
            CheckConsent(Check);
            CheckAtt(Check);
            CheckPrivacy(Check);
            CheckSkAd(Check);
            CheckDeps(Check);

            Debug.Log("[build77-ads] " + (fail == 0 ? "BUILD77_ADS_OK " + pass : "BUILD77_ADS_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD77_ADS_FAIL " + fail + " passed " + pass);
        }

        static void CheckDevOnly(System.Action<string, bool, string> Check)
        {
            string path = Path.Combine(Application.dataPath, "_Project/Scripts/App/Ads.cs");
            string src = File.ReadAllText(path);
            string leaked = DevOnlyLeaks(src);
            Check("dev-tools-ifdef", leaked == null, leaked ?? "is_test_suite, 3-finger suite, 4-finger log");

            string project = Path.GetDirectoryName(Application.dataPath);
            string settings = Path.Combine(project, "ProjectSettings/ProjectSettings.asset");
            string body = File.Exists(settings) ? File.ReadAllText(settings) : "";
            Check("define-not-in-player-settings", body.IndexOf("FF_ADS_DEV") < 0, "ProjectSettings");
        }

        static string DevOnlyLeaks(string source)
        {
            string[] markers =
            {
                "SetMetaData(\"is_test_suite\"",
                "LaunchTestSuite(",
                "_showLog"
            };
            var stack = new List<bool>();
            bool parent = false;
            bool inDev = false;
            var lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("#if"))
                {
                    stack.Add(parent);
                    parent = inDev;
                    inDev = inDev || t.IndexOf("FF_ADS_DEV") >= 0;
                    continue;
                }
                if (t.StartsWith("#else") && stack.Count > 0)
                {
                    inDev = parent;
                    continue;
                }
                if (t.StartsWith("#endif") && stack.Count > 0)
                {
                    inDev = parent;
                    parent = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                if (inDev) continue;
                for (int m = 0; m < markers.Length; m++)
                {
                    if (t.IndexOf(markers[m]) >= 0)
                        return "line " + (i + 1) + " " + markers[m];
                }
            }
            return null;
        }

        static void CheckConsent(System.Action<string, bool, string> Check)
        {
            var eea = AdConsent.Decide(AdConsent.RegionClass.EeaUk, false, false);
            var unknown = AdConsent.Decide(AdConsent.RegionClass.Unknown, false, false);
            var storedNo = AdConsent.Decide(AdConsent.RegionClass.EeaUk, true, false);
            var storedYes = AdConsent.Decide(AdConsent.RegionClass.EeaUk, true, true);
            Check("eea-prompt",
                eea.Prompt && !eea.Ready && unknown.Prompt && !unknown.Ready,
                "eea=" + eea.Prompt + " unknown=" + unknown.Prompt);

            Check("persisted-skips-prompt",
                !storedNo.Prompt && storedNo.Ready && !storedNo.Consent
                && !storedYes.Prompt && storedYes.Ready && storedYes.Consent,
                "no=" + storedNo.Consent + " yes=" + storedYes.Consent);

            var usGrant = AdConsent.Decide(AdConsent.RegionClass.UnitedStates, true, true);
            var usDeny = AdConsent.Decide(AdConsent.RegionClass.UnitedStates, true, false);
            var usDefault = AdConsent.Decide(AdConsent.RegionClass.UnitedStates, false, false);
            Check("us-do-not-sell",
                AdConsent.DoNotSellValue(true) == "false"
                && AdConsent.DoNotSellValue(false) == "true"
                && usGrant.ApplyDoNotSell && usGrant.DoNotSell == "false"
                && usDeny.ApplyDoNotSell && usDeny.DoNotSell == "true"
                && !usDefault.Prompt && usDefault.ApplyDoNotSell && usDefault.DoNotSell == "false",
                "grant=" + usGrant.DoNotSell + " deny=" + usDeny.DoNotSell + " default=" + usDefault.DoNotSell);

            var other = AdConsent.Decide(AdConsent.RegionClass.Other, false, false);
            Check("other-no-prompt", !other.Prompt && other.Ready && other.Consent && !other.ApplyDoNotSell, "other");

            Check("consent-after-first-frame",
                !AdConsent.MayShowConsent(false, true)
                && AdConsent.MayShowConsent(true, true)
                && !AdConsent.MayShowConsent(true, false)
                && ConsentWaitsForFirstFrame(),
                "first=" + AdConsent.FirstFrameReady);

            Check("region-eea",
                AdConsent.Classify("DE") == AdConsent.RegionClass.EeaUk
                && AdConsent.Classify("gb") == AdConsent.RegionClass.EeaUk
                && AdConsent.Classify("UK") == AdConsent.RegionClass.EeaUk
                && AdConsent.Classify("NO") == AdConsent.RegionClass.EeaUk
                && AdConsent.FromCultures("de-DE", "en-US") == AdConsent.RegionClass.EeaUk
                && AdConsent.RegionOf("de-DE") == "DE",
                AdConsent.RegionOf("de-DE") + " " + AdConsent.FromCultures("de-DE", "en-US"));

            Check("region-us-other-unknown",
                AdConsent.Classify("US") == AdConsent.RegionClass.UnitedStates
                && AdConsent.FromCultures("en-US", "ja-JP") == AdConsent.RegionClass.UnitedStates
                && AdConsent.Classify("JP") == AdConsent.RegionClass.Other
                && AdConsent.RegionOf("en") == null
                && AdConsent.FromCultures("en", "fr") == AdConsent.RegionClass.Unknown
                && AdConsent.Classify("") == AdConsent.RegionClass.Unknown
                && AdConsent.Classify(null) == AdConsent.RegionClass.Unknown,
                "us/jp/neutral");
        }

        static void CheckAtt(System.Action<string, bool, string> Check)
        {
            var gate = default(AdConsent.Gate);
            Check("boot-without-att-wait",
                !AdConsent.MayBoot(false, true, gate)
                && AdConsent.MayBoot(true, true, gate)
                && AdConsent.MayBoot(true, false, gate)
                && !AdConsent.Personalized(true, true, 0)
                && AdConsent.Personalized(true, true, 3)
                && AdConsent.Personalized(true, false, 0)
                && !Tracking.AttRequired
                && !LaunchWaitsForAtt(),
                "editor att=" + Tracking.AttRequired);

            Check("att-after-active",
                AttWaitsForActive() && !LaunchWaitsForAtt(),
                "native active, no launch wait");

            bool nullSafe = true;
            try
            {
                var ready = AdConsent.Decide(AdConsent.RegionClass.Other, false, false);
                Tracking.NoteComplete(null);
                AdConsent.DrawPrompt();
                AdConsent.ApplyToLevelPlay(ready);
                AdConsent.ApplyToLevelPlay(default(AdConsent.Plan));
            }
            catch (System.Exception e)
            {
                nullSafe = false;
                Check("null-callbacks", false, e.GetType().Name + " " + e.Message);
            }
            if (nullSafe) Check("null-callbacks", true, "consent, LevelPlay, callback");
        }

        static bool ConsentWaitsForFirstFrame()
        {
            string path = Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs");
            string src = File.ReadAllText(path);
            int wait = src.IndexOf("while (!FirstFrameReady)");
            int prompt = src.IndexOf("_prompt = true");
            int log = src.IndexOf("Debug.Log(\"FF_FIRST_SCENE_READY\")");
            int end = src.IndexOf("WaitForEndOfFrame");
            return wait >= 0 && prompt > wait && log > 0 && end > 0
                && src.IndexOf("ConsentCanvas") > 0;
        }

        static bool AttWaitsForActive()
        {
            string path = Path.Combine(Application.dataPath, "Plugins/iOS/FlockFiveTracking.mm");
            string src = File.ReadAllText(path);
            int fn = src.IndexOf("void FlockFive_RequestTracking(void)");
            int note = src.IndexOf("UIApplicationDidBecomeActiveNotification");
            int call = src.IndexOf("requestTrackingAuthorizationWithCompletionHandler");
            if (fn < 0) return false;
            string body = src.Substring(fn);
            return note >= 0 && note < fn
                && call >= 0 && call < fn
                && body.IndexOf("UIApplicationStateActive") >= 0
                && body.IndexOf("FlockFive_WaitUntilActive") >= 0
                && body.IndexOf("requestTrackingAuthorizationWithCompletionHandler") < 0;
        }

        // The launch path must not wait on ATT or keep the old 45s gate.
        static bool LaunchWaitsForAtt()
        {
            string consent = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs"));
            string tracking = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Tracking.cs"));
            return consent.IndexOf("WaitForAds") >= 0
                || consent.IndexOf("AttTimeoutSeconds") >= 0
                || tracking.IndexOf("AttTimeoutSeconds") >= 0
                || tracking.IndexOf("realtimeSinceStartup") >= 0
                || tracking.IndexOf("AskOnce") >= 0;
        }

        static void CheckPrivacy(System.Action<string, bool, string> Check)
        {
            string path = Path.Combine(Application.dataPath, "Plugins/iOS/PrivacyInfo.xcprivacy");
            var doc = new XmlDocument();
            doc.Load(path);
            bool tracking = false;
            var types = new Dictionary<string, XmlElement>();
            var reasons = new List<string>();
            var domains = new List<string>();
            foreach (XmlElement key in doc.GetElementsByTagName("key"))
            {
                string name = key.InnerText.Trim();
                XmlElement val = NextElement(key);
                if (val == null) continue;
                if (name == "NSPrivacyTracking")
                    tracking = val.Name == "true";
                if (name == "NSPrivacyTrackingDomains")
                {
                    foreach (XmlElement s in val.GetElementsByTagName("string"))
                        domains.Add(s.InnerText.Trim());
                }
                if (name == "NSPrivacyCollectedDataType")
                {
                    XmlElement dict = key.ParentNode as XmlElement;
                    if (dict != null) types[val == null ? name : key.InnerText] = dict;
                }
                if (name == "NSPrivacyAccessedAPITypeReasons")
                {
                    foreach (XmlElement s in val.GetElementsByTagName("string"))
                        reasons.Add(s.InnerText.Trim());
                }
            }

            // The data-type key's value is the following string, and the dict is the parent.
            types.Clear();
            foreach (XmlElement key in doc.GetElementsByTagName("key"))
            {
                if (key.InnerText.Trim() != "NSPrivacyCollectedDataType") continue;
                XmlElement val = NextElement(key);
                XmlElement dict = key.ParentNode as XmlElement;
                if (val != null && dict != null) types[val.InnerText.Trim()] = dict;
            }

            bool device = DataOk(types, "NSPrivacyCollectedDataTypeDeviceID");
            bool ads = DataOk(types, "NSPrivacyCollectedDataTypeAdvertisingData");
            bool product = DataOk(types, "NSPrivacyCollectedDataTypeProductInteraction");
            Check("privacy-tracking",
                tracking && device && ads && product,
                "tracking=" + tracking + " device=" + device + " ads=" + ads + " product=" + product);

            bool domainOk = domains.Contains("supersonicads.com")
                && domains.Contains("unityads.unity3d.com")
                && !domains.Contains("applovin.com")
                && !domains.Contains("facebook.com");
            Check("privacy-domains", domainOk, string.Join(",", domains.ToArray()));

            bool reasonsOk = reasons.Contains("CA92.1") && reasons.Contains("C617.1")
                && reasons.Contains("E174.1") && reasons.Contains("35F9.1");
            Check("privacy-api-reasons", reasonsOk, string.Join(",", reasons.ToArray()));
        }

        static bool DataOk(Dictionary<string, XmlElement> types, string typeName)
        {
            XmlElement dict;
            if (!types.TryGetValue(typeName, out dict) || dict == null) return false;
            bool linked = false;
            bool tracking = false;
            bool purpose = false;
            foreach (XmlElement key in dict.GetElementsByTagName("key"))
            {
                if (key.ParentNode != dict) continue;
                XmlElement val = NextElement(key);
                if (val == null) continue;
                string name = key.InnerText.Trim();
                if (name == "NSPrivacyCollectedDataTypeLinked") linked = val.Name == "true";
                if (name == "NSPrivacyCollectedDataTypeTracking") tracking = val.Name == "true";
                if (name == "NSPrivacyCollectedDataTypePurposes")
                {
                    foreach (XmlElement s in val.GetElementsByTagName("string"))
                    {
                        if (s.InnerText.Trim() == "NSPrivacyCollectedDataTypePurposeThirdPartyAdvertising")
                            purpose = true;
                    }
                }
            }
            return linked && tracking && purpose;
        }

        static XmlElement NextElement(XmlNode node)
        {
            for (XmlNode n = node.NextSibling; n != null; n = n.NextSibling)
            {
                var el = n as XmlElement;
                if (el != null) return el;
            }
            return null;
        }

        static void CheckSkAd(System.Action<string, bool, string> Check)
        {
            var merged = SkAdNetworkIds.LoadMerged();
            var once = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            bool dup = false;
            for (int i = 0; i < merged.Count; i++)
            {
                if (!once.Add(merged[i])) dup = true;
            }
            bool applovinOnly = once.Contains("ludvb6z3bs.skadnetwork");
            bool metaOnly = once.Contains("n38lu8286q.skadnetwork");

            string dir = SkAdNetworkIds.Folder;
            var baseline = new List<string>();
            baseline.AddRange(SkAdNetworkIds.Parse(File.ReadAllText(Path.Combine(dir, "skadnetworks.ironsource.plist.xml"))));
            baseline.AddRange(SkAdNetworkIds.Parse(File.ReadAllText(Path.Combine(dir, "skadnetworks.unityads.plist.xml"))));
            int baseCount = SkAdNetworkIds.Dedup(baseline).Count;
            bool droppedLists = !File.Exists(Path.Combine(dir, "skadnetworks.applovin.plist.xml"))
                && !File.Exists(Path.Combine(dir, "skadnetworks.meta.plist.xml"));

            var forced = SkAdNetworkIds.Dedup(new[]
            {
                "v9wttpbfk9.skadnetwork",
                "V9WTTPBFK9.skadnetwork",
                "ludvb6z3bs.skadnetwork"
            });

            Check("skad-unity-ironsource",
                !dup && droppedLists && !applovinOnly && !metaOnly
                && merged.Count == baseCount && baseCount == 82 && forced.Count == 2,
                "n=" + merged.Count + " base=" + baseCount + " dup=" + dup + " forced=" + forced.Count);
        }

        static void CheckDeps(System.Action<string, bool, string> Check)
        {
            string editor = Path.Combine(Application.dataPath, "LevelPlay/Editor");
            bool noXml = !File.Exists(Path.Combine(editor, "ISAppLovinAdapterDependencies.xml"))
                && !File.Exists(Path.Combine(editor, "ISFacebookAdapterDependencies.xml"));
            bool keptXml = File.Exists(Path.Combine(editor, "IronSourceSDKDependencies.xml"))
                && File.Exists(Path.Combine(editor, "ISUnityAdsAdapterDependencies.xml"));
            string gradlePath = Path.Combine(Application.dataPath, "Plugins/Android/mainTemplate.gradle");
            string gradle = File.Exists(gradlePath) ? File.ReadAllText(gradlePath) : "";
            string g = gradle.ToLowerInvariant();
            bool gradleClean = g.IndexOf("applovin") < 0
                && g.IndexOf("facebook") < 0
                && g.IndexOf("audience-network") < 0;
            bool gradleKept = gradle.Contains("com.unity3d.ads:unity-ads:")
                && gradle.Contains("com.unity3d.ads-mediation:mediation-sdk:")
                && gradle.Contains("com.unity3d.ads-mediation:unityads-adapter:");
            Check("adapter-xml", noXml && keptXml && gradleClean && gradleKept,
                "xml=" + noXml + " kept=" + keptXml + " gradle=" + gradleClean);
        }
    }
}
#endif
