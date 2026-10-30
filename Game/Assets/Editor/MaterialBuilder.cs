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

        [MenuItem("PlantGame/2 Build Materials", false, 2)]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var grow = Shader.Find("PlantGame/PlantGrow");
            if (lit == null)
            {
                Debug.LogError("[PlantGame] URP shaders not found. Is the Universal Render Pipeline package installed and the project using a URP asset?");
                return;
            }
            if (grow == null) Debug.LogError("[PlantGame] Shader 'PlantGame/PlantGrow' not found (Assets/Art/Shaders/PlantGrow.shader). Check the Console for shader compile errors.");

            if (TextureBuilder.Albedo("T_Terrain") == null || TextureBuilder.Albedo("T_Mud") == null) TextureBuilder.Build();
            // island tops: a calmer, deeper green than the plants and seeds, so those stand out; texture adds the grass mottling
            Make("M_Terrain", lit, m => { SetLit(m, Color.Lerp(Palette.Moss, Palette.LeafGreen, 0.55f), 0.1f); SetTextured(m, "T_Terrain", 0.8f); });
            // other ground types for islands with less grass (WorldGenerator picks one per island)
            Make("M_TerrainMud", lit, m => { SetLit(m, new Color(0.80f, 0.60f, 0.42f), 0.08f); SetTextured(m, "T_Mud", 1.3f); });
            Make("M_TerrainRock", lit, m => { SetLit(m, new Color(0.64f, 0.66f, 0.70f), 0.12f); SetTextured(m, "T_Pebbles", 1.1f); });
            var sheet = Shader.Find("PlantGame/WaterfallSheet");
            if (sheet != null) Make("M_WaterfallSheet", sheet, m => { });
            Make("M_Stone", lit, m => { SetLit(m, Palette.MistBlue, 0.15f); SetTextured(m, "T_Rock", 1f); });
            Make("M_RockLoose", lit, m => { SetLit(m, new Color(0.7f, 0.68f, 0.64f), 0.14f); SetTextured(m, "T_Stone", 0.8f); });   // loose stones and floating rocks (OrganicProps.RockMat)
            Make("M_Foliage", lit, m => { SetLit(m, Palette.Moss, 0.2f); SetTextured(m, "T_Terrain", 0.6f); m.mainTextureScale = new Vector2(3f, 3f); });
            Make("M_FoliageLight", lit, m => { SetLit(m, Palette.LeafGreen, 0.2f); SetTextured(m, "T_Terrain", 0.6f); m.mainTextureScale = new Vector2(3f, 3f); });
            Make("M_GoldFruit", lit, m =>   // the goal's golden fruit
            {
                SetLit(m, new Color(1f, 0.7f, 0.16f), 0.65f); SetTextured(m, "T_GoldFruit", 1.2f);
                SetEmission(m, new Color(1f, 0.55f, 0.1f) * 0.2f);
            });
            Make("M_Bark", lit, m => { SetLit(m, Palette.Bark, 0.1f); SetTextured(m, "T_Bark", 1.2f); });
            Make("M_Soil", lit, m => { SetLit(m, Palette.Soil, 0.05f); SetTextured(m, "T_Soil", 1.4f); });
            Make("M_Crystal", lit, m =>
            {
                SetLit(m, Palette.GlowCyan, 0.8f);
                SetEmission(m, Palette.GlowCyan * 1.0f);   // 1.0 keeps the hex readable without tonemapping; T16 raises it once bloom/tonemapping exist
            });
            Make("M_CrystalShard", lit, m =>   // gems and floating crystals: deep teal glass, see-through, a faint glow (GemSparkle adds the glints)
            {
                SetLit(m, new Color(0.06f, 0.27f, 0.33f, 0.74f), 0.94f);
                SetEmission(m, Palette.GlowCyan * 0.1f);
                RuntimeMats.SetTransparent(m);
            });
            // Player outfit roles (PlayerRole); Unity picks the material by mesh-name suffix through a PlayerOutfit asset.
            Make("M_PlayerSkin", lit, m => SetLit(m, Palette.PlayerSkin, 0.3f));
            Make("M_PlayerHair", lit, m => SetLit(m, Palette.PlayerHair, 0.35f));
            Make("M_PlayerCloth", lit, m => SetLit(m, Palette.PlayerCloth, 0.3f));
            Make("M_PlayerCloak", lit, m => SetLit(m, Palette.PlayerCloak, 0.4f));
            Make("M_PlayerGear", lit, m => SetLit(m, Palette.PlayerGear, 0.25f));
            Make("M_PlayerLeaf", lit, m => SetLit(m, Palette.PlayerLeaf, 0.35f));
            Make("M_PlayerDark", lit, m => SetLit(m, Palette.PlayerDark, 0.5f));
            Make("M_PlayerTrim", lit, m =>
            {
                SetLit(m, Palette.Gold, 0.7f);
                m.SetFloat("_Metallic", 0.75f);
                SetEmission(m, Palette.Gold * 0.15f);   // keeps the trim gold in shadow; no reflection probes yet
            });
            Make("M_PlayerGlow", lit, m =>
            {
                SetLit(m, Palette.GlowCyan, 0.8f);
                SetEmission(m, Palette.GlowCyan * 1.4f);   // runes, gems, the water in the can
            });
            if (grow != null)
                Make("M_PlantGrow", grow, m =>
                {
                    m.SetColor("_BaseColor", Palette.LeafGreen);
                    m.SetColor("_TipColor", Palette.CloudWhite);
                    m.SetColor("_ShadeColor", Palette.Moss);
                    m.SetColor("_RimColor", Palette.GlowCyan);
                    m.SetColor("_CapColor", Palette.MushroomRed);
                    m.SetColor("_FlowerColor", Palette.FlowerYellow);
                    m.SetFloat("_Growth", 1f);
                });

            AssignPlantTextures();
            BuildKeepers(lit);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlantGame] Materials built in " + Dir);
        }

        /// <summary>T67: give every PlantGrow material the bark / leaf / cap maps.</summary>
        [MenuItem("PlantGame/3 Assign Plant Textures", false, 3)]
        public static void AssignPlantTextures()
        {
            if (TextureBuilder.Albedo("T_PlantBark") == null) TextureBuilder.Build();
            var bark = TextureBuilder.Albedo("T_PlantBark"); var leaf = TextureBuilder.Albedo("T_PlantLeaf"); var cap = TextureBuilder.Albedo("T_PlantCap");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Dir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.shader == null || m.shader.name != "PlantGame/PlantGrow") continue;
                m.SetTexture("_BarkTex", bark); m.SetTexture("_LeafTex", leaf); m.SetTexture("_CapTex", cap);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// M_Keep_*: never shown, only listed in Resources/WorldKit so a player build carries the shaders and keyword variants that
        /// RuntimeMats and CameraOcclusionFade switch on in code (transparent Lit with/without emission and normal map, Particles/Unlit).
        /// </summary>
        static void BuildKeepers(Shader lit)
        {
            var particles = Shader.Find(RuntimeMats.ParticlesShader);
            foreach (var cutout in new[] { false, true })   // cutout = leaf cards that keep their alpha clip while they fade
            foreach (var emission in new[] { false, true })
                foreach (var normal in new[] { false, true })
                    Make("M_Keep_LitFade" + (cutout ? "Cutout" : "") + (emission ? "Emission" : "") + (normal ? "Normal" : ""), lit, m =>
                    {
                        RuntimeMats.SetTransparent(m);
                        m.SetFloat("_AlphaClip", cutout ? 1f : 0f); if (cutout) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
                        if (emission) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
                        if (normal) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
                    });
            if (particles == null) { Debug.LogError("[PlantGame] URP Particles/Unlit not found."); return; }
            Make("M_Keep_Particles", particles, m => RuntimeMats.SetTransparent(m));
            Make("M_Keep_ParticlesPremultiply", particles, m => { RuntimeMats.SetTransparent(m, true); m.SetFloat("_Blend", 1f); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); });
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

        static void SetTextured(Material m, string tex, float bump)
        {
            m.SetTexture("_BaseMap", TextureBuilder.Albedo(tex));
            m.SetTexture("_BumpMap", TextureBuilder.Normal(tex));
            m.SetFloat("_BumpScale", bump);
            m.EnableKeyword("_NORMALMAP");
        }

        static void SetEmission(Material m, Color hdr)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", hdr);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
    }
}
