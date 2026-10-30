using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// T63: makes a generated world cheaper to draw on the Medium / Low tiers (QualityTier.Lite). Runs once, a frame after the world is
    /// built, and only touches rendering, never layout, colliders or randomness, so seed codes and solvability are unchanged:
    /// - Unity's 768-triangle primitive spheres (leaves, petals, berries) become a 144-triangle sphere;
    /// - props smaller than 2.5 m cast no shadows;
    /// - static props are merged into one mesh per material (and shadow mode) under each island ("Merged_*"; the island may still bob)
    ///   and their own renderers switched off. Big scenery (trees, ruins) stays separate so CameraOcclusionFade can still fade it;
    /// - Low only (QualityTier.Thin): every second small decoration renderer is hidden.
    /// Anything under a script that may move, animate or highlight it (seed bobbing, the goal's cloth and doors, butterflies, sparkles,
    /// falling bits, plants, soil patches, water) and anything not drawn with a stock URP shader is left alone.
    /// </summary>
    public static class WorldOptimizer
    {
        const float SmallProp = 2.5f, ThinSize = 1.2f;
        static Mesh _sphere;

        /// <summary>Scripts that never move their children (the goal: except its cloth and doors, checked separately).</summary>
        static bool Still(MonoBehaviour mb) => mb is Inventory.SeedTree || mb is Audio.SeedAmbience || mb is Audio.LoopEmitter || mb is Audio.RuinSounds
            || mb is IslandSpawn || mb is GoalFlag;

        static bool StockShader(Material m) => m != null && m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/");

        public static void Apply(Transform worldRoot)
        {
            if (worldRoot == null || !QualityTier.Lite) return;
            if (_sphere == null) { _sphere = ShapeMeshes.Ellipsoid(0.5f, 0.5f, 12, 7); _sphere.name = "LowSphere"; }
            int merged = 0, meshes = 0, thinned = 0;
            var groups = new Dictionary<(Material, bool), List<CombineInstance>>();
            foreach (Transform island in worldRoot)
            {
                if (island.GetComponent<IslandTerrain>() == null) continue;
                groups.Clear();
                var toLocal = island.worldToLocalMatrix;
                int k = 0;
                foreach (var r in island.GetComponentsInChildren<MeshRenderer>())
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (!r.enabled || mf == null || mf.sharedMesh == null) continue;
                    if (mf.sharedMesh.name == "Sphere") mf.sharedMesh = _sphere;
                    if (r.transform == island || !Static(r.transform, island, out bool fadeable)) continue;
                    var size = r.bounds.size; float big = Mathf.Max(size.x, size.y, size.z);
                    if (big < SmallProp) r.shadowCastingMode = ShadowCastingMode.Off;
                    if (QualityTier.Thin && big < ThinSize && (k++ & 1) == 1) { r.enabled = false; thinned++; continue; }
                    if (fadeable && big >= SmallProp) continue;   // trees and ruins: CameraOcclusionFade fades them one by one
                    var mesh = mf.sharedMesh; var mats = r.sharedMaterials;
                    if (!mesh.isReadable || mats.Length == 0) continue;
                    bool stock = true;
                    foreach (var m in mats) if (!StockShader(m)) { stock = false; break; }
                    if (!stock) continue;
                    bool casts = r.shadowCastingMode != ShadowCastingMode.Off;
                    var matrix = toLocal * r.localToWorldMatrix;
                    for (int s = 0; s < Mathf.Min(mesh.subMeshCount, mats.Length); s++)
                    {
                        if (!groups.TryGetValue((mats[s], casts), out var list)) groups[(mats[s], casts)] = list = new List<CombineInstance>();
                        list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = matrix });
                    }
                    r.enabled = false; merged++;
                }
                foreach (var kv in groups)
                {
                    var mesh = new Mesh { name = "Merged_" + kv.Key.Item1.name, indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                    mesh.UploadMeshData(true);   // GPU only from here: no CPU copy
                    var go = new GameObject(mesh.name); go.transform.SetParent(island, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = kv.Key.Item1;
                    mr.shadowCastingMode = kv.Key.Item2 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    meshes++;
                }
            }
            Debug.Log($"[PlantGame] World pass ({QualityTier.Names[GameSettings.Quality]}): {merged} renderers merged into {meshes} meshes, {thinned} hidden.");
        }

        /// <summary>True when nothing between the renderer and its island moves it; fadeable when it sits under Scenery / SideDecor.</summary>
        static bool Static(Transform t, Transform island, out bool fadeable)
        {
            fadeable = false;
            for (var p = t; p != null && p != island; p = p.parent)
            {
                if (p.name == "Scenery" || p.name == "SideDecor") fadeable = true;
                foreach (var mb in p.GetComponents<MonoBehaviour>())
                {
                    if (mb == null || !Still(mb)) return false;
                    if (mb is GoalFlag g && (Under(t, g.cloth) || Under(t, g.doorLeft) || Under(t, g.doorRight))) return false;
                }
            }
            return true;
        }

        static bool Under(Transform t, Transform root) => root != null && (t == root || t.IsChildOf(root));
    }
}
