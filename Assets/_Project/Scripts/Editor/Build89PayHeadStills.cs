#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills of the open pay table (iPhone, iPad, Android) after the header
    // tests and the poker test suites. Launch without -batchmode and -nographics.
    public static class Build89PayHeadStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            var log = new System.Text.StringBuilder();
            void Try(string n, System.Action a)
            {
                try { a(); log.AppendLine("ran " + n); }
                catch (System.Exception e) { log.AppendLine("ERROR " + n + " " + e.Message); }
            }
            Try("Build89PayHeadTests", Build89PayHeadTests.Run);
            Try("PokerFlushTests", PokerFlushTests.Run);
            Try("PokerDealGateTests", PokerDealGateTests.Run);
            Try("PokerBetTests", PokerBetTests.Run);
            File.WriteAllText("/tmp/b89-payhead-testrun.txt", log.ToString());
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout/b89-payhead");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b89-payhead", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
