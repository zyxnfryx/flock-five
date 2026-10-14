#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode stills and the keep-streak clip. Launch with graphics, not -batchmode.
    public static class Build88StreakStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            // The resolver's "Enable Android Auto-resolution?" prompt is a modal
            // dialog. It froze the last capture as soon as an import landed.
            SilenceAndroidResolver();
            Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
            Build79Stills.SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b88-streak", "1");
            EditorApplication.isPlaying = true;
        }

        static void SilenceAndroidResolver()
        {
            try
            {
                Type resolver = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    resolver = asm.GetType("GooglePlayServices.PlayServicesResolver");
                    if (resolver != null) break;
                }
                if (resolver == null)
                {
                    Debug.Log("KEEP_STREAK resolver absent");
                    return;
                }
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                SetBool(resolver, "PromptBeforeAutoResolution", false, flags);
                SetBool(resolver, "EnableAutoResolution", false, flags);
                Debug.Log("KEEP_STREAK resolver silenced");
            }
            catch (Exception e)
            {
                Debug.LogWarning("KEEP_STREAK resolver silence failed: " + e.Message);
            }
        }

        static void SetBool(Type type, string property, bool value, BindingFlags flags)
        {
            var prop = type.GetProperty(property, flags);
            if (prop == null || !prop.CanWrite) return;
            prop.SetValue(null, value);
        }
    }
}
#endif
