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
        public static Mesh Build(float length, float width, float arch, float fold, int segments = 16)
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

        static void Quad(List<int> t, int a0, int a1, int b0, int b1, bool flip)
        {
            if (!flip) { t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1); }
            else { t.Add(a0); t.Add(a1); t.Add(b0); t.Add(a1); t.Add(b1); t.Add(b0); }
        }
    }
}
