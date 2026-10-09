using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // One consent path for iOS and Android, before Ads.Boot.
    //
    // Google UMP is not in this project (no GoogleMobileAds / UserMessagingPlatform
    // assembly). LevelPlay 9.5.1 does not show a consent form; it only receives
    // the result. The prompt below is the in-game form, and only where a choice
    // is required.
    //
    // Region comes from the device locale, not from IP and not from the store
    // country. CurrentCulture and CurrentUICulture are read, and a region is
    // taken only when that culture is specific (de-DE, en-GB). A neutral culture
    // (de, en) has no region. If either locale is EEA or UK, that wins and the
    // prompt is required. A US locale is the United States: the locale cannot
    // see a state, so the whole US is in scope for the CCPA do_not_sell flag.
    // Any other real region skips the prompt. If neither locale yields a region,
    // the result is Unknown and the prompt is required. Missing data is not
    // treated as "outside the EEA".
    //
    // A stored choice skips the prompt on every later launch. CCPA is opt-out:
    // with no stored denial, a US player is do_not_sell false. A stored denial
    // maps to do_not_sell true. Granted consent maps to false.
    //
    // iOS then waits for the ATT callback (Tracking / FlockFiveTracking.mm)
    // before LevelPlay.Init. Editor and Android skip ATT. A timeout still
    // inits if the callback never arrives.
    public static class AdConsent
    {
        public const string ChoiceKey = "flockfive.ads.consent";
        public const float AttTimeoutSeconds = 45f;

        public enum RegionClass
        {
            Unknown = 0,
            EeaUk = 1,
            UnitedStates = 2,
            Other = 3
        }

        public readonly struct Plan
        {
            public readonly bool Prompt;
            public readonly bool Ready;
            public readonly bool Consent;
            public readonly bool ApplyDoNotSell;
            public readonly string DoNotSell;

            public Plan(bool prompt, bool ready, bool consent, bool applyDoNotSell, string doNotSell)
            {
                Prompt = prompt;
                Ready = ready;
                Consent = consent;
                ApplyDoNotSell = applyDoNotSell;
                DoNotSell = doNotSell;
            }
        }

        public readonly struct Gate
        {
            public readonly bool Init;
            public readonly bool FromCallback;
            public readonly bool FromTimeout;

            public Gate(bool init, bool fromCallback, bool fromTimeout)
            {
                Init = init;
                FromCallback = fromCallback;
                FromTimeout = fromTimeout;
            }
        }

        // ISO 3166-1 alpha-2. UK is accepted as an alias of GB. EEA plus the UK.
        static readonly HashSet<string> EeaUk = new HashSet<string>
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR",
            "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK",
            "SI", "ES", "SE", "IS", "LI", "NO", "GB"
        };

        static bool _running;
        static bool _prompt;
        static bool _promptDone;
        static bool _promptYes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _running = false;
            _prompt = false;
            _promptDone = false;
            _promptYes = false;
        }

        public static RegionClass Classify(string regionCode)
        {
            if (string.IsNullOrWhiteSpace(regionCode)) return RegionClass.Unknown;
            string c = regionCode.Trim().ToUpperInvariant();
            if (c == "UK") c = "GB";
            if (c.Length != 2) return RegionClass.Unknown;
            if (c == "US") return RegionClass.UnitedStates;
            if (EeaUk.Contains(c)) return RegionClass.EeaUk;
            if (c[0] < 'A' || c[0] > 'Z' || c[1] < 'A' || c[1] > 'Z') return RegionClass.Unknown;
            return RegionClass.Other;
        }

        // Null when the culture is missing, invariant, neutral, or not a real culture.
        public static string RegionOf(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName)) return null;
            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
                if (culture == null || culture.Equals(CultureInfo.InvariantCulture) || culture.IsNeutralCulture)
                    return null;
                return new RegionInfo(culture.Name).TwoLetterISORegionName;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        // EEA/UK wins over US, US wins over a plain other region, and two
        // missing regions stay Unknown so the prompt still runs.
        public static RegionClass FromCultures(string cultureName, string uiCultureName)
        {
            RegionClass a = Classify(RegionOf(cultureName));
            RegionClass b = Classify(RegionOf(uiCultureName));
            if (a == RegionClass.EeaUk || b == RegionClass.EeaUk) return RegionClass.EeaUk;
            if (a == RegionClass.UnitedStates || b == RegionClass.UnitedStates) return RegionClass.UnitedStates;
            if (a == RegionClass.Other || b == RegionClass.Other) return RegionClass.Other;
            return RegionClass.Unknown;
        }

        public static RegionClass DeviceRegion()
        {
            string culture = CultureInfo.CurrentCulture != null ? CultureInfo.CurrentCulture.Name : null;
            string ui = CultureInfo.CurrentUICulture != null ? CultureInfo.CurrentUICulture.Name : null;
            return FromCultures(culture, ui);
        }

        // Granted personalized ads → do_not_sell false. A denial → true.
        public static string DoNotSellValue(bool consentGranted)
        {
            return consentGranted ? "false" : "true";
        }

        public static Plan Decide(RegionClass region, bool hasChoice, bool choiceGranted)
        {
            if (hasChoice)
            {
                bool us = region == RegionClass.UnitedStates;
                return new Plan(false, true, choiceGranted, us, us ? DoNotSellValue(choiceGranted) : null);
            }
            if (region == RegionClass.EeaUk || region == RegionClass.Unknown)
                return new Plan(true, false, false, false, null);
            if (region == RegionClass.UnitedStates)
                return new Plan(false, true, true, true, DoNotSellValue(true));
            return new Plan(false, true, true, false, null);
        }

        // Init only after the ATT callback, or after the timeout. A callback
        // wins when both are true so the reason stays the player's answer.
        public static Gate EvaluateAtt(float elapsedSeconds, bool callbackArrived, float timeoutSeconds)
        {
            if (timeoutSeconds <= 0f) timeoutSeconds = AttTimeoutSeconds;
            if (callbackArrived) return new Gate(true, true, false);
            if (elapsedSeconds >= timeoutSeconds) return new Gate(true, false, true);
            return new Gate(false, false, false);
        }

        // Editor and Android pass attRequired false and boot as soon as consent
        // is ready. iOS boots only once the ATT gate says init.
        public static bool MayBoot(bool consentReady, bool attRequired, Gate att)
        {
            if (!consentReady) return false;
            if (!attRequired) return true;
            return att.Init;
        }

        public static bool HasChoice()
        {
            return PlayerPrefs.HasKey(ChoiceKey);
        }

        public static bool SavedConsent()
        {
            return PlayerPrefs.GetInt(ChoiceKey, 0) == 1;
        }

        public static void SaveChoice(bool granted)
        {
            PlayerPrefs.SetInt(ChoiceKey, granted ? 1 : 0);
            PlayerPrefs.Save();
        }

        internal static void Begin(AdsHost host)
        {
            if (host == null || _running) return;
            _running = true;
            host.StartCoroutine(Run(host));
        }

        static IEnumerator Run(AdsHost host)
        {
            try
            {
                RegionClass region = DeviceRegion();
                bool has = HasChoice();
                bool granted = has && SavedConsent();
                Plan plan = Decide(region, has, granted);
                if (plan.Prompt)
                {
                    _prompt = true;
                    _promptDone = false;
                    while (!_promptDone) yield return null;
                    granted = _promptYes;
                    SaveChoice(granted);
                    plan = Decide(region, true, granted);
                    _prompt = false;
                }

                var att = new AttSession();
                bool attRequired = Tracking.AttRequired;
                if (attRequired)
                    yield return Tracking.WaitForAds(att);

                if (!MayBoot(plan.Ready, attRequired, att.Gate)) yield break;
                ApplyToLevelPlay(plan);
                if (host != null) host.Boot();
            }
            finally
            {
                _prompt = false;
                _running = false;
            }
        }

        public static void ApplyToLevelPlay(Plan plan)
        {
            if (!plan.Ready) return;
#pragma warning disable CS0618 // 9.5.1 still exposes SetConsent; it forwards to the GDPR consent flag.
            LevelPlay.SetConsent(plan.Consent);
#pragma warning restore CS0618
            if (!plan.ApplyDoNotSell || string.IsNullOrEmpty(plan.DoNotSell)) return;
            LevelPlay.SetMetaData("do_not_sell", plan.DoNotSell);
            // 9.4+ also reads the CCPA flag. Same meaning: true is opt-out.
            LevelPlayPrivacySettings.SetCCPA(plan.DoNotSell == "true");
        }

        public static void DrawPrompt()
        {
            if (!_prompt || _promptDone) return;
            float s = Screen.width / 400f;
            if (s < 0.5f) s = 0.5f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = 400f;
            float h = Screen.height / Mathf.Max(s, 0.01f);
            Color prev = GUI.color;
            GUI.color = new Color(0.05f, 0.12f, 0.08f, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = new Color(0.96f, 0.93f, 0.82f);
            var body = new GUIStyle(title) { fontSize = 16 };
            GUI.Label(new Rect(24f, h * 0.28f, w - 48f, 40f), "Ads keep Flock Five free", title);
            GUI.Label(new Rect(24f, h * 0.28f + 48f, w - 48f, 70f), "Allow personalized ads on this device?", body);
            if (GUI.Button(new Rect(24f, h * 0.62f, w - 48f, 52f), "Allow"))
            {
                _promptYes = true;
                _promptDone = true;
            }
            if (GUI.Button(new Rect(24f, h * 0.62f + 64f, w - 48f, 52f), "Not now"))
            {
                _promptYes = false;
                _promptDone = true;
            }
            GUI.color = prev;
            GUI.matrix = old;
        }

        // Shared with Tracking.WaitForAds. Tests drive Tick without a device.
        public sealed class AttSession
        {
            public bool Started;
            public bool Callback;
            public float Elapsed;
            public bool Finished;
            public bool FromCallback;
            public bool FromTimeout;
            public Gate Gate;

            public void Start()
            {
                Started = true;
            }

            public void Tick(float dt, float timeout)
            {
                if (!Started || Finished) return;
                if (dt > 0f) Elapsed += dt;
                Gate = EvaluateAtt(Elapsed, Callback, timeout);
                if (!Gate.Init) return;
                Finished = true;
                FromCallback = Gate.FromCallback;
                FromTimeout = Gate.FromTimeout;
            }
        }
    }
}
