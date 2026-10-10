using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
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
    // The GDPR card waits until the first scene has rendered a frame. It is a
    // wood panel that does not cover the LEVEL flower, and it blocks a control
    // only while that card is painted and actually overlaps the control.
    // Ads (LevelPlay) start as soon as GDPR is ready. They do not wait on ATT.
    // Until the player answers ATT, ads are non-personalized.
    //
    // The iOS ATT system prompt is not shown at launch and not during the first
    // gardens. Back on the home screen after the level 2 win, a short in-game
    // pre-prompt asks first. Continue requests the system dialog (only while the
    // app is active, never mid-level, never over a coach). Not now asks once
    // more after a later win, then never.
    public static class AdConsent
    {
        public const string ChoiceKey = "flockfive.ads.consent";
        public const string DoNotSellKey = "ff_do_not_sell";
        public const string AttAskKey = "ff.att.ask";
        public const string AttSnoozeKey = "ff.att.snooze";

        public const string GdprBody = "Ads keep Flock Five free. Allow personalized ads on this device?";
        public const string AttBody = "Ads keep Flock Five free. Allow tracking for ads that fit you better?";
        public const string AllowLabel = "Allow";
        public const string ContinueLabel = "Continue";
        public const string NotNowLabel = "Not now";

        // Set at the end of the first rendered frame. Consent stays hidden until then.
        public static bool FirstFrameReady { get; private set; }

        // Screenshot hold. Forces the ATT pre-prompt without writing prefs.
        public static bool EditorHoldAtt { get; set; }

        public static bool SystemPending => _systemPending;

        public enum RegionClass
        {
            Unknown = 0,
            EeaUk = 1,
            UnitedStates = 2,
            Other = 3
        }

        public enum Sheet
        {
            None = 0,
            Gdpr = 1,
            Att = 2
        }

        public enum AttMoment
        {
            None = 0,
            PrePrompt = 1
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

        public readonly struct HomeCard
        {
            public readonly Sheet Kind;
            public readonly Rect Card;
            public readonly Rect Body;
            public readonly Rect Accept;
            public readonly Rect Decline;

            public HomeCard(Sheet kind, Rect card, Rect body, Rect accept, Rect decline)
            {
                Kind = kind;
                Card = card;
                Body = body;
                Accept = accept;
                Decline = decline;
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
        static bool _transientDismissed;
        static bool _systemPending;
        static bool _painted;
        static Rect _block;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _running = false;
            _prompt = false;
            _promptDone = false;
            _promptYes = false;
            _transientDismissed = false;
            _systemPending = false;
            _painted = false;
            _block = default;
            EditorHoldAtt = false;
            FirstFrameReady = false;
            _smokeProbed = false;
            SmokeArgc = 0;
            SmokeArgv = false;
            SmokeNative = false;
            SmokeEnv = false;
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

        // ATT does not gate the boot. consentReady is the only requirement.
        // attRequired and att stay in the signature so older call sites compile;
        // both are ignored. Personalized ads wait for an ATT answer separately.
        public static bool MayBoot(bool consentReady, bool attRequired, Gate att)
        {
            // ATT is not a boot gate. The parameters stay so existing call sites compile.
            if (!consentReady && attRequired && att.FromTimeout) return false;
            return consentReady;
        }

        // Non-personalized until iOS ATT is authorized (status 3). Editor and
        // Android are not att-required, so the GDPR choice is the ad choice.
        public static bool Personalized(bool gdprConsent, bool attRequired, int attStatus)
        {
            if (!gdprConsent) return false;
            if (!attRequired) return true;
            return attStatus == 3;
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

        // 0 never asked, 1 snoozed once, 2 finished (continued, or the second Not now).
        // Pre-prompt only on the home screen, after the level 2 win (nextPlay >= 2),
        // and never over a coach or another modal. A snooze asks once more after a
        // later win (nextPlay moved past the stored value), then never.
        public static AttMoment Consider(int nextPlay, bool onHome, bool coachOrModal, int ask, int snoozeAt)
        {
            if (!onHome || coachOrModal) return AttMoment.None;
            if (ask >= 2) return AttMoment.None;
            if (nextPlay < 2) return AttMoment.None;
            if (ask <= 0) return AttMoment.PrePrompt;
            if (nextPlay > snoozeAt) return AttMoment.PrePrompt;
            return AttMoment.None;
        }

        public static bool ShouldRequestSystem(bool pending, bool appActive, bool midLevel, bool coachOrModal)
        {
            return pending && appActive && !midLevel && !coachOrModal;
        }

        // Continue on the pre-prompt. Persists "done" and arms the system request.
        // PollSystem performs the native call once the app is active and calm.
        public static void ContinueAtt()
        {
            PlayerPrefs.SetInt(AttAskKey, 2);
            PlayerPrefs.Save();
            _systemPending = true;
            EditorHoldAtt = false;
        }

        // Not now. First tap snoozes at this nextPlay. The next tap finishes forever.
        public static void NotNow(int nextPlay)
        {
            _systemPending = false;
            int ask = PlayerPrefs.GetInt(AttAskKey, 0);
            if (ask >= 2) return;
            if (ask <= 0)
            {
                PlayerPrefs.SetInt(AttAskKey, 1);
                PlayerPrefs.SetInt(AttSnoozeKey, nextPlay);
            }
            else
                PlayerPrefs.SetInt(AttAskKey, 2);
            PlayerPrefs.Save();
        }

        public static void AcceptGdpr()
        {
            _promptYes = true;
            _promptDone = true;
        }

        public static void DeclineGdpr()
        {
            _promptYes = false;
            _promptDone = true;
        }

        // True when the native request was started this call. A pending request
        // stays pending until the app is active, on the home screen, and not under a coach.
        public static bool PollSystem(bool appActive, bool midLevel, bool coachOrModal)
        {
            if (!ShouldRequestSystem(_systemPending, appActive, midLevel, coachOrModal))
                return false;
            _systemPending = false;
            Tracking.RequestWhenActive();
            return true;
        }

        // ATT status arrived after boot. Raise or keep the LevelPlay consent bit.
        // A GDPR choice that is still outstanding is left alone.
        public static void NoteAttAnswered()
        {
            bool attRequired = false;
            try { attRequired = Tracking.AttRequired; }
            catch (System.Exception) { return; }
            RegionClass region = DeviceRegion();
            bool has = HasChoice();
            Plan plan = Decide(region, has, has && SavedConsent());
            if (!plan.Ready) return;
            bool personal = Personalized(plan.Consent, attRequired, Tracking.CompletedStatus);
            ApplyToLevelPlay(new Plan(false, true, personal, plan.ApplyDoNotSell, plan.DoNotSell));
        }

        // Drops the in-game cards for this session without writing a GDPR or ATT choice.
        public static void DismissTransient()
        {
            _transientDismissed = true;
            _prompt = false;
            _promptDone = true;
            _systemPending = false;
            _painted = false;
            _block = default;
            SyncConsentCanvas(false);
        }

        // Fresh-install stand-in for tests. Does not write PlayerPrefs.
        public static void ArmLaunch(RegionClass region)
        {
            _transientDismissed = false;
            _promptDone = false;
            _promptYes = false;
            _systemPending = false;
            EditorHoldAtt = false;
            _painted = false;
            _block = default;
            Plan plan = Decide(region, false, false);
            _prompt = plan.Prompt;
            FirstFrameReady = true;
        }

        public static void ResetTransient()
        {
            _prompt = false;
            _promptDone = false;
            _promptYes = false;
            _transientDismissed = false;
            _systemPending = false;
            EditorHoldAtt = false;
            _painted = false;
            _block = default;
        }

        // Which smoke signal was present. Filled by SmokeAutoPlay. All false when inert.
        public static int SmokeArgc { get; private set; }
        public static bool SmokeArgv { get; private set; }
        public static bool SmokeNative { get; private set; }
        public static bool SmokeEnv { get; private set; }

        static bool _smokeProbed;

        // Simulator smoke hook. Inert unless one signal is present:
        // managed argv (-ffSmokeAutoPlay), NSProcessInfo arguments (iOS IL2CPP
        // stores only a single path in GetCommandLineArgs), or FF_SMOKE_AUTOPLAY=1
        // (simctl: SIMCTL_CHILD_FF_SMOKE_AUTOPLAY=1).
        public static bool SmokeAutoPlay()
        {
            if (!_smokeProbed) ProbeSmoke();
            return SmokeArgv || SmokeNative || SmokeEnv;
        }

        static void ProbeSmoke()
        {
            _smokeProbed = true;
            SmokeArgc = 0;
            SmokeArgv = false;
            SmokeNative = false;
            SmokeEnv = false;
            string[] args = null;
            try { args = System.Environment.GetCommandLineArgs(); }
            catch (System.Exception) { args = null; }
            if (args != null)
            {
                SmokeArgc = args.Length;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "-ffSmokeAutoPlay") SmokeArgv = true;
            }
            try
            {
                SmokeEnv = System.Environment.GetEnvironmentVariable("FF_SMOKE_AUTOPLAY") == "1";
            }
            catch (System.Exception) { SmokeEnv = false; }
#if UNITY_IOS && !UNITY_EDITOR
            try { SmokeNative = FlockFive_HasLaunchArg("-ffSmokeAutoPlay") != 0; }
            catch (System.Exception) { SmokeNative = false; }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern int FlockFive_HasLaunchArg(string needle);
#endif

        // True only while a card is painted and the point is inside that card.
        public static bool BlocksPoint(Vector2 p)
        {
            if (!_painted || _block.width < 2f || _block.height < 2f) return false;
            return _block.Contains(p);
        }

        // True only while a card is painted and the control actually overlaps it.
        public static bool BlocksControl(Rect control)
        {
            if (!_painted || _block.width < 2f || _block.height < 2f) return false;
            if (control.width < 1f || control.height < 1f) return false;
            return _block.Overlaps(control);
        }

        // Wood card between the wordmark and the LEVEL flower. Never covers the flower.
        public static Rect PromptCard(float w, float h, Rect safe)
        {
            float s = h / 720f;
            if (s < 1f) s = 1f;
            Rect level = SplashPress.LevelRect(w, h, safe);
            float topInset = (safe.width < 2f || safe.height < 2f) ? 0f : Mathf.Max(0f, h - safe.yMax);
            float hud = Mathf.Max(12f, topInset + 10f * s);
            float fit = s * 0.91f;
            float logoBottom = hud + (56f * 2f + 4f) * fit;
            float gap = 18f * s;
            float top = logoBottom + gap;
            float pad = 22f * s;
            float body = 72f * s;
            float btn = Mathf.Max(44f, 52f * s);
            float cardH = pad + body + 16f * s + btn + pad;
            float roof = level.yMin - 12f;
            if (top < hud) top = hud;
            if (top + cardH > roof)
            {
                float minH = Mathf.Min(cardH, roof - hud);
                if (minH < 8f) minH = 8f;
                if (roof - top < minH) top = roof - minH;
                if (top < 0f) top = 0f;
                cardH = roof - top;
                if (cardH < 8f) cardH = 8f;
            }
            float cardW = w * 0.86f;
            float cap = 520f * s;
            if (cardW > cap) cardW = cap;
            if (cardW < 8f) cardW = 8f;
            if (cardW > w - 16f) cardW = Mathf.Max(8f, w - 16f);
            float x = (w - cardW) * 0.5f;
            return new Rect(x, top, cardW, cardH);
        }

        public static void PlaceButtons(Rect card, float scale, out Rect accept, out Rect decline)
        {
            if (scale < 1f) scale = 1f;
            float pad = 18f * scale;
            float gap = 12f * scale;
            float bh = Mathf.Max(44f, 48f * scale);
            float maxH = card.height * 0.38f;
            if (bh > maxH && maxH > 36f) bh = maxH;
            if (bh > card.height - pad * 2f) bh = Mathf.Max(8f, card.height - pad * 2f);
            float y = card.yMax - pad - bh;
            if (y < card.y) y = card.y;
            float innerW = card.width - pad * 2f;
            float bw = (innerW - gap) * 0.5f;
            if (bw < 8f) bw = 8f;
            accept = new Rect(card.x + pad, y, bw, bh);
            decline = new Rect(accept.xMax + gap, y, bw, bh);
        }

        // Lays out the card that should be on the home screen this frame and arms
        // BlocksPoint / BlocksControl. A flow that is only waiting paints nothing.
        public static HomeCard LayoutHome(float w, float h, Rect safe, bool onHome, bool coachOrModal, int nextPlay)
        {
            Sheet kind = Due(onHome, coachOrModal, nextPlay);
            if (kind == Sheet.None)
            {
                _painted = false;
                _block = default;
                return default;
            }
            Rect card = PromptCard(w, h, safe);
            float scale = h / 720f;
            PlaceButtons(card, scale, out Rect accept, out Rect decline);
            float pad = 20f * scale;
            float bodyBottom = accept.y - 12f * scale;
            if (bodyBottom < card.y + pad) bodyBottom = card.y + pad;
            var body = new Rect(card.x + pad, card.y + pad, Mathf.Max(8f, card.width - pad * 2f), Mathf.Max(8f, bodyBottom - card.y - pad));
            _painted = card.width > 2f && card.height > 2f;
            _block = _painted ? card : default;
            return new HomeCard(kind, card, body, accept, decline);
        }

        static Sheet Due(bool onHome, bool coachOrModal, int nextPlay)
        {
            if (!onHome || coachOrModal) return Sheet.None;
            if (MayShowConsent(FirstFrameReady, _prompt) && !_promptDone && !_transientDismissed)
                return Sheet.Gdpr;
            if (EditorHoldAtt) return Sheet.Att;
            if (_systemPending) return Sheet.None;
            if (!Tracking.Undecided()) return Sheet.None;
            int ask = PlayerPrefs.GetInt(AttAskKey, 0);
            int snooze = PlayerPrefs.GetInt(AttSnoozeKey, 0);
            if (Consider(nextPlay, true, false, ask, snooze) == AttMoment.PrePrompt)
                return Sheet.Att;
            return Sheet.None;
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
                if (plan.Prompt && !_transientDismissed)
                {
                    _prompt = true;
                    _promptDone = false;
                    SyncConsentCanvas(true);
                    while (!_promptDone && !_transientDismissed)
                    {
                        if (host == null) yield break;
                        yield return null;
                    }
                    _prompt = false;
                    SyncConsentCanvas(false);
                    if (!_transientDismissed && _promptDone)
                    {
                        granted = _promptYes;
                        SaveChoice(granted);
                        plan = Decide(region, true, granted);
                    }
                }

                bool attRequired = false;
                try { attRequired = Tracking.AttRequired; }
                catch (System.Exception e)
                {
                    Debug.LogWarning("ATT requirement skipped: " + e.Message);
                }
                bool personal = Personalized(plan.Consent, attRequired, Tracking.CompletedStatus);
                var boot = new Plan(false, plan.Ready, personal, plan.ApplyDoNotSell, plan.DoNotSell);
                if (!MayBoot(boot.Ready, attRequired, default)) yield break;
                ApplyToLevelPlay(boot);
                // Stored CCPA choice before init. The Settings toggle applies it again later.
                ApplyStoredDoNotSell();
                if (host != null) host.Boot();
            }
            finally
            {
                _prompt = false;
                _painted = false;
                _block = default;
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

        // Kept so a call outside OnGUI cannot throw. The home sheet draws the card.
        // This used to be an IMGUI form whose Not now button sat on the LEVEL flower.
        public static void DrawPrompt()
        {
            if (Event.current == null) return;
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
