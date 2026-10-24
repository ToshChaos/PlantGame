using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Procedural tree pieces for the sandbox seed trees: a tapered, flared, slightly wobbly trunk (lathe) with bark UVs.</summary>
    public static class TreeParts
    {
        /// <summary>Trunk from y = 0 up to `height`; radius `baseR` at the root flare tapering to `topR`.</summary>
        public static Mesh Trunk(float height, float baseR, float topR, int seed)
        {
            const int Seg = 14;
            float[] ys = { 0f, 0.07f, 0.25f, 0.55f, 0.8f, 1f };
            float[] rs = { 1.45f, 1.18f, 0.9f, 0.78f, 0.7f, 0.62f };   // multiples of the linear taper: root flare, then slim
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * 6.28f, p2 = (float)rnd.NextDouble() * 6.28f;
            var verts = new System.Collections.Generic.List<Vector3>(); var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int r = 0; r < ys.Length; r++)
            {
                float t = ys[r], rad = Mathf.Lerp(baseR, topR, t) * rs[r];
                for (int i = 0; i <= Seg; i++)
                {
                    float a = i * Mathf.PI * 2f / Seg;
                    float w = 1f + 0.08f * Mathf.Sin(a * 3f + p1 + t * 4f) + 0.05f * Mathf.Sin(a * 5f + p2);   // ridged, not a perfect cylinder
                    verts.Add(new Vector3(Mathf.Cos(a) * rad * w + 0.12f * Mathf.Sin(t * 3f + p1) * t, t * height, Mathf.Sin(a) * rad * w));
                    uvs.Add(new Vector2(i / (float)Seg * 2f, t * height * 0.5f));
                }
            }
            for (int r = 0; r < ys.Length - 1; r++)
                for (int i = 0; i < Seg; i++)
                {
                    int a = r * (Seg + 1) + i, b = a + 1, c = a + Seg + 1, d = c + 1;
                    tris.AddRange(new[] { a, b, c, b, d, c });   // outward-facing (counter-clockwise seen from outside, Unity is clockwise: checked below)
                }
            var m = new Mesh { name = "Trunk" };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            // make sure the faces point outward
            var t0 = m.triangles; var v = m.vertices;
            var n = Vector3.Cross(v[t0[1]] - v[t0[0]], v[t0[2]] - v[t0[0]]);
            var outward = new Vector3(v[t0[0]].x, 0, v[t0[0]].z);
            if (Vector3.Dot(n, outward) < 0f) { for (int i = 0; i < t0.Length; i += 3) { int tmp = t0[i + 1]; t0[i + 1] = t0[i + 2]; t0[i + 2] = tmp; } m.triangles = t0; }
            m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }
    }
}
