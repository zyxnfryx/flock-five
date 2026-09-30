using UnityEngine;

namespace FlockFive
{
    // One router for every haptic. Callers opt in; routine taps never reach here.
    public static class Haptics
    {
        public enum Tier
        {
            Light = 1,
            Medium = 2,
            Strong = 3
        }

        public const string PrefKey = "flockfive.haptics";
        const float Gap = 0.18f;
        const int StrongCombo = 5;

        static int _pending;
        static int _held;
        static float _next;
        static bool _skipBreak;
        static HapticsPump _pump;

        // Missing key stays on.
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (!value) _pending = 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _pending = 0;
            _held = 0;
            _next = 0f;
            _skipBreak = false;
            _pump = null;
        }

        public static void Play(Tier tier)
        {
            try { Offer(tier); }
            catch (System.Exception) { }
        }

        // Spacing window lives here. It only coalesces vibrations.
        static void Offer(Tier tier)
        {
            if (!CanPlay(tier)) return;
            int t = (int)tier;
            float now = Time.unscaledTime;
            // Spacing window: a stronger tap replaces a lighter one still waiting.
            if (now < _next)
            {
                if (t > _pending && t > _held)
                {
                    _pending = t;
                    EnsurePump();
                }
                return;
            }
            if (_pending == 0 || t > _pending)
                _pending = t;
            EnsurePump();
        }

        // Combo owns this clear, so the limb snap must not buzz as well.
        // Fire-and-forget: a prefs, pump, or vibrate failure must not escape.
        public static void OnCombo(int size)
        {
            try
            {
                if (size >= 3)
                {
                    _skipBreak = true;
                    Play(size >= StrongCombo ? Tier.Strong : Tier.Medium);
                }
                else
                    _skipBreak = false;
            }
            catch (System.Exception) { }
        }

        public static void BranchBreak()
        {
            try
            {
                if (_skipBreak)
                {
                    _skipBreak = false;
                    return;
                }
                Play(Tier.Light);
            }
            catch (System.Exception) { }
        }

        internal static void Pump()
        {
            try { Drain(); }
            catch (System.Exception) { }
        }

        static void Drain()
        {
            if (_pending == 0) return;
            if (Time.unscaledTime < _next) return;
            int t = _pending;
            _pending = 0;
            if (!CanPlay((Tier)t)) return;
            try { Handheld.Vibrate(); }
            catch (System.Exception) { }
            _next = Time.unscaledTime + Gap;
            _held = t;
        }

        static bool CanPlay(Tier tier)
        {
            if (!Enabled || !Application.isMobilePlatform) return false;
            // UIImpactFeedbackGenerator needs an Objective-C plugin. Light has no system-vibrate equivalent.
            if (tier == Tier.Light) return false;
            return true;
        }

        static void EnsurePump()
        {
            try { EnsurePumpCore(); }
            catch (System.Exception) { }
        }

        static void EnsurePumpCore()
        {
            if (_pump != null) return;
            var found = Object.FindObjectsByType<HapticsPump>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            HapticsPump keep = null;
            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    var h = found[i];
                    if (h == null) continue;
                    if (keep == null) keep = h;
                    else Object.Destroy(h.gameObject);
                }
            }
            if (keep == null)
            {
                var go = new GameObject("Haptics");
                Object.DontDestroyOnLoad(go);
                keep = go.AddComponent<HapticsPump>();
            }
            else
                Object.DontDestroyOnLoad(keep.gameObject);
            _pump = keep;
        }
    }

    sealed class HapticsPump : MonoBehaviour
    {
        void LateUpdate() => Haptics.Pump();
    }
}
