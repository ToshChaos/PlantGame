using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Outline of a planting spot, one per plant kind so you can tell what grows there.</summary>
    public enum SoilShape { Squircle, Circle, Hexagon, Flower }

    /// <summary>
    /// Mesh for a planting spot: a raised bed (squircle for bridges, circle for beanstalks, hexagon for platforms, six-lobed flower for
    /// collectors) whose top is ploughed into furrows running along local Z (the growth direction). Authored in unit-cube space
    /// (x, z in -0.5..0.5, y in -0.5..0.5) so it drops onto the 1.2 x 0.3 x 1.2 BoxCollider cube; UVs are scaled so T_Soil's 6 ridges
    /// per tile line up with the geometric furrows.
    /// </summary>
    public static class SoilPatchMesh
    {
        const int Grid = 28, Ring = 40;
        const float Furrows = 3f;
        static readonly Dictionary<SoilShape, Mesh> Cache = new Dictionary<SoilShape, Mesh>();

        public static SoilShape ShapeFor(Plants.PlantKind kind) =>
            kind == Plants.PlantKind.Ladder ? SoilShape.Circle : kind == Plants.PlantKind.Platform ? SoilShape.Hexagon : kind == Plants.PlantKind.Collector ? SoilShape.Flower : SoilShape.Squircle;

        /// <summary>Distance from the centre to the rim in direction `a` (radians), in unit-cube space (max 0.5).</summary>
        static float Radius(SoilShape shape, float a)
        {
            float c = Mathf.Cos(a), sn = Mathf.Sin(a);
            switch (shape)
            {
                case SoilShape.Circle: return 0.5f;
                case SoilShape.Hexagon:
                {
                    float seg = Mathf.PI / 3f, local = Mathf.Repeat(a, seg) - seg * 0.5f;
                    float hex = 0.5f * Mathf.Cos(seg * 0.5f) / Mathf.Cos(local);
                    return Mathf.Lerp(hex, 0.5f, 0.22f);   // softened corners
                }
                case SoilShape.Flower: return 0.5f * (0.86f + 0.14f * Mathf.Cos(6f * a));
                default: { const float N = 3.2f; return 0.5f / Mathf.Pow(Mathf.Pow(Mathf.Abs(c), N) + Mathf.Pow(Mathf.Abs(sn), N), 1f / N); }
            }
        }

        static Vector2 Outline(SoilShape shape, float gx, float gz)
        {
            // map the square [-1,1]^2 onto the shape: the point keeps its angle and its fraction of the way to the square's boundary
            float inf = Mathf.Max(Mathf.Abs(gx), Mathf.Abs(gz));
            if (inf < 1e-5f) return Vector2.zero;
            float a = Mathf.Atan2(gz, gx);
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Radius(shape, a) * inf);
        }

        public static Mesh Create(string name, SoilShape shape = SoilShape.Squircle)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            const float Top = 0.5f, Amp = 0.2f;   // ridge amplitude in local y (0.3 m tall box -> ~6 cm furrows)
            for (int j = 0; j <= Grid; j++)
                for (int i = 0; i <= Grid; i++)
                {
                    float gx = i * 2f / Grid - 1f, gz = j * 2f / Grid - 1f;
                    var p = Outline(shape, gx, gz);
                    float edge = Mathf.Clamp01((0.5f - Mathf.Max(Mathf.Abs(gx), Mathf.Abs(gz)) * 0.5f) * 6f);   // 0 at the rim, 1 inside
                    float ridge = Mathf.Cos(p.x * Furrows * 2f * Mathf.PI * 1.7f);                                  // along z
                    float y = Top + Amp * (ridge - 1f) * 0.5f * edge - 0.06f * (1f - edge);
                    verts.Add(new Vector3(p.x, y, p.y)); uvs.Add(new Vector2(p.x * 0.85f, p.y * 0.85f));
                }
            for (int j = 0; j < Grid; j++)
                for (int i = 0; i < Grid; i++)
                {
                    int a = j * (Grid + 1) + i, b = a + 1, c = a + Grid + 1, d = c + 1;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }

            // sides: the rim loop (grid boundary) dropping to the base, slightly tucked in
            int topRim = verts.Count;
            var rim = new List<Vector2>();
            for (int k = 0; k < Ring; k++)
            {
                float t = k * 4f / Ring;                         // walk the square boundary
                float side = Mathf.Floor(t), f = t - side;
                float gx, gz;
                switch ((int)side) { case 0: gx = -1f + 2f * f; gz = -1f; break; case 1: gx = 1f; gz = -1f + 2f * f; break; case 2: gx = 1f - 2f * f; gz = 1f; break; default: gx = -1f; gz = 1f - 2f * f; break; }
                rim.Add(Outline(shape, gx, gz));
            }
            for (int k = 0; k <= Ring; k++)
            {
                var p = rim[k % Ring];
                verts.Add(new Vector3(p.x, Top - 0.06f, p.y)); uvs.Add(new Vector2(k * 0.15f, 0.5f));
                verts.Add(new Vector3(p.x * 0.93f, -0.5f, p.y * 0.93f)); uvs.Add(new Vector2(k * 0.15f, 0f));
            }
            for (int k = 0; k < Ring; k++)
            {
                int a = topRim + k * 2, b = a + 1, c = a + 2, d = a + 3;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }

            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            // winding: make sure the top faces up
            if (mesh.normals[Grid / 2 * (Grid + 1) + Grid / 2].y < 0f)
            {
                tris.Reverse(); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            }
            return mesh;
        }

        /// <summary>Replaces the cube mesh of a soil box with the furrowed mesh (shape per plant kind) and the M_Soil material; the BoxCollider stays.</summary>
        public static void Apply(GameObject soil, Plants.PlantKind kind = Plants.PlantKind.Bridge)
        {
            var shape = ShapeFor(kind);
            if (!Cache.TryGetValue(shape, out var mesh) || mesh == null) Cache[shape] = mesh = Create("SoilPatch_" + shape, shape);
            soil.GetComponent<MeshFilter>().sharedMesh = mesh;
            soil.GetComponent<Renderer>().sharedMaterial = KindMat(kind);
        }

        static readonly Dictionary<Plants.PlantKind, Material> KindMats = new Dictionary<Plants.PlantKind, Material>();
        /// <summary>The soil material tinted per plant kind so the patches tell apart at a glance: bridge warm tan, beanstalk dark green loam, mushroom grey, water collector blue.</summary>
        static Material KindMat(Plants.PlantKind kind)
        {
            var src = Kit.Mat("M_Soil"); if (src == null) return null;
            if (KindMats.TryGetValue(kind, out var m) && m != null) return m;
            Color tint = kind == Plants.PlantKind.Ladder ? new Color(0.62f, 1.0f, 0.62f) : kind == Plants.PlantKind.Platform ? new Color(1f, 1f, 1f) : kind == Plants.PlantKind.Collector ? new Color(0.65f, 0.95f, 1.35f) : new Color(1.25f, 0.95f, 0.65f);
            m = new Material(src) { name = "M_Soil_" + kind, hideFlags = HideFlags.DontSave };
            foreach (var prop in new[] { "_BaseColor", "_Color" }) if (m.HasProperty(prop)) { var c = m.GetColor(prop); m.SetColor(prop, kind == Plants.PlantKind.Platform ? new Color(0.5f, 0.5f, 0.53f, c.a) : new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a)); }   // mushroom patch: neutral grey
            return KindMats[kind] = m;
        }
    }
}
