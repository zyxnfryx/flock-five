#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 77. Release ads stay off the test suite. Consent, ATT ordering,
    // the privacy manifest, and the AppLovin / Meta adapter lists are checked here.
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
            var waiting = new AdConsent.AttSession();
            waiting.Start();
            waiting.Tick(1f, 10f);
            bool early = waiting.Finished;

            var answered = new AdConsent.AttSession();
            answered.Start();
            answered.Tick(0.2f, 10f);
            answered.Callback = true;
            answered.Tick(0f, 10f);

            var timed = new AdConsent.AttSession();
            timed.Start();
            timed.Tick(9.9f, 10f);
            bool before = timed.Finished;
            timed.Tick(0.2f, 10f);

            var idle = new AdConsent.AttSession();
            idle.Callback = true;
            idle.Tick(5f, 10f);

            var gateCallback = AdConsent.EvaluateAtt(0f, true, 10f);
            var gateTimeout = AdConsent.EvaluateAtt(10f, false, 10f);
            var gateWait = AdConsent.EvaluateAtt(9.9f, false, 10f);

            Check("att-order",
                !early
                && answered.Finished && answered.FromCallback && !answered.FromTimeout
                && !before && timed.Finished && timed.FromTimeout && !timed.FromCallback
                && !idle.Finished
                && gateCallback.Init && gateCallback.FromCallback
                && gateTimeout.Init && gateTimeout.FromTimeout
                && !gateWait.Init,
                "callback=" + answered.FromCallback + " timeout=" + timed.FromTimeout);

            Check("boot-after-consent-and-att",
                !AdConsent.MayBoot(false, true, gateCallback)
                && AdConsent.MayBoot(true, false, gateWait)
                && !AdConsent.MayBoot(true, true, gateWait)
                && AdConsent.MayBoot(true, true, gateCallback)
                && AdConsent.MayBoot(true, true, gateTimeout)
                && !Tracking.AttRequired,
                "editor att=" + Tracking.AttRequired);
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
                && domains.Contains("applovin.com")
                && domains.Contains("facebook.com");
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
            bool applovin = once.Contains("ludvb6z3bs.skadnetwork");
            bool metaA = once.Contains("v9wttpbfk9.skadnetwork");
            bool metaB = once.Contains("n38lu8286q.skadnetwork");

            string dir = SkAdNetworkIds.Folder;
            var baseline = new List<string>();
            baseline.AddRange(SkAdNetworkIds.Parse(File.ReadAllText(Path.Combine(dir, "skadnetworks.ironsource.plist.xml"))));
            baseline.AddRange(SkAdNetworkIds.Parse(File.ReadAllText(Path.Combine(dir, "skadnetworks.unityads.plist.xml"))));
            int baseCount = SkAdNetworkIds.Dedup(baseline).Count;

            var forced = SkAdNetworkIds.Dedup(new[]
            {
                "v9wttpbfk9.skadnetwork",
                "V9WTTPBFK9.skadnetwork",
                "ludvb6z3bs.skadnetwork"
            });

            Check("skad-applovin-meta",
                !dup && applovin && metaA && metaB && merged.Count > baseCount && baseCount == 82 && forced.Count == 2,
                "n=" + merged.Count + " base=" + baseCount + " dup=" + dup + " forced=" + forced.Count);
        }

        static void CheckDeps(System.Action<string, bool, string> Check)
        {
            string editor = Path.Combine(Application.dataPath, "LevelPlay/Editor");
            string app = File.ReadAllText(Path.Combine(editor, "ISAppLovinAdapterDependencies.xml"));
            string meta = File.ReadAllText(Path.Combine(editor, "ISFacebookAdapterDependencies.xml"));
            bool appOk = app.Contains("5.12.0.0")
                && app.Contains("com.unity3d.ads-mediation:applovin-adapter:5.9.0")
                && app.Contains("com.applovin:applovin-sdk:13.6.4")
                && app.Contains("IronSourceAppLovinAdapter")
                && app.Contains("5.9.0.0");
            bool metaOk = meta.Contains("5.7.0.0")
                && meta.Contains("com.unity3d.ads-mediation:facebook-adapter:5.4.0")
                && meta.Contains("com.facebook.android:audience-network-sdk:6.22.0")
                && meta.Contains("IronSourceFacebookAdapter")
                && meta.Contains("5.4.0.0");
            Check("adapter-xml", appOk && metaOk, "applovin=" + appOk + " meta=" + metaOk);
        }
    }
}
#endif
