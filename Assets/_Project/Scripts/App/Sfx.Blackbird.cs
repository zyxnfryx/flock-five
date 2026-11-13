using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip _bbTaunt;

        // One recorded chook. Lead, so it is heard after the drop. No sine, no second pitch.
        public static void BlackbirdTaunt()
        {
            if (_bbTaunt == null) _bbTaunt = Resources.Load<AudioClip>("Audio/Blackbird/chook");
            if (_bbTaunt == null) return;
            float pitch = 1f;
            if (pitch > BlackbirdCue.TauntPitchMax) pitch = BlackbirdCue.TauntPitchMax;
            Shot(_bbTaunt, pitch, BlackbirdCue.TauntLevel, MixLayer.Lead, MixDesk.DuckWhoosh);
        }

        // The 0.5s tell is a wing from the flap bank, on Mid. It is skipped
        // while a branch crunch holds the lead seat.
        public static void BlackbirdTell()
        {
            if (!BlackbirdCue.HearTell(MixDesk.Live != null && MixDesk.Live.LeadHot)) return;
            PlayFlap(0.34f, 1f, MixLayer.Mid);
        }
    }
}
