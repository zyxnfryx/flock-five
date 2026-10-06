#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play Mode debug hands for recording the poker celebrations.
    static class PokerDebugMenu
    {
        // FlockFiveApp.LateUpdate picks this file up (editor only), opens poker, deals a real
        // hand, rigs the draw to FIVE WILDS and draws through the normal DRAW path.
        const string ForceFiveWildsCmd = "/tmp/flock-five-force-five-wilds";

        [MenuItem("Flock Five/Debug/Force Five Wilds")]
        public static void ForceFiveWilds()
        {
            File.WriteAllText(ForceFiveWildsCmd, "1");
            Debug.Log("[poker] Force Five Wilds queued; the hand plays on the next frame.");
        }

        [MenuItem("Flock Five/Debug/Force Five Wilds", true)]
        static bool ForceFiveWildsValid() => EditorApplication.isPlaying;

        // Clears the punch card so the FIVE WILDS stamp (WildKind) plays again.
        [MenuItem("Flock Five/Debug/Clear Poker Punches")]
        public static void ClearPunches() => BirdPoker.ClearPunches();
    }
}
#endif
