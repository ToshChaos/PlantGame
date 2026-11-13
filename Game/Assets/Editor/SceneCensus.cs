using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantGame.Editor
{
    /// <summary>
    /// PlantGame > Test > Scene Census: counts what the loaded scene would draw (enabled MeshRenderers: triangles, shadow casters, mesh memory) and
    /// lists the heaviest groups, meshes and materials, so an asset pass can see where the triangles are. Run it in play mode on a generated level
    /// (on Medium / Low wait a frame after the load: WorldOptimizer merges then). Writes Logs/census_&lt;tier&gt;_&lt;level&gt;.txt and returns the summary line.
    /// Counts every renderer in the scene, not only what the camera sees: compare runs with each other, not with the Stats window.
    /// </summary>
    public static class SceneCensus
    {
        [MenuItem("PlantGame/Test/Scene Census", false, 43)]
        static void Menu() => Debug.Log("[PlantGame] " + Run());

        /// <summary>A hierarchy name without its trailing index ("Island_3" and "Island_4" count as one group).</summary>
        static string Group(string s)
        {
            int e = s.Length;
            while (e > 0 && (char.IsDigit(s[e - 1]) || s[e - 1] == '_' || s[e - 1] == ' ')) e--;
            return s.Substring(0, e);
        }

        public static string Run(int top = 50)
        {
            var byMesh = new Dictionary<string, (int n, long tris, int verts, int casters)>();
            var byGroup = new Dictionary<string, (int n, long tris, int casters)>();
            var byMat = new Dictionary<string, (int n, long tris)>();
            var meshes = new HashSet<Mesh>();
            long total = 0; int renderers = 0, casters = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (!r.enabled || mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh; long t = 0;
                for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
                renderers++; total += t; meshes.Add(m);
                bool cast = r.shadowCastingMode != ShadowCastingMode.Off; if (cast) casters++;
                byMesh.TryGetValue(m.name, out var e); byMesh[m.name] = (e.n + 1, e.tris + t, m.vertexCount, e.casters + (cast ? 1 : 0));
                var chain = new List<string>();   // root / child / grandchild, e.g. World/Island/Scenery
                for (var p = r.transform; p != null; p = p.parent) chain.Add(Group(p.name));
                chain.Reverse();
                string group = string.Join("/", chain.Take(3));
                byGroup.TryGetValue(group, out var g); byGroup[group] = (g.n + 1, g.tris + t, g.casters + (cast ? 1 : 0));
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    string key = mat.name + " [" + (mat.shader != null ? mat.shader.name.Replace("Universal Render Pipeline/", "URP/") : "?") + "]";
                    byMat.TryGetValue(key, out var mm); byMat[key] = (mm.n + 1, mm.tris + t);
                }
            }
            long drawnMem = meshes.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong);
            var loaded = Resources.FindObjectsOfTypeAll<Mesh>();
            string tier = PlantGame.Core.QualityTier.Names[Mathf.Clamp(PlantGame.Core.GameSettings.Quality, 0, 2)];
            string level = Application.isPlaying ? PlantGame.Core.GameBootstrap.Current.levelId ?? "seed" : "edit";
            string head = $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} {level} {tier}: {renderers} renderers, {total} triangles, {casters} shadow casters, " +
                          $"{meshes.Count} meshes drawn ({drawnMem / 1024} KB), {loaded.Length} meshes loaded ({loaded.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong) / 1024} KB, editor count)";
            var sb = new StringBuilder();
            sb.AppendLine(head);
            sb.AppendLine("== groups by triangles");
            foreach (var kv in byGroup.OrderByDescending(k => k.Value.tris).Take(top)) sb.AppendLine($"{kv.Key}: {kv.Value.n} renderers, {kv.Value.tris} tris, {kv.Value.casters} casters");
            sb.AppendLine("== meshes by total triangles");
            foreach (var kv in byMesh.OrderByDescending(k => k.Value.tris).Take(top)) sb.AppendLine($"{kv.Key}: {kv.Value.n} x {kv.Value.tris / kv.Value.n} = {kv.Value.tris} tris ({kv.Value.verts} verts), {kv.Value.casters} casters");
            sb.AppendLine("== meshes by instance count");
            foreach (var kv in byMesh.OrderByDescending(k => k.Value.n).Take(top / 2)) sb.AppendLine($"{kv.Key}: {kv.Value.n} instances, {kv.Value.tris} tris");
            sb.AppendLine("== materials by triangles");
            foreach (var kv in byMat.OrderByDescending(k => k.Value.tris).Take(top)) sb.AppendLine($"{kv.Key}: {kv.Value.n} renderers, {kv.Value.tris} tris");
            Directory.CreateDirectory("Logs");
            string path = $"Logs/census_{tier}_{level}.txt";
            File.WriteAllText(path, sb.ToString());
            return head + " -> " + path;
        }
    }
}
