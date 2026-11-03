using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// T02: applies the pipeline import settings to every model under Assets/Art/Models (docs/pipeline.md).
    /// Scale 1 m, no material import (project materials are assigned in prefabs), mesh compression off,
    /// read/write on for plants (vertex colours + runtime collider work), empties preserved (Anchor/Tip/COL_*).
    /// Runs on first import only (no existing .meta), so hand-tuned settings survive.
    /// </summary>
    public class ModelImportSettings : AssetPostprocessor
    {
        const string Root = "Assets/Art/Models/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return;
            if (assetPath.StartsWith(Root + "Kenney/") || assetPath.StartsWith(Root + "Quaternius/") || assetPath.StartsWith(Root + "Ruins/")) return;  // third-party props: KenneyPropBuilder / QuaterniusPropBuilder (keep embedded materials)
            if (!assetImporter.importSettingsMissing) return;   // already configured once
            Apply((ModelImporter)assetImporter);
        }

        public static void Apply(ModelImporter m)
        {
            m.globalScale = 1f;
            m.useFileScale = true;                       // FBX_SCALE_UNITS export: file unit -> metres
            m.bakeAxisConversion = m.assetPath.StartsWith(Root + "Characters/") || m.assetPath.StartsWith(Root + "Plants/");   // hierarchies exported without bake_space_transform (T31)
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.meshCompression = ModelImporterMeshCompression.Off;
            m.isReadable = m.assetPath.StartsWith(Root + "Plants/");
            m.preserveHierarchy = true;                  // keep empty transforms (Anchor, Tip, Land, COL_*)
            m.importBlendShapes = false;
            m.importCameras = false;
            m.importLights = false;
            m.importAnimation = false;
            m.animationType = ModelImporterAnimationType.None;
            m.addCollider = false;
            m.generateSecondaryUV = false;
        }

        /// <summary>Re-applies the settings to all models (use after changing the preset).</summary>
        [MenuItem("PlantGame/Tools/Reapply Model Import Settings")]
        public static void ReapplyAll()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models" }))
            {
                var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
                if (imp == null) continue;
                Apply(imp); imp.SaveAndReimport(); n++;
            }
            Debug.Log($"[PlantGame] Reapplied import settings to {n} model(s).");
        }
    }
}
