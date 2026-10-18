using UnityEngine;

namespace FlockFive
{
    // b110 (Brandon): one-time lesson the first time lightning burns a gift branch. It rides the shared pest-cue
    // path (caption, glove, tap gate). The strike arms it; PestIntroAdvance shows it once the stage is calm.
    public sealed partial class FlockFiveApp
    {
        // The limb is still burning and falling for about a second. The line waits for that.
        public const float FireLessonDelay = 1.1f;

        bool _fireArmed;
        Vector3 _fireAim;
        float _fireShowAt;

        // Pure rule, shared with the tests. Seen is the persisted flag.
        public static bool FireLessonOwed(bool seen, bool armed, float now, float showAt) =>
            !seen && armed && now >= showAt;

        void ArmFireLesson(Vector3 world)
        {
            if (PlayerPrefs.GetInt(CoachFireKey, 0) != 0) return;
            _fireArmed = true;
            _fireAim = world;
            _fireShowAt = PlayClock.Now + FireLessonDelay;
        }

        bool FireDue()
        {
            if (!FireLessonOwed(PlayerPrefs.GetInt(CoachFireKey, 0) != 0, _fireArmed, PlayClock.Now, _fireShowAt))
                return false;
            return _lightningStrikes > 0;
        }

        bool FireCueAlive() => _fireArmed && _lightningStrikes > 0 && !_won && _board != null;
    }
}
