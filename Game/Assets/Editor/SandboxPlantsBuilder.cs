using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.Editor
{
    /// <summary>
    /// T01: builds Sandbox_Plants: material swatches, a lighting preset (fog/ambient from the palette)
    /// and a stretched, subdivided stem with a vertex-colour growth_t gradient that sweeps _Growth 0 -> 1.
    /// Menu: PlantGame > 3 Build Sandbox_Plants. Rebuilds (overwrites) the scene each time.
    /// </summary>
    public static class SandboxPlantsBuilder
    {
        const string MeshPath = "Assets/Art/Models/Test/GrowTestStem.asset";
        public const int Segments = 24;   // minimum rings A3 should give a bridge (see art bible)

        [MenuItem("PlantGame/3 Build Sandbox_Plants")]
        public static void Build()
        {
            if (MaterialBuilder.Load("M_PlantGrow") == null) MaterialBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyLightingPreset();
            ProjectSetup.AddSun();

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.MistBlue;
            camGo.transform.position = new Vector3(0f, 2.6f, -11f);
            camGo.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

            // Ground slab
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(24f, 0.5f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialBuilder.Load("M_Stone");

            // Material swatches
            string[] names = { "M_Terrain", "M_Stone", "M_Foliage", "M_Cloud", "M_Crystal" };
            for (int i = 0; i < names.Length; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Swatch_" + names[i];
                s.transform.position = new Vector3(-9f + i * 2.2f, 1f, 0f);
                s.GetComponent<Renderer>().sharedMaterial = MaterialBuilder.Load(names[i]);
            }
            var ui = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ui.name = "Swatch_M_UI";
            ui.transform.position = new Vector3(-9f + names.Length * 2.2f, 1f, 0f);
            ui.GetComponent<Renderer>().sharedMaterial = MaterialBuilder.Load("M_UI");

            // Growth test stems
            var mesh = EnsureStemMesh();
            var growMat = MaterialBuilder.Load("M_PlantGrow");
            MakeStem("GrowTestStem_Sweep", mesh, growMat, new Vector3(5.5f, 0f, 0f), autoSweep: true, growth: 0f);
            MakeStem("GrowTestStem_Half", mesh, growMat, new Vector3(7.5f, 0f, 0f), autoSweep: false, growth: 0.5f);
            MakeStem("GrowTestStem_Full", mesh, growMat, new Vector3(9.5f, 0f, 0f), autoSweep: false, growth: 1f);

            Directory.CreateDirectory(ProjectSetup.ScenesDir);
            EditorSceneManager.SaveScene(scene, ProjectSetup.ScenePath("Sandbox_Plants"));
            ProjectSetup.RegisterScenes();
            Debug.Log("[PlantGame] Sandbox_Plants built. Press Play: the left stem sweeps _Growth 0 -> 1.");
        }

        /// <summary>Soft fantasy ambient + fog from the palette. Final sky/atmosphere is T16.</summary>
        public static void ApplyLightingPreset()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.MistBlue;
            RenderSettings.ambientEquatorColor = Palette.CloudWhite;
            RenderSettings.ambientGroundColor = Palette.Moss;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.MistBlue;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 220f;
        }

        static void MakeStem(string name, Mesh mesh, Material mat, Vector3 pos, bool autoSweep, float growth)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var sweep = go.AddComponent<GrowSweep>();
            sweep.autoSweep = autoSweep;
            sweep.growth = growth;
            sweep.Apply();
        }

        static Mesh EnsureStemMesh()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
            var mesh = BuildStem(width: 0.6f, height: 4f, segments: Segments);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, MeshPath);
            else { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            return mesh;
        }

        /// <summary>
        /// Square-section stem along +Y (pivot at the base). Vertex colour R = growth_t = y / height.
        /// Four separate sides (hard normals), 'segments' quads along the length, plus a top cap.
        /// Winding: cross(b - a, c - a) points along the outward normal (verified for Unity's clockwise front faces).
        /// </summary>
        public static Mesh BuildStem(float width, float height, int segments)
        {
            float h = width * 0.5f;
            var corners = new[] { new Vector2(-h, -h), new Vector2(h, -h), new Vector2(h, h), new Vector2(-h, h) }; // x,z
            var normals = new[] { new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(-1, 0, 0) };

            var verts = new System.Collections.Generic.List<Vector3>();
            var norms = new System.Collections.Generic.List<Vector3>();
            var cols = new System.Collections.Generic.List<Color>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            for (int side = 0; side < 4; side++)
            {
                var a = corners[side];
                var b = corners[(side + 1) % 4];
                int start = verts.Count;
                for (int row = 0; row <= segments; row++)
                {
                    float t = row / (float)segments;
                    float y = t * height;
                    verts.Add(new Vector3(a.x, y, a.y)); norms.Add(normals[side]); cols.Add(new Color(t, 0, 0, 1)); uvs.Add(new Vector2(0, t));
                    verts.Add(new Vector3(b.x, y, b.y)); norms.Add(normals[side]); cols.Add(new Color(t, 0, 0, 1)); uvs.Add(new Vector2(1, t));
                }
                for (int row = 0; row < segments; row++)
                {
                    int v00 = start + row * 2, v10 = v00 + 1, v01 = v00 + 2, v11 = v00 + 3;
                    tris.AddRange(new[] { v00, v01, v11, v00, v11, v10 });
                }
            }
            // top cap (growth_t = 1)
            int c = verts.Count;
            verts.Add(new Vector3(-h, height, -h)); verts.Add(new Vector3(-h, height, h));
            verts.Add(new Vector3(h, height, h));   verts.Add(new Vector3(h, height, -h));
            for (int i = 0; i < 4; i++) { norms.Add(Vector3.up); cols.Add(new Color(1, 0, 0, 1)); }
            uvs.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            tris.AddRange(new[] { c, c + 1, c + 2, c, c + 2, c + 3 });

            var m = new Mesh { name = "GrowTestStem", indexFormat = IndexFormat.UInt16 };
            m.SetVertices(verts); m.SetNormals(norms); m.SetColors(cols); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
