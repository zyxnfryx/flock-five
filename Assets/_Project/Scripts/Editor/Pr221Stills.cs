#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // PR 221 play-mode stills (spring garden, strike + flame tongues, frozen captions). Launch without -batchmode / -nographics.
    public static class Pr221Stills
    {
        public static void Capture()
        {
            ShotPrefsGuard.Begin(); // a play session writes the real PlayerPrefs; put them back after
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            var sz = FlockFiveApp.Pr221Size(); // FF_PR221_SIZE="W H tag", default 1170x2532
            Build79Stills.SetGameView(sz.x, sz.y);
            File.WriteAllText("/tmp/flock-five-pr221", "1");
            EditorApplication.isPlaying = true;
        }

        // Badger tutorial copy + caption-fit suite (not in RunEditorTests), sandboxed like RunEditorTests.
        public static void CheckBadgerTutorial()
        {
            using (TestPrefsSandbox.Begin()) Build123BadgerTutorialTests.Run();
        }
    }
}
#endif
