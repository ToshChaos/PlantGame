using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// Greyscale copy of the Quaternius leaf texture (alpha kept) and a material that uses it, so Scenery can tint the same leaf detail
    /// red, yellow, orange or purple (multiplying the green original would only give mud). Menu PlantGame > 16 Build Leaf Tint.
    /// </summary>
    public static class LeafTintBuilder
    {
        [MenuItem("PlantGame/16 Build Leaf Tint")]
        public static void Build()
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Quaternius/Q_Leaves_NormalTree.mat");
            if (src == null || src.mainTexture == null) { Debug.LogWarning("[PlantGame] Q_Leaves_NormalTree missing: no leaf tint material."); return; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(src.mainTexture)));
            var px = tex.GetPixels();
            double sum = 0; int n = 0;
            foreach (var c in px) if (c.a > 0.5f) { sum += c.grayscale; n++; }
            float gain = n > 0 ? 0.8f / (float)(sum / n) : 1f;   // average leaf = 0.8, so a tint colour shows up close to itself
            for (int i = 0; i < px.Length; i++) { float l = Mathf.Clamp01(px[i].grayscale * gain); px[i] = new Color(l, l, l, px[i].a); }
            tex.SetPixels(px);
            const string path = "Assets/Art/Textures/T_LeafGrey.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.SaveAndReimport();
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_LeafTint.mat");
            if (mat == null) { mat = new Material(src); AssetDatabase.CreateAsset(mat, "Assets/Art/Materials/M_LeafTint.mat"); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Leaf tint material built.");
        }
    }
}
