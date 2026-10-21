#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b128 play-mode stills (badger 'Your pick.' caption + power row). Launch without -batchmode and without -nographics.
    public static class BadgerPowerStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Build79Stills.SetGameView(750, 1334);
            File.WriteAllText("/tmp/flock-five-badger-power", "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
