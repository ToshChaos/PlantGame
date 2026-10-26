using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Procedural leaf blade: a pointed, lanceolate outline lying along local X (tips at +/-X), width along Z, with a raised midrib fold
    /// and an arch along its length. Two-sided (the back is a second, flipped copy) so any material works. Used for the parachute
    /// leaf pickup and for the open glide leaf.
    /// </summary>
    public static class LeafMesh
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <param name="length">tip to tip, along X</param>
        /// <param name="width">widest point, along Z</param>
        /// <param name="arch">how far the middle rises over the tips (the tips droop)</param>
        /// <param name="fold">how far the edges lift over the midrib (0 = flat blade, positive = a shallow V)</param>
        public static Mesh Build(float length, float width, float arch, float fold, int segments = 28)
        {
            string key = $"{length:F2}/{width:F2}/{arch:F2}/{fold:F2}/{segments}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int rows = segments + 1;
            var v = new List<Vector3>(rows * 6); var uv = new List<Vector2>(rows * 6); var tris = new List<int>(segments * 12);
            for (int side = 0; side < 2; side++)   // 0 = top face, 1 = back face (flipped winding)
            {
                int o = v.Count;
                for (int i = 0; i < rows; i++)
                {
                    float t = i / (float)segments;
                    float x = (t - 0.5f) * length;
                    float hw = width * 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.82f))), 0.75f);   // widest a bit before the middle, pointed at both ends
                    hw *= 1f + 0.04f * Mathf.Sin(t * 70f) * Mathf.Sin(Mathf.PI * t);   // T67: slightly serrated edge
                    float e = 2f * t - 1f;
                    float y = arch * (1f - e * e);
                    float ye = y + fold * hw;
                    v.Add(new Vector3(x, ye, -hw)); v.Add(new Vector3(x, y, 0f)); v.Add(new Vector3(x, ye, hw));
                    uv.Add(new Vector2(t, 0f)); uv.Add(new Vector2(t, 0.5f)); uv.Add(new Vector2(t, 1f));
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = o + i * 3, b = a + 3;
                    // two quads per row: left half (edge, mid) and right half (mid, edge)
                    Quad(tris, a, a + 1, b, b + 1, side == 1);
                    Quad(tris, a + 1, a + 2, b + 1, b + 2, side == 1);
                }
            }
            var m = new Mesh { name = "LeafBlade" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            Cache[key] = m;
            return m;
        }

        /// <summary>
        /// The glide leaf as the player holds it: one big leaf blade arching over the head, its stem (petiole) running down to the hands
        /// on one side, the midrib is the blade's centre ridge. `parent` is the player model; returns the root ("GlideLeaf").
        /// </summary>
        public static GameObject CreateHeld(Transform parent, Material blade, Material stem)
        {
            var root = new GameObject("GlideLeaf");
            root.transform.SetParent(parent, false);
            const float L = 3.0f, W = 2.1f;
            var hand = new Vector3(0.5f, 1.25f, 0.15f); var ctrl = new Vector3(0.6f, 2.1f, -0.2f); var basePt = new Vector3(0.1f, 2.45f, -1.5f);   // the stem is held in the right hand and runs up beside the body
            var tilt = Quaternion.Euler(-6f, 0f, 0f);            // nearly level, low enough for the chase camera to see the top
            var b = new GameObject("Blade"); b.transform.SetParent(root.transform, false);
            b.transform.localPosition = basePt + tilt * new Vector3(0f, 0f, L * 0.5f);
            b.transform.localRotation = tilt * Quaternion.Euler(0f, -90f, 0f);   // blade tip (+X) points forward
            b.AddComponent<MeshFilter>().sharedMesh = Build(L, W, 0.4f, 0.1f, 28);
            var mr = b.AddComponent<MeshRenderer>(); mr.sharedMaterial = blade; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // petiole: a curved stem from the hands up to the base of the blade
            Vector3 prev = hand;
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f; var q = (1 - t) * (1 - t) * hand + 2 * (1 - t) * t * ctrl + t * t * basePt;
                Segment(root.transform, prev, q, 0.075f - 0.02f * t, stem); prev = q;
            }
            return root;
        }

        static void Segment(Transform parent, Vector3 a, Vector3 b, float radius, Material m)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(parent, false);
            c.transform.localPosition = (a + b) * 0.5f; c.transform.localScale = new Vector3(radius * 2f, (b - a).magnitude * 0.5f, radius * 2f);
            c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            var r = c.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void Quad(List<int> t, int a0, int a1, int b0, int b1, bool flip)
        {
            if (!flip) { t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1); }
            else { t.Add(a0); t.Add(a1); t.Add(b0); t.Add(a1); t.Add(b1); t.Add(b0); }
        }
    }
}
