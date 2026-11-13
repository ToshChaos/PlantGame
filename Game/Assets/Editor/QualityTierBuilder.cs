using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PlantGame.Editor
{
    /// <summary>
    /// T63: the three graphics tiers (PlantGame.Core.QualityTier). Menu: PlantGame > Configure Quality Tiers. Idempotent.
    /// - Quality levels become exactly Low / Medium / High; phones (Android, iOS) default to Medium, everything else to High.
    /// - High keeps PlantGame_URP (desktop). Medium (PlantGame_URP_Medium) targets a 2022 phone at 1080 x 2340: render scale 0.75
    ///   (1755 x 810), no HDR, no MSAA, one 1024 shadow cascade out to 30 m, no additional lights, cookies, lens flares or LOD cross-fade.
    ///   Low (PlantGame_URP_Low) is the same at render scale 0.6 without shadows.
    /// - Small scatter props (Quaternius bushes, ferns, mushrooms) are imported read/write so the
    ///   WorldOptimizer can static-batch them at runtime; the big trees and ruins stay GPU-only.
    /// - Phone builds cap the big third-party textures (Quaternius, Ruins: 2048 maps) at 1024; desktop keeps 2048.
    /// </summary>
    public static class QualityTierBuilder
    {
        const string Dir = "Assets/Settings", High = Dir + "/PlantGame_URP.asset", Medium = Dir + "/PlantGame_URP_Medium.asset", Low = Dir + "/PlantGame_URP_Low.asset";

        [MenuItem("PlantGame/Platforms/Quality Tiers", false, 80)]
        public static void Build()
        {
            var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(High);
            if (high == null) { Debug.LogError("[PlantGame] " + High + " is missing (run the project setup first)."); return; }
            var medium = Tier(Medium, 0.75f, true);
            var low = Tier(Low, 0.6f, false);
            Levels(low, medium, high);
            int readable = MakeScatterReadable();
            int capped = CapPhoneTextures();
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] Quality tiers: Low / Medium / High configured; {readable} scatter model(s) switched to read/write, {capped} texture(s) capped at {PhoneMaxTexture} on phones.");
        }

        static UniversalRenderPipelineAsset Tier(string path, float scale, bool shadows)
        {
            if (!File.Exists(path)) AssetDatabase.CopyAsset(High, path);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var so = new SerializedObject(asset);
            void F(string p, float v) => so.FindProperty(p).floatValue = v;
            void I(string p, int v) => so.FindProperty(p).intValue = v;
            void B(string p, bool v) => so.FindProperty(p).boolValue = v;
            F("m_RenderScale", scale);
            I("m_UpscalingFilter", 1);                 // linear: the cheapest upscale
            B("m_SupportsHDR", false);
            I("m_MSAA", 1);
            B("m_MainLightShadowsSupported", shadows); B("m_AnyShadowsSupported", shadows);
            I("m_MainLightShadowmapResolution", 1024);
            F("m_ShadowDistance", 30f);
            I("m_ShadowCascadeCount", 1);
            B("m_SoftShadowsSupported", false);
            I("m_AdditionalLightsRenderingMode", 0);   // disabled
            B("m_AdditionalLightShadowsSupported", false);
            B("m_EnableLODCrossFade", false);
            B("m_SupportsLightCookies", false);
            B("m_SupportDataDrivenLensFlare", false); B("m_SupportScreenSpaceLensFlare", false);
            B("m_SupportsTerrainHoles", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>Quality levels: exactly Low, Medium, High (the old Very Low / High / Very High go, Ultra becomes High).</summary>
        static void Levels(Object low, Object medium, Object high)
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var so = new SerializedObject(qs);
            var arr = so.FindProperty("m_QualitySettings");
            int Find(string n) { for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) return i; return -1; }
            if (Find("Ultra") >= 0)
            {
                foreach (var gone in new[] { "Very Low", "High", "Very High" }) { int i = Find(gone); if (i >= 0) arr.DeleteArrayElementAtIndex(i); }
                arr.GetArrayElementAtIndex(Find("Ultra")).FindPropertyRelative("name").stringValue = "High";
            }
            void Set(string n, Object rp, int shadows)
            {
                int i = Find(n); if (i < 0) { Debug.LogError("[PlantGame] Quality level " + n + " not found."); return; }
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("customRenderPipeline").objectReferenceValue = rp;
                e.FindPropertyRelative("shadows").intValue = shadows;
            }
            Set("Low", low, 0); Set("Medium", medium, 1); Set("High", high, 2);
            int highIndex = Find("High"), mediumIndex = Find("Medium");
            var map = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < map.arraySize; i++)
            {
                var e = map.GetArrayElementAtIndex(i); var platform = e.FindPropertyRelative("first").stringValue;
                e.FindPropertyRelative("second").intValue = platform == "Android" || platform == "iPhone" || platform == "tvOS" || platform == "WebGL" ? mediumIndex : highIndex;
            }
            so.FindProperty("m_CurrentQuality").intValue = highIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Scatter props that the runtime world pass static-batches (it needs CPU-readable meshes).</summary>
        public static bool Batchable(string path)
        {
            if (!path.StartsWith("Assets/Art/Models/Quaternius/")) return false;
            var f = Path.GetFileName(path);
            return f.StartsWith("Bush_") || f.StartsWith("Fern_") || f.StartsWith("Mushroom_");
        }

        public const int PhoneMaxTexture = 1024;

        /// <summary>Android / iOS overrides: the third-party maps above 1024 import at 1024 on phones (desktop keeps the default size).</summary>
        static int CapPhoneTextures()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Models/Quaternius", "Assets/Art/Models/Ruins" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                if (Mathf.Max(w, h) <= PhoneMaxTexture) continue;
                bool changed = false;
                foreach (var platform in new[] { "Android", "iPhone" })
                {
                    var s = imp.GetPlatformTextureSettings(platform);
                    if (s.overridden && s.maxTextureSize == PhoneMaxTexture) continue;
                    s.overridden = true; s.maxTextureSize = PhoneMaxTexture;   // format stays Automatic: the project-wide phone compression
                    imp.SetPlatformTextureSettings(s); changed = true;
                }
                if (changed) { imp.SaveAndReimport(); n++; }
            }
            return n;
        }

        static int MakeScatterReadable()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models/Quaternius" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Batchable(path) || !(AssetImporter.GetAtPath(path) is ModelImporter imp) || imp.isReadable) continue;
                imp.isReadable = true; imp.SaveAndReimport(); n++;
            }
            return n;
        }
    }
}
