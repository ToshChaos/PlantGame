using System.Text;
using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// T02: verifies the Blender round trip using Assets/Art/Models/Test/Test_Asym.fbx
    /// (made by Source/Blender/scripts/t02_make_test_object.py). Menu: PlantGame > Tools > Verify Pipeline Round-trip.
    /// Prints measured size, growth_t readability, Anchor position and the side of the arm/nub, so the X-mirror
    /// question from AGENTS.md section 2 can be answered with numbers.
    /// </summary>
    public static class PipelineRoundTripTest
    {
        const string Path = "Assets/Art/Models/Test/Test_Asym.fbx";

        [MenuItem("PlantGame/Tools/Verify Pipeline Round-trip")]
        public static void Run()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if (go == null) { Debug.LogError("[PlantGame] " + Path + " not found. Export Test_Asym from Blender and run sync_to_unity.py."); return; }

            var sb = new StringBuilder("[PlantGame] Round-trip report for Test_Asym\n");
            var mf = go.GetComponentInChildren<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null) { Debug.LogError("[PlantGame] no mesh in " + Path); return; }

            var b = mesh.bounds;   // Blender: base 1x1x1 (x,y -0.5..0.5, z 0..1), arm to +X (x up to 1.5), nub +Y (y up to 0.8)
            sb.AppendLine($"mesh bounds size {b.size} centre {b.center}  (expected size ~ (2.0, 1.0..1.15, 1.8) in some axis order)");
            sb.AppendLine($"height (Unity Y) {b.size.y:F3} m  — expected 1.0 m if scale and up-axis are right");

            var cols = mesh.colors;
            if (cols == null || cols.Length == 0) sb.AppendLine("FAIL growth_t: mesh.colors is empty (vertex colours not imported)");
            else
            {
                float min = 1f, max = 0f;
                foreach (var c in cols) { min = Mathf.Min(min, c.r); max = Mathf.Max(max, c.r); }
                sb.AppendLine($"OK   growth_t via mesh.colors: {cols.Length} entries, R range {min:F2}..{max:F2} (expected 0..1)");
            }

            var anchor = FindChild(go.transform, "Anchor");
            sb.AppendLine(anchor != null ? $"OK   Anchor found at local position {anchor.localPosition}" : "FAIL Anchor child not found");

            // Arm direction: extent of the bounds along X on each side of the base (x in -0.5..0.5 for the base).
            float extentPlusX = b.max.x, extentMinusX = -b.min.x;
            sb.AppendLine($"arm extent +X {extentPlusX:F2}, -X {extentMinusX:F2}. In Blender the arm is on +X (1.5 m) and the base reaches -0.5 m on -X.");
            sb.AppendLine(extentPlusX > extentMinusX
                ? "     => arm stays on +X: X is NOT mirrored."
                : "     => arm is on -X: X IS MIRRORED. Fix once in export_fbx.py and document it in docs/pipeline.md.");
            Debug.Log(sb.ToString());
        }

        static Transform FindChild(Transform t, string name)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }
    }
}
