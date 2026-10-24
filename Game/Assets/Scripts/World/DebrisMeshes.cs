using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Procedural low-poly shapes for the sky backdrop: jagged rocks (displaced icosphere) and double-pointed hexagonal crystals. Flat shaded, cached per seed.</summary>
    public static class DebrisMeshes
    {
        static readonly Dictionary<int, Mesh> Rocks = new Dictionary<int, Mesh>(), Crystals = new Dictionary<int, Mesh>();

        public static Mesh Rock(int seed)
        {
            seed &= 7;   // eight distinct rocks are plenty
            if (Rocks.TryGetValue(seed, out var m) && m != null) return m;
            var rnd = new System.Random(seed * 7777 + 5);
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            var f = new List<int[]>
            {
                new[] {0,11,5}, new[] {0,5,1}, new[] {0,1,7}, new[] {0,7,10}, new[] {0,10,11}, new[] {1,5,9}, new[] {5,11,4}, new[] {11,10,2}, new[] {10,7,6}, new[] {7,1,8},
                new[] {3,9,4}, new[] {3,4,2}, new[] {3,2,6}, new[] {3,6,8}, new[] {3,8,9}, new[] {4,9,5}, new[] {2,4,11}, new[] {6,2,10}, new[] {8,6,7}, new[] {9,8,1},
            };
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int i)) return i;
                v.Add((v[a] + v[b]) * 0.5f); mid[key] = v.Count - 1; return v.Count - 1;
            }
            var f2 = new List<int[]>();
            foreach (var tri in f) { int a = Mid(tri[0], tri[1]), b = Mid(tri[1], tri[2]), c = Mid(tri[2], tri[0]); f2.Add(new[] { tri[0], a, c }); f2.Add(new[] { tri[1], b, a }); f2.Add(new[] { tri[2], c, b }); f2.Add(new[] { a, b, c }); }
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * (0.7f + (float)rnd.NextDouble() * 0.45f);
            m = Flat("Rock" + seed, v, f2);
            Rocks[seed] = m; return m;
        }

        public static Mesh Crystal(int seed)
        {
            seed &= 3;
            if (Crystals.TryGetValue(seed, out var m) && m != null) return m;
            var rnd = new System.Random(seed * 313 + 9);
            const int N = 6;
            var v = new List<Vector3>(); var f = new List<int[]>();
            v.Add(new Vector3(0, -0.5f, 0));                         // 0 bottom tip
            for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2f / N; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f)); }                   // 1..6
            for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2f / N + 0.1f; v.Add(new Vector3(Mathf.Cos(a) * 0.46f, 1.2f + (float)rnd.NextDouble() * 0.2f, Mathf.Sin(a) * 0.46f)); }   // 7..12
            v.Add(new Vector3(0.05f, 2.0f + (float)rnd.NextDouble() * 0.5f, 0));   // 13 top tip
            for (int i = 0; i < N; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % N, c = 7 + i, d = 7 + (i + 1) % N;
                f.Add(new[] { 0, b, a });          // bottom point
                f.Add(new[] { a, b, c }); f.Add(new[] { b, d, c });   // side
                f.Add(new[] { c, d, 13 });         // top point
            }
            m = Flat("Crystal" + seed, v, f);
            Crystals[seed] = m; return m;
        }

        /// <summary>Unwelds the triangles so every face has its own normal (faceted look). Winding is fixed to face away from the origin.</summary>
        static Mesh Flat(string name, List<Vector3> verts, List<int[]> faces)
        {
            var vs = new List<Vector3>(); var tris = new List<int>();
            Vector3 centre = Vector3.zero; foreach (var p in verts) centre += p; centre /= verts.Count;
            foreach (var tri in faces)
            {
                Vector3 a = verts[tri[0]], b = verts[tri[1]], c = verts[tri[2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - centre) < 0f) { var tmp = b; b = c; c = tmp; }
                int i0 = vs.Count; vs.Add(a); vs.Add(b); vs.Add(c); tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
            }
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(vs); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var uv = new Vector2[vs.Count]; for (int i = 0; i < vs.Count; i++) uv[i] = new Vector2(vs[i].x + vs[i].z, vs[i].y + vs[i].z) * 0.7f;
            m.SetUVs(0, uv);
            return m;
        }
    }
}
