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
                default: return null;
            }
        }

        /// <summary>Radius of the solid collider that lets the pickup rest on the ground.</summary>
        public static float RestRadius(string itemId)
        {
            switch (itemId) { case "vine_seed": return 0.1f; case "beanstalk_seed": return 0.1f; case "shelf_seed": return 0.22f; default: return 0.15f; }
        }

        // ---------------------------------------------------------------- shapes
        /// <summary>A bamboo piece: two joints with ring bulges and a short cut stub at each end, 0.8 m long, along X.</summary>
        public static Mesh Bamboo() => Culm(0.8f, 0.085f, 2, true);

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
            var m = Lathe(prof, 14);
            m.name = "Seed_Bamboo";
            if (centredAlongX)
            {
                // the lathe axis is Y: lay it onto X and centre it, (x,y,z) -> (y - L/2, x, z)
                var src = m.vertices; var v = new Vector3[src.Length];
                for (int i = 0; i < src.Length; i++) v[i] = new Vector3(src[i].y - length * 0.5f, -src[i].x, src[i].z);
                m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            }
            return m;
        }

        /// <summary>A kidney bean, 0.46 m long.</summary>
        public static Mesh Bean()
        {
            var b = new Builder();
            b.Ellipsoid(Vector3.zero, Quaternion.identity, new Vector3(0.23f, 0.125f, 0.1f), 16, 10);
            var m = b.ToMesh("Seed_Bean");
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                float x = v[i].x;
                v[i].y += -x * x * 1.1f + 0.02f;                                                  // curve the back down at both ends
                if (v[i].y > 0.02f && Mathf.Abs(x) < 0.09f) v[i].y -= (0.09f - Mathf.Abs(x)) * 0.35f * (v[i].y / 0.13f);   // the notch on the inner side
            }
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A puffball mushroom: a round cap on a short, thick stalk (0.5 m tall).</summary>
        public static Mesh Puffball()
        {
            var b = new Builder();
            b.Ellipsoid(new Vector3(0, 0.07f, 0), Quaternion.identity, new Vector3(0.22f, 0.21f, 0.22f), 18, 12);       // cap
            b.Ellipsoid(new Vector3(0, -0.15f, 0), Quaternion.identity, new Vector3(0.085f, 0.11f, 0.085f), 10, 6);     // stalk
            b.Ellipsoid(new Vector3(0, -0.2f, 0), Quaternion.identity, new Vector3(0.12f, 0.06f, 0.12f), 10, 6);        // foot
            return b.ToMesh("Seed_Puffball");
        }

        /// <summary>A succulent: a rosette of fat, pointed leaves in three rings (0.5 m wide).</summary>
        public static Mesh Succulent()
        {
            var b = new Builder();
            Ring(b, 9, 0.17f, -0.07f, 62f, new Vector3(0.075f, 0.2f, 0.045f), 0f);
            Ring(b, 6, 0.1f, -0.02f, 38f, new Vector3(0.07f, 0.18f, 0.045f), 0.5f);
            Ring(b, 4, 0.04f, 0.02f, 14f, new Vector3(0.065f, 0.16f, 0.045f), 0.2f);
            b.Ellipsoid(new Vector3(0, 0.1f, 0), Quaternion.identity, new Vector3(0.05f, 0.1f, 0.05f), 8, 6);
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
            var verts = new List<Vector3>(); var tris = new List<int>();
            for (int p = 0; p < profile.Count; p++)
                for (int a = 0; a <= around; a++)
                {
                    float t = a * Mathf.PI * 2f / around;
                    verts.Add(new Vector3(Mathf.Cos(t) * profile[p].x, profile[p].y, Mathf.Sin(t) * profile[p].x));
                }
            int row = around + 1;
            for (int p = 0; p < profile.Count - 1; p++)
                for (int a = 0; a < around; a++)
                {
                    int i0 = p * row + a, i1 = i0 + 1, i2 = i0 + row, i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        sealed class Builder
        {
            readonly List<Vector3> _v = new List<Vector3>(); readonly List<Vector3> _n = new List<Vector3>(); readonly List<int> _t = new List<int>();

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

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(_v); m.SetNormals(_n); m.SetTriangles(_t, 0); m.RecalculateBounds();
                return m;
            }
        }
    }
}
