#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    public sealed class SpriteImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Art/Resources/Sprites/") < 0) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.isReadable = true;
            if (assetPath.Contains("bg_")) imp.spritePixelsPerUnit = 96f;
            else if (assetPath.Contains("branch")) imp.spritePixelsPerUnit = 140f;
            else if (assetPath.Contains("feeder")) imp.spritePixelsPerUnit = 180f;
            else if (assetPath.Contains("bird_")) imp.spritePixelsPerUnit = 220f;
            else if (assetPath.Contains("fx_")) imp.spritePixelsPerUnit = 200f;
            else imp.spritePixelsPerUnit = 256f;
            if (assetPath.Contains("fx_vine")) imp.spritePivot = new Vector2(0.5f, 0.94f);
            else if (assetPath.Contains("fx_leaf")) imp.spritePivot = new Vector2(0.5f, 0.08f);
            else imp.spritePivot = new Vector2(0.5f, 0.5f);
            if (IsAndroidOpaqueAstc(assetPath))
                ApplyAndroidOpaqueAstc(imp);
        }

        // Opaque poker faces and the two backgrounds. Android override only.
        // Sparse RGBA bird art is not in this list.
        public static readonly string[] AndroidOpaqueAstc =
        {
            "Assets/_Project/Art/Resources/Sprites/bg_garden.png",
            "Assets/_Project/Art/Resources/Sprites/bg_oasis.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_gold.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_gold_f.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_gold_m.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_peach.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_peach_f.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_peach_m.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_ruby.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_ruby_f.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_ruby_m.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_teal.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_teal_f.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_teal_m.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_violet.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_violet_f.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_violet_m.png",
            "Assets/_Project/Art/Resources/Sprites/fx_poker_face_wild.png",
        };

        public static bool IsAndroidOpaqueAstc(string assetPath)
        {
            for (int i = 0; i < AndroidOpaqueAstc.Length; i++)
                if (AndroidOpaqueAstc[i] == assetPath) return true;
            return false;
        }

        public static void ApplyAndroidOpaqueAstc(TextureImporter imp)
        {
            var platform = imp.GetPlatformTextureSettings("Android");
            int max = platform.maxTextureSize > 0 ? platform.maxTextureSize : 2048;
            platform.overridden = true;
            platform.format = TextureImporterFormat.ASTC_6x6;
            platform.textureCompression = TextureImporterCompression.Compressed;
            platform.compressionQuality = 100;
            platform.crunchedCompression = false;
            platform.maxTextureSize = max;
            imp.SetPlatformTextureSettings(platform);
        }

        public static void EnsureAndroidOpaqueAstc()
        {
            for (int i = 0; i < AndroidOpaqueAstc.Length; i++)
            {
                string path = AndroidOpaqueAstc[i];
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) throw new System.Exception("ANDROIDBUILD missing " + path);
                var ios = imp.GetPlatformTextureSettings("iPhone");
                bool iosOver = ios.overridden;
                int iosFormat = (int)ios.format;
                int iosCompression = (int)ios.textureCompression;
                var cur = imp.GetPlatformTextureSettings("Android");
                if (!(cur.overridden && cur.format == TextureImporterFormat.ASTC_6x6
                    && cur.compressionQuality == 100 && !cur.crunchedCompression))
                {
                    ApplyAndroidOpaqueAstc(imp);
                    imp.SaveAndReimport();
                    imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) throw new System.Exception("ANDROIDBUILD reimport failed " + path);
                }
                var after = imp.GetPlatformTextureSettings("iPhone");
                if (after.overridden != iosOver || (int)after.format != iosFormat
                    || (int)after.textureCompression != iosCompression)
                    throw new System.Exception("ANDROIDBUILD iOS settings changed " + path);
                UnityEngine.Debug.Log("ANDROIDBUILD astc6x6 " + path);
            }
        }

        void OnPreprocessAudio()
        {
            if (assetPath.IndexOf("/Art/Resources/Audio/") < 0) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.PCM;
            s.quality = 1f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.loadInBackground = false;
        }
    }
}
#endif
