#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Writes the settings cog once. Launch loads this sprite instead of filling
    // the 512² supersampled SDF on the first home paint.
    public static class GearBake
    {
        public const string AssetPath = "Assets/_Project/Art/Resources/Sprites/settings_gear.png";

        public static void Run()
        {
            GearIcon.ResetForTests();
            var px = GearIcon.Pixels();
            var tex = new Texture2D(GearIcon.Resolution, GearIcon.Resolution, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(AssetPath, png);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            UnityEngine.Debug.Log("GEAR_BAKE wrote " + png.Length + " " + AssetPath);
        }
    }
}
#endif
