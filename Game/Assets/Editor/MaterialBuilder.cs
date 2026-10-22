using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Editor
{
    /// <summary>
    /// T01: builds the shared materials in Assets/Art/Materials from the palette (exact hexes).
    /// Menu: PlantGame > 2 Build Materials. Idempotent: existing materials are updated in place so references survive.
    /// </summary>
    public static class MaterialBuilder
    {
        public const string Dir = "Assets/Art/Materials";

        [MenuItem("PlantGame/2 Build Materials")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var grow = Shader.Find("PlantGame/PlantGrow");
            if (lit == null || unlit == null)
            {
                Debug.LogError("[PlantGame] URP shaders not found. Is the Universal Render Pipeline package installed and the project using a URP asset?");
                return;
            }
            if (grow == null) Debug.LogError("[PlantGame] Shader 'PlantGame/PlantGrow' not found (Assets/Art/Shaders/PlantGrow.shader). Check the Console for shader compile errors.");

            Make("M_Terrain", lit, m => SetLit(m, Palette.LeafGreen, 0.1f));
            Make("M_Stone", lit, m => SetLit(m, Palette.MistBlue, 0.15f));
            Make("M_Foliage", lit, m => SetLit(m, Palette.Moss, 0.2f));
            Make("M_Cloud", lit, m =>
            {
                SetLit(m, Palette.CloudWhite, 0.05f);
                SetEmission(m, Palette.CloudWhite * 0.25f);
            });
            Make("M_Crystal", lit, m =>
            {
                SetLit(m, Palette.GlowCyan, 0.8f);
                SetEmission(m, Palette.GlowCyan * 1.0f);   // 1.0 keeps the hex readable without tonemapping; T16 raises it once bloom/tonemapping exist
            });
            Make("M_UI", unlit, m => { m.SetColor("_BaseColor", Palette.CloudWhite); });
            if (grow != null)
                Make("M_PlantGrow", grow, m =>
                {
                    m.SetColor("_BaseColor", Palette.LeafGreen);
                    m.SetColor("_TipColor", Palette.CloudWhite);
                    m.SetColor("_ShadeColor", Palette.Moss);
                    m.SetColor("_RimColor", Palette.GlowCyan);
                    m.SetFloat("_Growth", 1f);
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlantGame] Materials built in " + Dir);
        }

        public static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{name}.mat");

        static void Make(string name, Shader shader, Action<Material> setup)
        {
            var path = $"{Dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            setup(mat);
            EditorUtility.SetDirty(mat);
        }

        static void SetLit(Material m, Color c, float smoothness)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
        }

        static void SetEmission(Material m, Color hdr)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", hdr);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
    }
}
