using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// T33: procedural shapes of the seeds (pickup, fruit on the seed sources). All meshes are centred on the origin and lie flat
    /// (bamboo along X) so they rest on the ground and spin about Y. Built from ellipsoids and lathes, smooth shaded.
    /// </summary>
    public static class SeedMeshes
    {
        public static Mesh For(string itemId)
        {
            switch (itemId)
            {
                case "vine_seed": return Bamboo();
                case "beanstalk_seed": return Bean();
                case "shelf_seed": return Puffball();
                case "dew_seed": return Succulent();
                case "coconut": return Coconut();
                default: return null;
            }
        }

        /// <summary>Radius of the solid collider that lets the pickup rest on the ground.</summary>
        public static float RestRadius(string itemId)
        {
            switch (itemId) { case "vine_seed": return 0.1f; case "beanstalk_seed": return 0.1f; case "shelf_seed": return 0.22f; case "coconut": return 0.19f; default: return 0.15f; }
        }

        // ---------------------------------------------------------------- shapes
        /// <summary>A bamboo piece: two joints with ring bulges and a short cut stub at each end, 0.8 m long, along X.</summary>
        public static Mesh Bamboo()
        {
            // T67: the culm plus a short sprouting twig with two blades at each joint, and ring scars
            var culm = Culm(0.8f, 0.085f, 2, true);
            var b = new Builder();
            for (int j = 0; j < 2; j++)
            {
                float x = -0.4f + 0.8f * (j + 1) / 3f;
                for (int k = 0; k < 2; k++)
                {
                    float side = k == 0 ? 1f : -1f;
                    var rot = Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(side * (35f + 20f * j), Vector3.right);
                    b.Ellipsoid(new Vector3(x, 0.02f, side * 0.1f), Quaternion.Euler(side * 60f, 0, 20f), new Vector3(0.012f, 0.07f, 0.012f), 6, 4);   // twig
                    b.Ellipsoid(new Vector3(x + 0.05f * side, 0.1f, side * 0.17f), Quaternion.Euler(side * 55f, 0, 35f * side), new Vector3(0.01f, 0.1f, 0.03f), 8, 5);   // blade
                    b.Ellipsoid(new Vector3(x - 0.05f * side, 0.1f, side * 0.16f), Quaternion.Euler(side * 55f, 0, -25f * side), new Vector3(0.009f, 0.08f, 0.026f), 8, 5);
                }
            }
            var extra = b.ToMesh("x");
            var ci = new CombineInstance[] { new CombineInstance { mesh = culm, transform = Matrix4x4.identity }, new CombineInstance { mesh = extra, transform = Matrix4x4.identity } };
            var m = new Mesh { name = "Seed_Bamboo", indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.CombineMeshes(ci, true, true);
            Builder.WeldNormals(m); m.RecalculateBounds();
            return m;
        }

        /// <summary>A vertical bamboo culm (grove): `joints` ring bulges, slightly tapered. Origin at the base when not centred.</summary>
        public static Mesh Culm(float length, float radius, int joints, bool centredAlongX = false)
        {
            var prof = new List<Vector2>();   // (radius, along)
            prof.Add(new Vector2(0.001f, 0f));
            prof.Add(new Vector2(radius * 0.92f, 0f));
            int segs = joints + 1;
            for (int s = 0; s < segs; s++)
            {
                float a = length * s / segs, b = length * (s + 1) / segs;
                float taper = 1f - 0.25f * (a / length);
                prof.Add(new Vector2(radius * taper, a + (b - a) * 0.06f));
                prof.Add(new Vector2(radius * taper * 0.96f, (a + b) * 0.5f));
                if (s < segs - 1)
                {
                    prof.Add(new Vector2(radius * taper * 0.98f, b - (b - a) * 0.07f));
                    prof.Add(new Vector2(radius * taper * 1.12f, b - 0.012f));   // the joint ring
                    prof.Add(new Vector2(radius * taper * 1.12f, b + 0.012f));
                    prof.Add(new Vector2(radius * taper * 0.98f, b + (b - a) * 0.07f));
                }
            }
            float tip = 1f - 0.25f;
            prof.Add(new Vector2(radius * tip * 0.95f, length));
            prof.Add(new Vector2(0.001f, length));
            var m = Lathe(prof, centredAlongX ? 24 : 14);
            m.name = "Seed_Bamboo";
            if (centredAlongX)
            {
                // the lathe axis is Y: lay it onto X and centre it, (x,y,z) -> (y - L/2, x, z)
                var src = m.vertices; var v = new Vector3[src.Length];
                for (int i = 0; i < src.Length; i++) v[i] = new Vector3(src[i].y - length * 0.5f, -src[i].x, src[i].z);
                m.vertices = v; Builder.WeldNormals(m); m.RecalculateBounds();
            }
            return m;
        }

        /// <summary>A kidney bean, 0.46 m long.</summary>
        public static Mesh Bean()
        {
            var b = new Builder();
            b.Ellipsoid(Vector3.zero, Quaternion.identity, new Vector3(0.23f, 0.125f, 0.1f), 32, 20);
            b.Displace(0.012f, 14f, 3f);                                                      // T67: wrinkled skin
            var m = b.ToMesh("Seed_Bean", false);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                float x = v[i].x;
                v[i].y += -x * x * 1.1f + 0.02f;                                                  // curve the back down at both ends
                if (v[i].y > 0.02f && Mathf.Abs(x) < 0.09f) v[i].y -= (0.09f - Mathf.Abs(x)) * 0.35f * (v[i].y / 0.13f);   // the notch on the inner side
                float seam = Mathf.Exp(-v[i].z * v[i].z * 900f) * Mathf.Clamp01((v[i].y + 0.1f) * 5f);                      // raised ridge round the rim (the bean's seam)
                v[i] += new Vector3(0, 0.006f, 0) * seam;
            }
            m.vertices = v; Builder.WeldNormals(m); m.RecalculateBounds();
            // hilum scar and a tiny root tip, built separately and merged
            var h = new Builder();
            h.Ellipsoid(new Vector3(0.0f, -0.045f, 0.0f), Quaternion.identity, new Vector3(0.05f, 0.012f, 0.014f), 10, 6);
            h.Ellipsoid(new Vector3(0.215f, -0.07f, 0f), Quaternion.Euler(0, 0, -25f), new Vector3(0.035f, 0.012f, 0.012f), 8, 5);
            var ci = new CombineInstance[] { new CombineInstance { mesh = m, transform = Matrix4x4.identity }, new CombineInstance { mesh = h.ToMesh("h"), transform = Matrix4x4.identity } };
            var all = new Mesh { name = "Seed_Bean", indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            all.CombineMeshes(ci, true, true); Builder.WeldNormals(all); all.RecalculateBounds();
            return all;
        }

        /// <summary>A puffball mushroom: a round cap on a short, thick stalk (0.5 m tall).</summary>
        public static Mesh Puffball()
        {
            var b = new Builder();
            b.Ellipsoid(new Vector3(0, 0.07f, 0), Quaternion.identity, new Vector3(0.22f, 0.21f, 0.22f), 32, 20);       // cap
            b.Displace(0.008f, 9f, 5f);
            int capEnd = b.Count;
            for (int i = 0; i < 16; i++)                                                                                   // T67: warty scales on the cap
            {
                float a = i * 2.39996f, yy = 0.97f - i * 0.058f, r = Mathf.Sqrt(1f - yy * yy);
                var dir = new Vector3(Mathf.Cos(a) * r, yy, Mathf.Sin(a) * r);
                b.Ellipsoid(new Vector3(0, 0.07f, 0) + Vector3.Scale(dir, new Vector3(0.215f, 0.205f, 0.215f)), Quaternion.FromToRotation(Vector3.up, dir),
                            new Vector3(0.03f, 0.014f, 0.03f), 8, 4);
            }
            b.Ellipsoid(new Vector3(0, -0.085f, 0), Quaternion.identity, new Vector3(0.19f, 0.03f, 0.19f), 28, 6);       // gill skirt under the cap
            b.Ellipsoid(new Vector3(0, -0.15f, 0), Quaternion.identity, new Vector3(0.075f, 0.11f, 0.075f), 16, 8);      // stalk
            b.Ellipsoid(new Vector3(0, -0.2f, 0), Quaternion.identity, new Vector3(0.12f, 0.06f, 0.12f), 18, 8);         // foot
            b.Ellipsoid(new Vector3(0, -0.105f, 0), Quaternion.identity, new Vector3(0.09f, 0.012f, 0.09f), 16, 4);      // ring on the stalk
            return b.ToMesh("Seed_Puffball");
        }

        /// <summary>A coconut: a slightly egg-shaped husk (0.4 m) with a blunt tip and the three "eyes" as small bumps on one side.</summary>
        public static Mesh Coconut()
        {
            var b = new Builder();
            b.Ellipsoid(Vector3.zero, Quaternion.identity, new Vector3(0.18f, 0.2f, 0.18f), 32, 22);                  // husk
            b.Displace(0.01f, 11f, 9f);                                                                                 // T67: fibrous, uneven husk
            b.Ellipsoid(new Vector3(0, -0.17f, 0), Quaternion.identity, new Vector3(0.07f, 0.06f, 0.07f), 14, 8);     // blunt bottom tip
            b.Ellipsoid(new Vector3(0, 0.185f, 0), Quaternion.identity, new Vector3(0.1f, 0.05f, 0.1f), 14, 8);       // shoulder where the eyes sit
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.0944f + 0.4f;
                b.Ellipsoid(new Vector3(Mathf.Cos(a) * 0.045f, 0.205f, Mathf.Sin(a) * 0.045f), Quaternion.identity, new Vector3(0.028f, 0.02f, 0.028f), 10, 6);
                float a2 = a + 1.047f;                                                                                  // three ridges (the husk seams) running down from the shoulder
                for (int k = 1; k <= 5; k++)
                {
                    float yy = 0.17f - k * 0.07f, rr = 0.18f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (yy / 0.2f) * (yy / 0.2f)));
                    b.Ellipsoid(new Vector3(Mathf.Cos(a2) * rr * 0.98f, yy, Mathf.Sin(a2) * rr * 0.98f), Quaternion.identity, new Vector3(0.022f, 0.045f, 0.022f), 8, 5);
                }
            }
            return b.ToMesh("Seed_Coconut");
        }

        /// <summary>A succulent: a rosette of fat, pointed leaves in three rings (0.5 m wide).</summary>
        public static Mesh Succulent()
        {
            var b = new Builder();
            Ring(b, 12, 0.19f, -0.08f, 70f, new Vector3(0.085f, 0.2f, 0.045f), 0f);
            Ring(b, 9, 0.14f, -0.04f, 52f, new Vector3(0.075f, 0.19f, 0.045f), 0.5f);
            Ring(b, 7, 0.09f, 0.0f, 34f, new Vector3(0.07f, 0.17f, 0.045f), 0.2f);
            Ring(b, 5, 0.05f, 0.03f, 18f, new Vector3(0.065f, 0.15f, 0.045f), 0.6f);
            Ring(b, 3, 0.02f, 0.05f, 6f, new Vector3(0.05f, 0.12f, 0.04f), 0.1f);
            b.Ellipsoid(new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(0.05f, 0.1f, 0.05f), 12, 8);
            return b.ToMesh("Seed_Succulent");
        }

        static void Ring(Builder b, int n, float radius, float y, float tiltDeg, Vector3 radii, float phase)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + phase) * Mathf.PI * 2f / n;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                // leaf lies along `dir`, tilted up by tiltDeg; its centre is half a leaf length out from the ring radius
                var rot = Quaternion.AngleAxis(-a * Mathf.Rad2Deg, Vector3.up) * Quaternion.Euler(0, 0, tiltDeg);
                var centre = dir * radius + new Vector3(0, y, 0) + rot * new Vector3(radii.y * 0.9f, 0, 0);
                // long axis of the ellipsoid is local X, so the radii are given (length, thickness, width)
                b.Ellipsoid(centre, rot, new Vector3(radii.y, radii.z, radii.x), 10, 6);
            }
        }

        // ---------------------------------------------------------------- helpers
        static Mesh Lathe(List<Vector2> profile, int around)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int p = 0; p < profile.Count; p++)
                for (int a = 0; a <= around; a++)
                {
                    float t = a * Mathf.PI * 2f / around;
                    verts.Add(new Vector3(Mathf.Cos(t) * profile[p].x, profile[p].y, Mathf.Sin(t) * profile[p].x));
                    uvs.Add(new Vector2(a / (float)around, p / (float)(profile.Count - 1)));
                }
            int row = around + 1;
            for (int p = 0; p < profile.Count - 1; p++)
                for (int a = 0; a < around; a++)
                {
                    int i0 = p * row + a, i1 = i0 + 1, i2 = i0 + row, i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            Builder.WeldNormals(m); m.RecalculateBounds();
            return m;
        }

        sealed class Builder
        {
            readonly List<Vector3> _v = new List<Vector3>(); readonly List<Vector3> _n = new List<Vector3>(); readonly List<Vector2> _uv = new List<Vector2>(); readonly List<int> _t = new List<int>();
            public int Count => _v.Count;

            /// <summary>UV-sphere stretched to `radii` (local axes), rotated and moved.</summary>
            public void Ellipsoid(Vector3 centre, Quaternion rot, Vector3 radii, int around, int rings)
            {
                int start = _v.Count;
                for (int r = 0; r <= rings; r++)
                {
                    float phi = Mathf.PI * r / rings; float y = Mathf.Cos(phi), s = Mathf.Sin(phi);
                    for (int a = 0; a <= around; a++)
                    {
                        float t = a * Mathf.PI * 2f / around;
                        var unit = new Vector3(Mathf.Cos(t) * s, y, Mathf.Sin(t) * s);
                        // the radii are (x, y, z) of the shape; local X = long axis for leaves is handled by the caller
                        _v.Add(centre + rot * Vector3.Scale(unit, radii));
                        _uv.Add(new Vector2(a / (float)around, r / (float)rings));
                        _n.Add(rot * Vector3.Scale(unit, new Vector3(1f / radii.x, 1f / radii.y, 1f / radii.z)).normalized);
                    }
                }
                int row = around + 1;
                for (int r = 0; r < rings; r++)
                    for (int a = 0; a < around; a++)
                    {
                        int i0 = start + r * row + a, i1 = i0 + 1, i2 = i0 + row, i3 = i2 + 1;
                        _t.Add(i0); _t.Add(i2); _t.Add(i1); _t.Add(i1); _t.Add(i2); _t.Add(i3);
                    }
            }

            /// <summary>T67: push vertices along their normals by a 3D noise (wrinkles, pits), amplitude in metres. Optional per-vertex mask.</summary>
            public void Displace(float amp, float freq, float seed, System.Func<Vector3, float> mask = null, int from = 0)
            {
                for (int i = from; i < _v.Count; i++)
                {
                    var p = _v[i] * freq + new Vector3(seed, seed * 1.7f, seed * 0.3f);
                    float n = (Noise3(p) + 0.5f * Noise3(p * 2.1f)) / 1.5f - 0.5f;
                    _v[i] += _n[i] * (n * amp * (mask != null ? mask(_v[i]) : 1f));
                }
            }

            public Mesh ToMesh(string name, bool smooth = true)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(_v); m.SetNormals(_n); m.SetUVs(0, _uv); m.SetTriangles(_t, 0);
                if (smooth) WeldNormals(m);
                m.RecalculateBounds();
                return m;
            }

            static float Noise3(Vector3 p)
            {
                float xy = Mathf.PerlinNoise(p.x, p.y), yz = Mathf.PerlinNoise(p.y, p.z), zx = Mathf.PerlinNoise(p.z, p.x);
                return (xy + yz + zx) / 3f;
            }

            /// <summary>Recalculate normals and average them across vertices at the same position (the UV seam would otherwise show).</summary>
            public static void WeldNormals(Mesh m)
            {
                m.RecalculateNormals();
                var v = m.vertices; var n = m.normals; var sum = new Dictionary<Vector3Int, Vector3>();
                for (int i = 0; i < v.Length; i++)
                {
                    var k = Vector3Int.RoundToInt(v[i] * 2000f);
                    sum[k] = sum.TryGetValue(k, out var s) ? s + n[i] : n[i];
                }
                for (int i = 0; i < v.Length; i++) n[i] = sum[Vector3Int.RoundToInt(v[i] * 2000f)].normalized;
                m.normals = n;
            }
        }
    }
}
