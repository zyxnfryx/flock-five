using UnityEngine;

namespace FlockFive
{
    // Build 89 (Brandon, TestFlight 88: tapped an ad through to the App Store, came back,
    // music played over the ad that was still open). One shared hold for every ad.
    // Ads.BeginShow takes it, Ads.EndShow gives it back (close, fail, skip and the
    // safety release all end there). While it is held, nothing may unpause the listener
    // or put the music and SFX back: GamePause.Pop/Reset/UnstickAudio, AdPause.End,
    // AudioGuard.Restore/Reassert, MixDesk and every OnApplicationPause/Focus resume
    // path ask Held first. Coming back to the app is NOT a release: on iOS the ad is
    // usually still on screen after a click-through, and on Android the same focus
    // return happens. Only the ad's own close/fail callback releases it.
    //
    // Safety: if a close never arrives, the hold lets go after SafetySeconds of
    // foreground time (suspension spikes do not count), or ResumeSafetySeconds after
    // the latest return to the app, whichever comes first.
    public static class AdAudioHold
    {
        public const float SafetySeconds = 185f;
        public const float ResumeSafetySeconds = 90f;

        static int _count;
        static float _foreground;
        static float _limit = SafetySeconds;

        public static bool Held => _count > 0;
        public static int Count => _count;
        public static float Foreground => _foreground;
        public static bool SafetyDue => _count > 0 && _foreground >= _limit;

        public static void Acquire()
        {
            if (_count == 0)
            {
                _foreground = 0f;
                _limit = SafetySeconds;
            }
            _count++;
        }

        public static void Release()
        {
            if (_count > 0) _count--;
        }

        // Back in the foreground with an ad still up. Keeps the hold; only tightens the
        // safety window so a close that was lost in the store hop cannot strand silence.
        public static void NoteResume()
        {
            if (_count == 0) return;
            float resumeLimit = _foreground + ResumeSafetySeconds;
            if (resumeLimit < _limit) _limit = resumeLimit;
        }

        // Foreground seconds only. Returns true on the frame the safety window runs out.
        public static bool Tick(float foregroundDt)
        {
            if (_count == 0) return false;
            if (foregroundDt > 0f) _foreground += foregroundDt;
            return SafetyDue;
        }

        // The single question every resume or unpause path asks.
        public static bool MayUnpauseListener => _count == 0 && !GamePause.Paused;

        public static void ResetForTest()
        {
            _count = 0;
            _foreground = 0f;
            _limit = SafetySeconds;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ResetForTest();
    }
}
