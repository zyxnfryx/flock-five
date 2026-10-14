using UnityEngine;

namespace FlockFive
{
    // Build 85 (Brandon: "start an ad in daytime, come back at night"). One shared freeze for
    // every ad (rewarded + interstitial, via Ads.BeginShow/EndShow, plus close, fail, watchdog
    // and ForceClear through EndShow). While an ad is up:
    //  - Time.timeScale is held at 0 every frame (nothing can put it back mid-ad);
    //  - PlayClock (SkyCycle day/night, storms, pests, ambience) does not advance, even if an
    //    unbalanced GamePause.Pop elsewhere (e.g. a tutorial heal on app background) drops depth;
    //  - AdPause.Unscaled / UnscaledDelta, which replace Time.unscaledTime / unscaledDeltaTime in
    //    gameplay code (cooldowns, idle triggers, unscaled coroutines), stand still;
    //  - music and SFX are paused (listener pause + AudioGuard hold, MixDesk stems included).
    // On end everything is restored FIRST; a reward earned during the ad is applied after.
    public static class AdPause
    {
        static bool _on;
        static float _startedAt;
        static float _total;
        static bool _grantDue;
        public static int Opens { get; private set; }

        public static bool Active => _on;

        // Unscaled seconds with ad time removed.
        public static float Unscaled => Time.unscaledTime - _total - (_on ? Time.unscaledTime - _startedAt : 0f);
        public static float UnscaledDelta => _on ? 0f : Time.unscaledDeltaTime;
        public static float AdSeconds => _total + (_on ? Time.unscaledTime - _startedAt : 0f);

        public static void Begin()
        {
            if (_on) return;
            _on = true;
            Opens++;
            _startedAt = Time.unscaledTime;
            Hold();
        }

        // Every frame while up (Ads host Update) and on resume from an app pause.
        public static void Hold()
        {
            if (!_on) return;
            if (Time.timeScale != 0f) Time.timeScale = 0f;
            if (!AudioListener.pause) AudioListener.pause = true;
        }

        public static void End()
        {
            if (!_on) return;
            _total += Time.unscaledTime - _startedAt;
            _on = false;
            // Pop already restored scale/listener; a stolen pause depth would leave 0, so fix it.
            if (!GamePause.Paused && Time.timeScale == 0f) GamePause.SetBaseScale(GamePause.BaseScale);
            if (!GamePause.Paused) AudioListener.pause = false;
            // b87: the ad is over (close, fail or watchdog), so a missing close cannot strand
            // a latched reward; it applies now, once, with the game restored.
            RewardedGrant.NoteAdOver();
            if (_grantDue || RewardedGrant.HasUnapplied)
            {
                _grantDue = false;
                RewardedGrant.Flush();
            }
        }

        // A reward callback during the ad: remember it, grant after End restores the game.
        public static bool DeferGrant()
        {
            if (!_on) return false;
            _grantDue = true;
            return true;
        }

        public static void OnAppPause(bool paused)
        {
            if (!paused) Hold();
        }

        public static void ResetForTest()
        {
            _on = false;
            _total = 0f;
            _grantDue = false;
            Opens = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ResetForTest();
    }
}
