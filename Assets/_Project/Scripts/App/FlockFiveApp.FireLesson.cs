using UnityEngine;

namespace FlockFive
{
    // b110 (Brandon): one-time lesson the first time lightning burns a gift branch. It rides the shared pest-cue
    // path (caption, glove, tap gate). The strike arms it; PestIntroAdvance shows it once the stage is calm.
    public sealed partial class FlockFiveApp
    {
        // b113: the limb now burns for BranchView.ScorchSeconds (1.5 s) before it falls. The line shows once the fire has
        // taken hold (0.9 s), while the branch is still burning, so the arrow points at a branch that is on fire.
        public const float FireLessonDelay = 0.9f;

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
            return _lightningStrikes > 0 && LightningStrike.GardenEligible(_lightningGarden);
        }

        bool FireCueAlive() => _fireArmed && LightningStrike.GardenEligible(_lightningGarden) && _lightningStrikes > 0 && !_won && _board != null;
    }
}
