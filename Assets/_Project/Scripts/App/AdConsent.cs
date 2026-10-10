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
    // A stored GDPR choice skips the prompt on every later launch.
    // CCPA is a separate opt-out on the Settings screen, shown to everyone.
    // AdConsent.DoNotSell persists in PlayerPrefs ("ff_do_not_sell"), default
    // off. LevelPlay gets do_not_sell "true" or "false" from that flag before
    // Ads.Boot, and again when the toggle changes. Decide() still fills
    // Plan.DoNotSell from the US consent mapping (no stored denial is "false",
    // a stored denial is "true", granted consent is "false"). The flag that
    // is sent is the stored toggle, not that plan field.
    //
    // The consent UI waits until the first scene has rendered a frame.
    // iOS then waits for the ATT callback (Tracking / FlockFiveTracking.mm)
    // before LevelPlay.Init. The native request runs only once the app is
    // active. Editor and Android skip ATT. The 45s timeout still inits if
    // the callback never arrives.
    public static class AdConsent
    {
        public const string ChoiceKey = "flockfive.ads.consent";
        public const string DoNotSellKey = "ff_do_not_sell";
        public const float AttTimeoutSeconds = 45f;

        // Set at the end of the first rendered frame. Consent stays hidden until then.
        public static bool FirstFrameReady { get; private set; }

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
            FirstFrameReady = false;
        }

        // After the first scene is loaded. The component waits until that frame
        // has rendered, then opens the consent gate.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void WatchFirstScene()
        {
            if (FirstFrameReady) return;
            var go = new GameObject("FFFirstScene");
            if (go == null) return;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<FirstSceneFrame>();
        }

        // One log for the launch smoke test. Later calls do not log again.
        public static void NoteFirstSceneFrame()
        {
            if (FirstFrameReady) return;
            FirstFrameReady = true;
            Debug.Log("FF_FIRST_SCENE_READY");
        }

        // The prompt is drawn only after the first frame, and only when a choice is due.
        public static bool MayShowConsent(bool firstFrameReady, bool wantsPrompt)
        {
            return firstFrameReady && wantsPrompt;
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

        // Player opt-out. Missing key is off, which sends do_not_sell "false".
        public static bool DoNotSell
        {
            get => PlayerPrefs.GetInt(DoNotSellKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(DoNotSellKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyStoredDoNotSell();
            }
        }

        // The string LevelPlay.SetMetaData expects for the stored toggle.
        public static string DoNotSellMeta(bool stored)
        {
            return stored ? "true" : "false";
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
                // Never paint consent on the frame the host was created. The
                // watcher logs FF_FIRST_SCENE_READY once the first frame renders.
                // A few frames of slack still opens the gate if that watcher is gone.
                int guard = 0;
                while (!FirstFrameReady)
                {
                    guard++;
                    if (guard > 2)
                    {
                        NoteFirstSceneFrame();
                        break;
                    }
                    yield return null;
                }

                if (host == null) yield break;
                RegionClass region = DeviceRegion();
                bool has = HasChoice();
                bool granted = has && SavedConsent();
                Plan plan = Decide(region, has, granted);
                if (plan.Prompt)
                {
                    _prompt = true;
                    _promptDone = false;
                    SyncConsentCanvas(true);
                    while (!_promptDone)
                    {
                        if (host == null) yield break;
                        yield return null;
                    }
                    granted = _promptYes;
                    SaveChoice(granted);
                    plan = Decide(region, true, granted);
                    _prompt = false;
                    SyncConsentCanvas(false);
                }

                var att = new AttSession();
                bool attRequired = false;
                try { attRequired = Tracking.AttRequired; }
                catch (System.Exception e)
                {
                    Debug.LogWarning("ATT requirement skipped: " + e.Message);
                }
                if (attRequired)
                    yield return Tracking.WaitForAds(att);

                // The wait returns on the callback or the 45s timeout. If it
                // returned without opening the gate, the timeout still inits.
                if (attRequired && !att.Finished)
                    att.CatchUp(AttTimeoutSeconds, AttTimeoutSeconds);
                if (!MayBoot(plan.Ready, attRequired, att.Gate)) yield break;
                ApplyToLevelPlay(plan);
                // Stored CCPA choice before init. The Settings toggle applies it again later.
                ApplyStoredDoNotSell();
                if (host != null) host.Boot();
            }
            finally
            {
                _prompt = false;
                SyncConsentCanvas(false);
                _running = false;
            }
        }

        // Optional scene canvas. A missing canvas, or a missing Canvas on it,
        // leaves the OnGUI prompt as the form. Neither path may throw.
        static void SyncConsentCanvas(bool show)
        {
            GameObject canvas = null;
            try { canvas = GameObject.Find("ConsentCanvas"); }
            catch (System.Exception) { return; }
            if (canvas == null) return;
            try
            {
                if (canvas.GetComponent<Canvas>() == null) return;
                if (canvas.activeSelf != show) canvas.SetActive(show);
            }
            catch (System.Exception) { }
        }

        public static void ApplyToLevelPlay(Plan plan)
        {
            if (!plan.Ready) return;
            try
            {
#pragma warning disable CS0618 // 9.5.1 still exposes SetConsent; it forwards to the GDPR consent flag.
                LevelPlay.SetConsent(plan.Consent);
#pragma warning restore CS0618
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("LevelPlay consent skipped: " + e.Message);
            }
        }

        // do_not_sell from the stored toggle. Safe when LevelPlay is missing or not inited:
        // the SDK null-checks its bridge, and a native throw is swallowed so launch and the
        // Settings toggle keep going. Metadata is applied before init and again after it.
        public static void ApplyStoredDoNotSell()
        {
            string meta = DoNotSellMeta(DoNotSell);
            if (string.IsNullOrEmpty(meta)) return;
            try
            {
                LevelPlay.SetMetaData("do_not_sell", meta);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("LevelPlay do_not_sell skipped: " + e.Message);
            }
            ApplyCcpa(DoNotSell);
        }

        // com.unity.services.levelplay in this project exposes LevelPlayPrivacySettings.SetCCPA(bool).
        // true is the CCPA opt-out, the same bit as do_not_sell "true". The call is skipped
        // when that method is not on this SDK. A null or unready bridge is caught below.
        static void ApplyCcpa(bool optedOut)
        {
            var method = typeof(LevelPlayPrivacySettings).GetMethod(
                "SetCCPA",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(bool) },
                null);
            if (method == null) return;
            try
            {
                // Direct call so the player build keeps the method. Same guard as the lookup.
                LevelPlayPrivacySettings.SetCCPA(optedOut);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("LevelPlay CCPA skipped: " + e.Message);
            }
        }

        public static void DrawPrompt()
        {
            if (!MayShowConsent(FirstFrameReady, _prompt) || _promptDone) return;
            if (Event.current == null) return;
            GUISkin skin = GUI.skin;
            if (skin == null || skin.label == null || skin.button == null) return;
            if (Texture2D.whiteTexture == null) return;
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
                if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt)) Elapsed += dt;
                CatchUp(Elapsed, timeout);
            }

            // Wall-clock path. A stalled unscaled delta still expires at the timeout.
            public void CatchUp(float elapsedSeconds, float timeout)
            {
                if (!Started || Finished) return;
                if (!float.IsNaN(elapsedSeconds) && !float.IsInfinity(elapsedSeconds) && elapsedSeconds > Elapsed)
                    Elapsed = elapsedSeconds;
                Gate = EvaluateAtt(Elapsed, Callback, timeout);
                if (!Gate.Init) return;
                Finished = true;
                FromCallback = Gate.FromCallback;
                FromTimeout = Gate.FromTimeout;
            }
        }
    }

    // Renders nothing. Marks the first scene frame so consent can open.
    sealed class FirstSceneFrame : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            AdConsent.NoteFirstSceneFrame();
            Destroy(gameObject);
        }
    }
}
