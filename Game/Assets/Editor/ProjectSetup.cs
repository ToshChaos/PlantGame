using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PlantGame.Editor
{
    /// <summary>
    /// T00: idempotent project setup. Menu: PlantGame > 1 Setup Project.
    /// Sets Linear colour space, creates layers/tags (AGENTS.md 5.6), the physics matrix and the empty scenes.
    /// Safe to run repeatedly.
    /// </summary>
    public static class ProjectSetup
    {
        // Fixed layer indices so masks are stable between machines.
        static readonly (int index, string name)[] LayerSlots =
        {
            (8, "Ground"), (9, "Plant"), (10, "Climbable"), (11, "Interactable"), (13, "Player"),   // "Water" is a built-in layer (index 4)
        };

        public const string ScenesDir = "Assets/Scenes";
        public static readonly string[] SceneNames = { "Title", "Main", "Sandbox_Gameplay" };

        [MenuItem("PlantGame/1 Setup Project")]
        public static void Run()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureLayers();
            EnsureTags();
            EnsureUrp();
            EnsurePhysicsMatrix();
            EnsureEmptyScene("Main", addLight: true);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Project setup done: Linear colour space, layers, tags, physics matrix, base scenes.");
        }

        /// <summary>Soft fantasy ambient + fog from the palette (the title, gameplay course and game scenes start from it).</summary>
        public static void ApplyLightingPreset()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = PlantGame.Core.Palette.MistBlue;
            RenderSettings.ambientEquatorColor = PlantGame.Core.Palette.CloudWhite;
            RenderSettings.ambientGroundColor = PlantGame.Core.Palette.Moss;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = PlantGame.Core.Palette.MistBlue;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 220f;
        }

        static void EnsureLayers()
        {
            var tm = LoadSettings("ProjectSettings/TagManager.asset");
            var layers = tm.FindProperty("layers");
            foreach (var (index, name) in LayerSlots)
            {
                var slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name) continue;
                if (string.IsNullOrEmpty(slot.stringValue)) { slot.stringValue = name; continue; }
                Debug.LogWarning($"[PlantGame] Layer slot {index} is taken by '{slot.stringValue}'; cannot create '{name}' there.");
            }
            var dup = layers.GetArrayElementAtIndex(12);
            if (dup.stringValue == "Water") dup.stringValue = "";   // created by an earlier version; Water is built-in layer 4
            tm.ApplyModifiedProperties();
        }

        /// <summary>Creates a URP asset + renderer if none is assigned and wires it to Graphics and all Quality levels.</summary>
        static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            const string dir = "Assets/Settings";
            Directory.CreateDirectory(dir);
            var rendererPath = dir + "/PlantGame_Renderer.asset";
            var assetPath = dir + "/PlantGame_URP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, assetPath);
            }
            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] URP asset created and assigned: " + assetPath);
        }

        static void EnsureTags()
        {
            // "Player" and "Respawn" are built-in Unity tags; this only guards against an unusual template.
            foreach (var tag in new[] { "Player", "Respawn" })
            {
                try { UnityEngine.GameObject.FindGameObjectsWithTag(tag); }
                catch (UnityException) { UnityEditorInternal.InternalEditorUtility.AddTag(tag); }
            }
        }

        // m_LayerCollisionMatrix[a] bit b = layers a and b collide.
        static void EnsurePhysicsMatrix()
        {
            int L(string n) => LayerMask.NameToLayer(n);
            int ground = L("Ground"), plant = L("Plant"), climb = L("Climbable"), inter = L("Interactable"),
                water = L("Water"), player = L("Player");
            if (ground < 0 || player < 0) { Debug.LogWarning("[PlantGame] Layers missing; skipping physics matrix."); return; }

            var rules = new (int a, int b, bool collide)[]
            {
                // Player collides with Ground, Plant; Climbable / Interactable / Water are triggers it must be able to detect.
                (player, 0, true), (player, ground, true), (player, plant, true),
                (player, climb, true), (player, inter, true), (player, water, true),
                (player, player, false),
                // Dropped items (Interactable, rigidbody) rest on the ground; everything else among new layers stays off.
                (inter, ground, true), (inter, plant, false), (inter, 0, true),
                (ground, 0, true), (plant, 0, true), (plant, ground, false),
                (climb, ground, false), (climb, plant, false), (climb, inter, false), (climb, water, false), (climb, 0, false),
                (water, ground, false), (water, plant, false), (water, inter, false), (water, 0, false),
            };

            try
            {
                var dm = LoadSettings("ProjectSettings/DynamicsManager.asset");
                var matrix = dm.FindProperty("m_LayerCollisionMatrix");
                foreach (var (a, b, collide) in rules)
                {
                    SetBit(matrix, a, b, collide);
                    SetBit(matrix, b, a, collide);
                }
                dm.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PlantGame] Could not edit DynamicsManager directly (" + e.Message + "); falling back to Physics.IgnoreLayerCollision.");
                foreach (var (a, b, collide) in rules) Physics.IgnoreLayerCollision(a, b, !collide);
            }
        }

        static void SetBit(SerializedProperty matrix, int row, int bit, bool on)
        {
            var el = matrix.GetArrayElementAtIndex(row);
            uint v = unchecked((uint)el.longValue);
            v = on ? v | (1u << bit) : v & ~(1u << bit);
            el.longValue = v;
        }

        static SerializedObject LoadSettings(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0) throw new FileNotFoundException(path);
            return new SerializedObject(assets[0]);
        }

        public static string ScenePath(string name) => $"{ScenesDir}/{name}.unity";

        /// <summary>Creates an empty scene file if missing; never overwrites an existing scene.</summary>
        public static void EnsureEmptyScene(string name, bool addLight)
        {
            var path = ScenePath(name);
            if (File.Exists(path)) return;
            Directory.CreateDirectory(ScenesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (addLight) AddSun();
            EditorSceneManager.SaveScene(scene, path);
        }

        public static Light AddSun()
        {
            var go = new GameObject("Directional Light");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.97f, 0.9f);
            l.intensity = 1.1f;
            l.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = l;
            return l;
        }

        /// <summary>Main first, then the sandboxes; only scenes that exist on disk.</summary>
        public static void RegisterScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var n in SceneNames)
                if (File.Exists(ScenePath(n))) list.Add(new EditorBuildSettingsScene(ScenePath(n), true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
