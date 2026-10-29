using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// Island terrain: builds the mesh from the shape + noise and a list of carve discs (water basins, flat spots), answers height
    /// queries so scenery can sit on the bumps, and builds the flat water surface for the carved hollows.
    /// </summary>
    public class IslandTerrain : MonoBehaviour
    {
        public const float WaterLevel = -0.14f, BasinDepth = -0.55f, Amp = 0.9f, Shoulder = -0.5f;   // Shoulder: how far the top surface curves down at the very rim
        [System.NonSerialized] public IslandShape shape; public float depth, tile = 3f; public int seed;
        /// <summary>x, z (world), radius, bed height (local y; BasinDepth for water, 0 to flatten).</summary>
        public readonly List<Vector4> carves = new List<Vector4>();
        public static readonly List<IslandTerrain> All = new List<IslandTerrain>();

        const int Segments = 96;
        float o1, o2, o3, o4, o5, o6;
        Mesh _mesh; int _rings;
        float[] _ringH;   // heights of the top grid, for the water surface

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Init()
        {
            var r = new System.Random(seed * 7919 + 101);
            float F() => (float)r.NextDouble() * 100f;
            o1 = F(); o2 = F(); o3 = F(); o4 = F(); o5 = F(); o6 = F();
        }

        /// <summary>Outer rim radius: the logical edge plus a ragged, outward-only extra (so layout distances stay valid).</summary>
        public float Rim(float a)
        {
            float j = Mathf.PerlinNoise(Mathf.Cos(a) * 2.2f + o1, Mathf.Sin(a) * 2.2f + o2) * 0.55f
                    + Mathf.PerlinNoise(Mathf.Cos(a) * 6f + o3, Mathf.Sin(a) * 6f + o4) * 0.45f;
            return shape.Edge(a) + 0.9f * j * j;
        }

        float Noise(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.11f + o5, z * 0.11f + o6) * 0.65f + Mathf.PerlinNoise(x * 0.31f + o2, z * 0.31f + o3) * 0.35f;
            return Mathf.Clamp01((n - 0.25f) * 1.5f) * Amp;
        }

        /// <summary>Local height of the top surface at local (x, z); `u` = 0 centre .. 1 rim.</summary>
        float LocalHeight(float x, float z, float u)
        {
            float h = Noise(x, z) * (1f - Smooth((u - 0.78f) / 0.22f));
            var wc = transform.position;
            float d = float.MaxValue, bed = 0f;
            foreach (var c in carves)
            {
                float dx = x - (c.x - wc.x), dz = z - (c.y - wc.z);
                float dd = Mathf.Sqrt(dx * dx + dz * dz) - c.z;
                if (dd < d) { d = dd; bed = c.w; }
            }
            if (d < 1.4f)
            {
                d += (Mathf.PerlinNoise(x * 0.5f + o4, z * 0.5f + o1) - 0.5f) * 0.9f;   // wobbly shoreline
                float s = Smooth((1.2f - d) / 1.7f);
                h = Mathf.Lerp(h, bed, s);
            }
            return h + Shoulder * Smooth((u - 0.86f) / 0.14f);   // rounded shoulder instead of a sharp edge
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>Height above the island's reference plane at a world position (0 off the island).</summary>
        public float HeightAtWorld(Vector3 p)
        {
            var c = transform.position; float x = p.x - c.x, z = p.z - c.z;
            float a = Mathf.Atan2(z, x), rim = Rim(a), r = Mathf.Sqrt(x * x + z * z);
            if (r >= rim) return 0f;
            return LocalHeight(x, z, r / rim);
        }

        /// <summary>Highest island terrain under a world position (null-safe lookup over all islands in the world).</summary>
        public static float Sample(Vector3 p)
        {
            foreach (var t in All) { if (t == null) continue; var c = t.transform.position; float dx = p.x - c.x, dz = p.z - c.z; if (dx * dx + dz * dz < 900f && Mathf.Abs(p.y - c.y) < 4f) { float h = t.HeightAtWorld(p); var rim = t.Rim(Mathf.Atan2(dz, dx)); if (dx * dx + dz * dz < rim * rim) return h; } }
            return 0f;
        }

        public void Rebuild()
        {
            Init();
            float maxR = shape.MaxEdge + 1f;
            _rings = Mathf.Clamp(Mathf.CeilToInt(maxR / 0.5f), 12, 40);
            var verts = new List<Vector3>(); var uvs = new List<Vector2>();
            var topTris = new List<int>(); var sideTris = new List<int>();
            var topCentre = transform.position;

            // ---- top: concentric rings from the centre to the rim
            var ringStart = new int[_rings + 1];
            _ringH = new float[(_rings + 1) * Segments];
            int c0 = verts.Count; verts.Add(new Vector3(0, LocalHeight(0, 0, 0), 0)); uvs.Add(new Vector2(topCentre.x / tile, topCentre.z / tile));
            for (int k = 1; k <= _rings; k++)
            {
                ringStart[k] = verts.Count;
                float u = k / (float)_rings;
                for (int i = 0; i < Segments; i++)
                {
                    float a = i * Mathf.PI * 2f / Segments, rr = Rim(a) * u;
                    float x = Mathf.Cos(a) * rr, z = Mathf.Sin(a) * rr;
                    float h = LocalHeight(x, z, u);
                    _ringH[k * Segments + i] = h;
                    verts.Add(new Vector3(x, h, z)); uvs.Add(new Vector2((topCentre.x + x) / tile, (topCentre.z + z) / tile));
                }
            }
            for (int i = 0; i < Segments; i++) { int a = ringStart[1] + i, b = ringStart[1] + (i + 1) % Segments; topTris.AddRange(new[] { c0, b, a }); }
            for (int k = 1; k < _rings; k++)
                for (int i = 0; i < Segments; i++)
                {
                    int j = (i + 1) % Segments;
                    int a = ringStart[k] + i, b = ringStart[k] + j, c = ringStart[k + 1] + i, d = ringStart[k + 1] + j;
                    topTris.AddRange(new[] { a, b, c, b, d, c });
                }

            // ---- sides: profile rings (radius scale, y) from the rim down to the underside tip, with rugged noise growing with depth
            float[] sc = { 1f, 0.997f, 0.97f, 0.85f, 0.64f, 0.38f, 0f };
            float[] yy = { Shoulder, -2.4f, -depth * 0.3f, -depth * 0.5f, -depth * 0.72f, -depth * 0.9f, -depth };   // a tall, nearly vertical rim wall (thick vertically, not wider), then the rock tapering down
            int rings = sc.Length, s0 = verts.Count;
            for (int r = 0; r < rings; r++)
                for (int i = 0; i <= Segments; i++)
                {
                    float a = i * Mathf.PI * 2f / Segments;
                    float rim = Rim(a);
                    float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                    float n1 = Mathf.PerlinNoise(cx * 2.5f + o1 + r * 0.7f, sz * 2.5f + o2) - 0.5f;
                    float n2 = Mathf.PerlinNoise(cx * 6f + o3, sz * 6f + o4 + r * 1.3f) - 0.5f;
                    float lump = 1f - r * 0.1f * (0.5f + n1 * 1.2f + n2 * 0.9f);   // inward only past the rim, so the top never overhangs
                    lump = Mathf.Min(lump, 1f);
                    float rr = rim * sc[r] * lump;
                    float dy = r == 0 ? 0f : (n1 * 0.7f + n2 * 0.5f) * 0.45f * r;
                    float y0 = r == 0 ? _ringH[_rings * Segments + i % Segments] : yy[r];   // ring 0 follows the top rim exactly (carved hollows at the rim, e.g. waterfall streams, would otherwise leave a gap)
                    verts.Add(new Vector3(cx * rr, y0 + dy, sz * rr));
                    uvs.Add(new Vector2(i * (rim * Mathf.PI * 2f / Segments) / tile, yy[r] / tile));
                }
            for (int r = 0; r < rings - 1; r++)
                for (int i = 0; i < Segments; i++)
                {
                    int a = s0 + r * (Segments + 1) + i, b = a + 1, c = a + Segments + 1, d = c + 1;
                    sideTris.AddRange(new[] { a, b, c, b, d, c });   // outward winding for the whole wall: a per-triangle flip left holes where the rugged noise tilted a face
                }

            if (_mesh == null) _mesh = new Mesh { name = name + "_Mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            else _mesh.Clear();
            _mesh.SetVertices(verts); _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(topTris, 0); _mesh.SetTriangles(sideTris, 1);
            Orient(_mesh, 0, v => Vector3.up);
            _mesh.RecalculateNormals(); _mesh.RecalculateBounds(); _mesh.RecalculateTangents();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            var mc = GetComponent<MeshCollider>(); mc.sharedMesh = null; mc.sharedMesh = _mesh;
            _ringStart = ringStart;
        }
        int[] _ringStart;

        /// <summary>Flat water surface (local y = WaterLevel) made of the top-grid triangles that dip below it; terrain hides the rest.</summary>
        public Mesh BuildWaterSurface()
        {
            var m = new Mesh { name = name + "_Water", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var verts = _mesh.vertices; var tris = _mesh.GetTriangles(0);
            var outV = new List<Vector3>(); var outT = new List<int>(); var map = new Dictionary<int, int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                if (Bed(verts[tris[t]]) >= WaterLevel && Bed(verts[tris[t + 1]]) >= WaterLevel && Bed(verts[tris[t + 2]]) >= WaterLevel) continue;
                for (int k = 0; k < 3; k++)
                {
                    int vi = tris[t + k];
                    if (!map.TryGetValue(vi, out int ni)) { ni = outV.Count; map[vi] = ni; outV.Add(new Vector3(verts[vi].x, WaterLevel, verts[vi].z)); }
                    outT.Add(ni);
                }
            }
            m.SetVertices(outV); m.SetTriangles(outT, 0);
            var nrm = new Vector3[outV.Count]; for (int i = 0; i < nrm.Length; i++) nrm[i] = Vector3.up;
            m.SetNormals(nrm); m.RecalculateBounds();
            // triangles wound upward
            var tt = m.triangles; for (int i = 0; i < tt.Length; i += 3) { var n = Vector3.Cross(outV[tt[i + 1]] - outV[tt[i]], outV[tt[i + 2]] - outV[tt[i]]); if (n.y < 0) { (tt[i + 1], tt[i + 2]) = (tt[i + 2], tt[i + 1]); } }
            m.triangles = tt;
            return m;
        }

        /// <summary>Height of a top-grid vertex without the rounded shoulder at the rim (so the shoulder does not count as a hollow).</summary>
        float Bed(Vector3 v)
        {
            float rim = Rim(Mathf.Atan2(v.z, v.x)), u = Mathf.Sqrt(v.x * v.x + v.z * v.z) / Mathf.Max(0.01f, rim);
            return v.y - Shoulder * Smooth((u - 0.86f) / 0.14f);
        }

        static void Orient(Mesh m, int sub, System.Func<Vector3, Vector3> expected)
        {
            var t = m.GetTriangles(sub); var v = m.vertices;
            for (int i = 0; i < t.Length; i += 3)
            {
                var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                var centre = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                if (Vector3.Dot(n, expected(centre)) < 0f) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            }
            m.SetTriangles(t, sub);
        }
    }
}
