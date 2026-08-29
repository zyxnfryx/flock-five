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
