using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// Builds the four goal looks from procedural meshes (ShapeMeshes): a marble flag shrine with a waving pennant, a golden fruit in a leafy
    /// bush on a rune pedestal, an overgrown marble gate with sliding doors and a portal, a stone-ringed campfire with log seats.
    /// White marble, rune gold, glow cyan and green foliage, like the arch ruins and the plants.
    /// </summary>
    public static class GoalBuilder
    {
        static Material _portalMat, _emberMat;

        public static GoalFlag Build(Transform parent, Vector3 pos, GoalKind kind, LevelTheme theme)
        {
            var stone = Kit.Mat("M_Stone"); var marble = Kit.Mat("M_Marble") ?? stone; var gold = Kit.Mat("M_RuneGold"); var crystal = Kit.Mat("M_Crystal");
            var bark = Kit.Mat("M_Bark"); var leaf = Scenery.Mat("M_FoliageLight"); var leaf2 = Scenery.Mat("M_Foliage");
            var root = new GameObject("Goal_" + kind); root.transform.SetParent(parent, true); root.transform.position = pos;
            var rnd = new System.Random((int)(pos.x * 13f + pos.z * 7f) + (int)kind * 101);

            GameObject Obj(string n, Mesh m, Material mat, Vector3 lp, Vector3 euler, Vector3 sc, Transform under = null)
            {
                var g = ShapeMeshes.Obj(n, m, mat, under != null ? under : root.transform, lp, euler, sc);
                g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                return g;
            }
            void Col(PrimitiveType t, string n, Vector3 lp, Vector3 sc)   // invisible collider on the Ground layer
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; g.transform.SetParent(root.transform, false);
                g.transform.localPosition = lp; g.transform.localScale = sc; Object.DestroyImmediate(g.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(g.GetComponent<MeshFilter>());
                int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) g.layer = gl;
            }
            // a leaf blade starting at `from`, pointing along `dir`
            void Blade(Transform under, Vector3 from, Vector3 dir, float len, Material mat)
            {
                dir = dir.normalized;
                var g = ShapeMeshes.Obj("Blade", LeafMesh.Build(len, len * 0.3f, len * 0.1f, 0.03f, 8), mat, under, from + dir * len * 0.5f, Vector3.zero, Vector3.one);
                g.transform.localRotation = Quaternion.FromToRotation(Vector3.right, dir);
            }
            // a ring of grass blades and a few blossoms round `centre`
            void Tuft(Vector3 centre, int count, float radius, float len)
            {
                for (int i = 0; i < count; i++)
                {
                    float a = (float)rnd.NextDouble() * 6.283f, d = radius * (0.6f + 0.4f * (float)rnd.NextDouble());
                    var p = centre + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                    Blade(root.transform, p, new Vector3(Mathf.Cos(a) * 0.35f, 1f, Mathf.Sin(a) * 0.35f), len * (0.7f + 0.6f * (float)rnd.NextDouble()), i % 2 == 0 ? leaf : leaf2);
                }
            }
            Mesh petalMesh = null, eyeMesh = null;   // shared by every blossom (built on first use)
            void Blossom(Vector3 p, Material petals)
            {
                petalMesh ??= ShapeMeshes.Ellipsoid(0.07f, 0.02f, 8, 5); eyeMesh ??= ShapeMeshes.Ellipsoid(0.04f, 0.035f, 8, 6);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 1.047f;
                    Obj("Petal", petalMesh, petals, p + new Vector3(Mathf.Cos(a) * 0.09f, 0f, Mathf.Sin(a) * 0.09f), new Vector3(0, -a * Mathf.Rad2Deg, 0), new Vector3(1.4f, 1f, 0.7f));
                }
                Obj("BlossomEye", eyeMesh, gold, p + Vector3.up * 0.01f, Vector3.zero, Vector3.one);
            }
            var steppedBase = new List<Vector2>();
            void Tier(float r, float y) => steppedBase.Add(new Vector2(r, y));

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.center = new Vector3(0, 1.2f, 0); trigger.radius = 1.8f;
            int il = LayerMask.NameToLayer(Layers.InteractableName);
            if (il >= 0) root.layer = il;
            var goal = root.AddComponent<GoalFlag>(); goal.kind = kind;
            var white = Kit.Mat("M_FlowerWhite") ?? marble; var yellow = Kit.Mat("M_FlowerYellow") ?? gold;

            switch (kind)
            {
                case GoalKind.Fruit:
                {
                    // rune pedestal: stepped marble disc, a gold rim and rune studs
                    foreach (var pt in new[] { (0.001f, 0f), (2.4f, 0f), (2.4f, 0.14f), (2.2f, 0.18f), (2.2f, 0.34f), (1.95f, 0.38f), (1.95f, 0.55f), (1.7f, 0.6f), (0.001f, 0.6f) }) Tier(pt.Item1, pt.Item2);
                    Obj("Pedestal", ShapeMeshes.Lathe(steppedBase, 40, true, null, "Pedestal"), marble, Vector3.zero, Vector3.zero, Vector3.one);
                    Col(PrimitiveType.Cylinder, "PedestalCol", new Vector3(0, 0.3f, 0), new Vector3(4.8f, 0.3f, 4.8f));
                    Obj("PedestalRim", ShapeMeshes.Torus(2.22f, 0.05f, 48, 8), gold, new Vector3(0, 0.36f, 0), Vector3.zero, Vector3.one);
                    var studMesh = ShapeMeshes.Ellipsoid(0.11f, 0.06f, 10, 6);
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI * 2f / 12f;
                        Obj("RuneStud", studMesh, gold, new Vector3(Mathf.Cos(a) * 2.08f, 0.4f, Mathf.Sin(a) * 2.08f), Vector3.zero, Vector3.one);
                    }
                    // the bush: heaped foliage blobs, leaf blades sticking out, blossoms and berries
                    Obj("Bush", ShapeMeshes.Ellipsoid(1.35f, 0.85f, 18, 12), leaf2, new Vector3(0, 1.0f, 0), Vector3.zero, Vector3.one);
                    var lobeMesh = ShapeMeshes.Ellipsoid(0.85f, 0.6f, 14, 9);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 1.047f + 0.3f;
                        Obj("BushLobe" + i, lobeMesh, i % 2 == 0 ? leaf : leaf2, new Vector3(Mathf.Cos(a) * 1.15f, 0.85f + 0.1f * (i % 3), Mathf.Sin(a) * 1.15f), Vector3.zero, Vector3.one);
                    }
                    var topMesh = ShapeMeshes.Ellipsoid(0.7f, 0.5f, 14, 9);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.57f + 0.8f;
                        Obj("BushTop" + i, topMesh, i % 2 == 0 ? leaf2 : leaf, new Vector3(Mathf.Cos(a) * 0.65f, 1.4f, Mathf.Sin(a) * 0.65f), Vector3.zero, Vector3.one);
                    }
                    for (int i = 0; i < 26; i++)
                    {
                        float a = (float)rnd.NextDouble() * 6.283f, h = 0.7f + 0.9f * (float)rnd.NextDouble(), rr = 1.0f + 0.5f * (1.4f - h) / 1.4f;
                        Blade(root.transform, new Vector3(Mathf.Cos(a) * rr * 0.8f, h, Mathf.Sin(a) * rr * 0.8f), new Vector3(Mathf.Cos(a), 0.45f - 0.6f * (h - 0.7f), Mathf.Sin(a)), 0.55f + 0.3f * (float)rnd.NextDouble(), i % 2 == 0 ? leaf : leaf2);
                    }
                    var berryMesh = ShapeMeshes.Ellipsoid(0.085f, 0.085f, 10, 7);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 0.9f + 0.2f, rr = 1.55f - 0.1f * (i % 3);
                        if (i % 2 == 0) Blossom(new Vector3(Mathf.Cos(a) * rr, 0.95f + 0.1f * (i % 3), Mathf.Sin(a) * rr), i % 4 == 0 ? yellow : white);
                        else Obj("Berry", berryMesh, gold, new Vector3(Mathf.Cos(a) * rr, 0.95f, Mathf.Sin(a) * rr), Vector3.zero, Vector3.one);
                    }
                    Tuft(Vector3.zero, 16, 2.1f, 0.5f);
                    // the golden fruit with stem, calyx leaves and a ring of motes
                    var fruit = Obj("GoldenFruit", FruitMesh(), Kit.Mat("M_GoldFruit") ?? gold, new Vector3(0, 2.1f, 0), Vector3.zero, Vector3.one);
                    var stem = new List<Vector3>(); for (int i = 0; i <= 6; i++) { float s = i / 6f; stem.Add(new Vector3(0.1f * s * s, 0.36f + 0.3f * s, 0f)); }
                    Obj("Stem", ShapeMeshes.Tube(stem, s => 0.035f - 0.015f * s, 6), bark, Vector3.zero, Vector3.zero, Vector3.one, fruit.transform);
                    Blade(fruit.transform, new Vector3(0.06f, 0.5f, 0f), new Vector3(1f, 0.3f, 0.2f), 0.4f, leaf);
                    for (int i = 0; i < 5; i++) { float a = i * 1.257f; Blade(fruit.transform, new Vector3(Mathf.Cos(a) * 0.05f, 0.37f, Mathf.Sin(a) * 0.05f), new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)), 0.2f, leaf2); }
                    var moteMesh = ShapeMeshes.Ellipsoid(0.04f, 0.04f, 8, 5);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 0.785f;
                        Obj("Mote", moteMesh, crystal, new Vector3(Mathf.Cos(a) * 0.85f, 2.1f + 0.18f * Mathf.Sin(a * 3f), Mathf.Sin(a) * 0.85f), Vector3.zero, Vector3.one);
                    }
                    goal.cloth = fruit.transform; goal.raiseHeight = 1.2f; goal.raiseSeconds = 2.2f;
                    var lg = new GameObject("FruitGlow"); lg.transform.SetParent(fruit.transform, false);
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.35f); l.range = 7f; l.intensity = 1.6f;
                    break;
                }
                case GoalKind.Gate:
                {
                    // the temple arch of the islands (marble pillars, stone ring, steps, gold ring), closed by two round doors that
                    // slide apart on use and reveal a blue translucent portal
                    Scenery.Arch(root.transform, pos, Quaternion.identity, true);
                    const float hw = 1.45f;   // half width of the straight part of the clear opening (see ShapeMeshes.OpeningHalf)
                    var doors = new GameObject[2];
                    var doorStud = ShapeMeshes.Ellipsoid(0.07f, 0.04f, 10, 6); var sealRing = ShapeMeshes.Torus(0.28f, 0.04f, 32, 8); var sealGem = ShapeMeshes.Ellipsoid(0.17f, 0.17f, 14, 9);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        bool left = s < 0;
                        var door = new GameObject(left ? "DoorLeft" : "DoorRight"); door.transform.SetParent(root.transform, false); door.transform.localPosition = new Vector3(hw * s, -Scenery.ArchSink, 0.12f);   // hinge at the outer edge, in front of the portal
                        var hinge = new GameObject("Leaf"); hinge.transform.SetParent(door.transform, false); hinge.transform.localPosition = new Vector3(-hw * s, 0, 0);
                        Obj("Slab", ShapeMeshes.OpeningSlab(left, 0.16f), stone, Vector3.zero, Vector3.zero, Vector3.one, hinge.transform);
                        Obj("Rim", ShapeMeshes.Tube(ShapeMeshes.OpeningTrim(-0.06f, 0.09f, true, left), q => 0.03f, 6), gold, Vector3.zero, Vector3.zero, Vector3.one, hinge.transform);
                        Obj("Seam", ShapeMeshes.Tube(new List<Vector3> { new Vector3(0, 0.1f, 0.09f), new Vector3(0, 2.3f, 0.09f), new Vector3(0, 4.1f, 0.09f) }, q => 0.04f, 6), gold, Vector3.zero, Vector3.zero, Vector3.one, hinge.transform);
                        for (int r = 0; r < 3; r++)   // gold bands across the leaf
                            Obj("Band", ShapeMeshes.Tube(new List<Vector3> { new Vector3(0, 0.9f + r * 1.0f, 0.09f), new Vector3(0.7f * s, 0.9f + r * 1.0f, 0.09f), new Vector3(1.38f * s, 0.9f + r * 1.0f, 0.09f) }, q => 0.025f, 6), gold, Vector3.zero, Vector3.zero, Vector3.one, hinge.transform);
                        for (int r = 0; r < 4; r++)
                            Obj("Stud", doorStud, gold, new Vector3(0.7f * s, 0.9f + (r < 3 ? r * 1.0f : 3.0f) + (r == 3 ? 0.0f : 0f), 0.1f), new Vector3(90, 0, 0), Vector3.one, hinge.transform);
                        Obj("SealRing", sealRing, gold, new Vector3(0.5f * s, 2.0f, 0.11f), new Vector3(90, 0, 0), Vector3.one, hinge.transform);
                        Obj("SealGem", sealGem, crystal, new Vector3(0.5f * s, 2.0f, 0.12f), new Vector3(90, 0, 0), new Vector3(1f, 0.5f, 1f), hinge.transform);
                        doors[left ? 0 : 1] = door;
                    }
                    var portal = Obj("Portal", ShapeMeshes.OpeningPanel(), PortalMat(), new Vector3(0, -Scenery.ArchSink, 0.0f), Vector3.zero, Vector3.one);
                    portal.SetActive(false);
                    for (int s = -1; s <= 1; s += 2)   // glowing crystals and moss at the foot of the pillars
                    {
                        for (int i = 0; i < 3; i++)
                            Obj("Crystal", DebrisMeshes.Crystal(i + (s > 0 ? 3 : 0)), crystal, new Vector3(s * (3.3f + 0.3f * i), 0f, 0.4f + 0.5f * i), new Vector3(0, i * 70f, (i - 1) * 8f), Vector3.one * (0.55f - 0.1f * i));
                        Tuft(new Vector3(s * 2.8f, 0.05f, 0.9f), 10, 0.9f, 0.45f);
                    }
                    goal.doorLeft = doors[0].transform; goal.doorRight = doors[1].transform; goal.doorSwingDegrees = 95f; goal.enableOnUse = portal; goal.raiseSeconds = 2.4f;
                    trigger.radius = 2.6f;
                    break;
                }
                case GoalKind.Fire:
                {
                    // ring of stones, a second loose ring, moss and grass
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i * Mathf.PI * 2f / 14f + 0.1f * (float)rnd.NextDouble(), s = 0.34f + 0.12f * (float)rnd.NextDouble();
                        Obj("Stone" + i, DebrisMeshes.Rock(rnd.Next(8)), stone, new Vector3(Mathf.Cos(a) * 1.2f, 0.12f, Mathf.Sin(a) * 1.2f), new Vector3(0, (float)rnd.NextDouble() * 360f, 0), new Vector3(s * 1.1f, s * 0.8f, s));
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 0.9f + 0.4f, d = 1.75f + 0.25f * (float)rnd.NextDouble(), s = 0.2f + 0.1f * (float)rnd.NextDouble();
                        Obj("OuterStone" + i, DebrisMeshes.Rock(rnd.Next(8)), stone, new Vector3(Mathf.Cos(a) * d, 0.06f, Mathf.Sin(a) * d), new Vector3(0, (float)rnd.NextDouble() * 360f, 0), Vector3.one * s);
                    }
                    Tuft(Vector3.zero, 14, 2.4f, 0.4f);
                    var ash = new List<Vector2> { new Vector2(0.001f, 0.02f), new Vector2(1.0f, 0.03f), new Vector2(1.05f, 0.08f), new Vector2(0.8f, 0.1f), new Vector2(0.5f, 0.07f), new Vector2(0.001f, 0.06f) };
                    Obj("Ash", ShapeMeshes.Lathe(ash, 28, false, (ang, p) => 1f + 0.06f * Mathf.Sin(ang * 5f), "Ash"), Kit.Mat("M_PlayerDark"), Vector3.zero, Vector3.zero, Vector3.one);
                    Col(PrimitiveType.Cylinder, "AshCol", new Vector3(0, 0.03f, 0), new Vector3(2.0f, 0.03f, 2.0f));
                    GameObject LogAt(string n, Vector3 from, Vector3 to, float r, float len)
                    {
                        var prof = new List<Vector2> { new Vector2(0.001f, -len / 2), new Vector2(r * 0.8f, -len / 2), new Vector2(r, -len / 2 + 0.04f), new Vector2(r * 1.05f, -len * 0.25f), new Vector2(r, len * 0.25f), new Vector2(r * 0.95f, len / 2 - 0.04f), new Vector2(r * 0.78f, len / 2), new Vector2(0.001f, len / 2) };
                        var g = Obj(n, ShapeMeshes.Lathe(prof, 14, false, (ang, p) => 1f + 0.07f * Mathf.Cos(ang * 7f), "Log"), bark, (from + to) * 0.5f, Vector3.zero, Vector3.one);
                        g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
                        return g;
                    }
                    for (int i = 0; i < 6; i++)   // a teepee of logs
                    {
                        float a = i * 1.047f + 0.2f;
                        LogAt("Log" + i, new Vector3(Mathf.Cos(a) * 0.8f, 0.12f, Mathf.Sin(a) * 0.8f), new Vector3(Mathf.Cos(a) * 0.12f, 1.25f, Mathf.Sin(a) * 0.12f), 0.1f, 1.45f);
                    }
                    for (int i = 0; i < 3; i++)   // laid logs
                    {
                        float a = i * 2.1f + 0.7f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        LogAt("LaidLog" + i, dir * -0.7f + Vector3.up * 0.14f, dir * 0.7f + Vector3.up * 0.14f, 0.12f, 1.4f);
                    }
                    for (int i = 0; i < 3; i++)   // log benches round the fire
                    {
                        float a = i * 2.1f + 1.4f, d = 2.7f; var c = new Vector3(Mathf.Cos(a) * d, 0.26f, Mathf.Sin(a) * d); var tan = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                        LogAt("Bench" + i, c - tan * 0.9f, c + tan * 0.9f, 0.26f, 1.8f);
                        Blade(root.transform, c + Vector3.up * 0.2f + tan * 0.3f, tan * 0.2f + Vector3.up, 0.25f, leaf);
                    }
                    var fire = new GameObject("Flames"); fire.transform.SetParent(root.transform, false); fire.transform.localPosition = new Vector3(0, 0.35f, 0);
                    var l = fire.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.range = 12f; l.intensity = 2.5f;
                    for (int i = 0; i < 9; i++)   // glowing embers once lit
                    {
                        float a = i * 0.7f, d = 0.15f + 0.65f * (float)rnd.NextDouble();
                        Obj("Ember" + i, DebrisMeshes.Rock(rnd.Next(8)), EmberMat(), new Vector3(Mathf.Cos(a) * d, -0.2f, Mathf.Sin(a) * d), new Vector3(0, i * 40f, 0), Vector3.one * (0.12f + 0.06f * (float)rnd.NextDouble()), fire.transform);
                    }
                    BuildFlames(fire);
                    fire.SetActive(false);
                    goal.enableOnUse = fire;
                    break;
                }
                default:
                {
                    // tall shrine flag: stepped marble base, brown wooden pole with gold fittings, crystal in a gold cage, big light purple pennant
                    foreach (var pt in new[] { (0.001f, 0f), (2.9f, 0f), (2.9f, 0.18f), (2.6f, 0.23f), (2.6f, 0.4f), (2.15f, 0.45f), (2.15f, 0.6f), (1.75f, 0.7f), (0.6f, 0.85f), (0.001f, 0.88f) }) Tier(pt.Item1, pt.Item2);
                    Obj("Base", ShapeMeshes.Lathe(steppedBase, 44, true, null, "FlagBase"), marble, Vector3.zero, Vector3.zero, Vector3.one);
                    Col(PrimitiveType.Cylinder, "BaseCol", new Vector3(0, 0.4f, 0), new Vector3(5.8f, 0.4f, 5.8f));
                    Obj("BaseRim", ShapeMeshes.Torus(2.68f, 0.05f, 56, 8), gold, new Vector3(0, 0.38f, 0), Vector3.zero, Vector3.one);
                    var flagStud = ShapeMeshes.Ellipsoid(0.12f, 0.065f, 10, 6);
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI * 2f / 12f;
                        Obj("RuneStud", flagStud, gold, new Vector3(Mathf.Cos(a) * 2.4f, 0.5f, Mathf.Sin(a) * 2.4f), Vector3.zero, Vector3.one);
                    }
                    const float top = 8.4f;
                    var pole = new List<Vector2> { new Vector2(0.001f, 0.85f), new Vector2(0.3f, 0.85f), new Vector2(0.26f, 1.1f), new Vector2(0.2f, 1.25f), new Vector2(0.19f, 3f), new Vector2(0.15f, top), new Vector2(0.001f, top) };
                    Obj("Pole", ShapeMeshes.Lathe(pole, 18, false, (ang, p) => 1f + 0.04f * Mathf.Cos(ang * 5f), "Pole"), bark, Vector3.zero, Vector3.zero, Vector3.one);
                    Obj("Collar", ShapeMeshes.Lathe(new List<Vector2> { new Vector2(0.16f, 0.9f), new Vector2(0.3f, 0.92f), new Vector2(0.24f, 1.25f), new Vector2(0.16f, 1.3f) }, 18, true), gold, Vector3.zero, Vector3.zero, Vector3.one);
                    var bead = ShapeMeshes.Torus(0.14f, 0.04f, 20, 6);
                    foreach (float by in new[] { 2.2f, 4.4f, 6.4f, 8.1f }) Obj("Bead", bead, gold, new Vector3(0, by, 0), Vector3.zero, Vector3.one);
                    Obj("OrbCrystal", ShapeMeshes.Lathe(ShapeMeshes.Blob(0.34f, 0.42f, 14), 20, false, null, "Orb"), crystal, new Vector3(0, top + 0.4f, 0), Vector3.zero, Vector3.one);
                    var cage = ShapeMeshes.Torus(0.42f, 0.026f, 24, 6);
                    for (int i = 0; i < 3; i++) Obj("OrbCage" + i, cage, gold, new Vector3(0, top + 0.4f, 0), new Vector3(90, i * 60f, 90), Vector3.one);
                    Obj("OrbCap", ShapeMeshes.Lathe(new List<Vector2> { new Vector2(0.001f, 0f), new Vector2(0.11f, 0.03f), new Vector2(0.04f, 0.25f), new Vector2(0.001f, 0.38f) }, 12), gold, new Vector3(0, top + 0.82f, 0), Vector3.zero, Vector3.one);
                    for (int i = 0; i < 12; i++)   // ivy spiralling up the pole
                    {
                        float a = i * 1.1f, h = 1.5f + i * 0.3f; var o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Blade(root.transform, o * 0.13f + Vector3.up * h, o + Vector3.down * 0.3f, 0.36f + 0.12f * (i % 3), i % 2 == 0 ? leaf : leaf2);
                    }
                    // the pennant starts low and rises to the top on use
                    var purple = FlagMat();
                    var cloth = new GameObject("Cloth"); cloth.transform.SetParent(root.transform, false); cloth.transform.localPosition = new Vector3(0.12f, 1.7f, 0);
                    Obj("Pennant", ShapeMeshes.Banner(3.4f, 2.0f, 18, 10, 0.2f), purple, Vector3.zero, Vector3.zero, Vector3.one, cloth.transform);
                    Obj("Hem", ShapeMeshes.Tube(new List<Vector3> { new Vector3(0.02f, 2.0f, 0), new Vector3(1.7f, 2.0f, 0.1f), new Vector3(3.4f, 2.0f, 0.2f) }, q => 0.03f, 6), gold, Vector3.zero, Vector3.zero, Vector3.one, cloth.transform);
                    Obj("Hem2", ShapeMeshes.Tube(new List<Vector3> { new Vector3(0.02f, 0f, 0), new Vector3(1.7f, 0f, 0.1f), new Vector3(3.4f, 0f, 0.2f) }, q => 0.03f, 6), gold, Vector3.zero, Vector3.zero, Vector3.one, cloth.transform);
                    for (int side = -1; side <= 1; side += 2)
                        ShapeMeshes.Obj("Emblem", LeafMesh.Build(1.2f, 0.5f, 0.02f, 0.01f, 10), white, cloth.transform, new Vector3(1.3f, 1.0f, 0.06f * side), new Vector3(0, 0, 22f), Vector3.one);
                    var tassel = ShapeMeshes.Ellipsoid(0.07f, 0.14f, 8, 6);
                    for (int i = 0; i < 4; i++) Obj("Tassel", tassel, gold, new Vector3(1.8f + 0.45f * i, -0.1f + 0.03f * i, 0.1f), Vector3.zero, Vector3.one, cloth.transform);
                    goal.cloth = cloth.transform; goal.raiseHeight = 4.6f; goal.raiseSeconds = 2.6f;
                    Tuft(Vector3.zero, 22, 2.8f, 0.55f);
                    for (int i = 0; i < 6; i++) { float a = i * 1.1f + 0.5f; Blossom(new Vector3(Mathf.Cos(a) * 3.05f, 0.08f, Mathf.Sin(a) * 3.05f), i % 2 == 0 ? white : yellow); }
                    trigger.center = new Vector3(0, 1.5f, 0); trigger.radius = 2.4f;
                    break;
                }
            }
            return goal;
        }

        /// <summary>The golden fruit: a plump apple shape, shallow dimples at the stem and the base, five soft lobes.</summary>
        static Mesh FruitMesh()
        {
            var prof = new List<Vector2>();
            const int rings = 22;
            for (int i = 0; i <= rings; i++)
            {
                float phi = Mathf.PI * i / rings, s = Mathf.Sin(phi), c = Mathf.Cos(phi);
                float r = 0.43f * s * (1f + 0.07f * c), y = -0.39f * c;
                if (i == 0) { prof.Add(new Vector2(0.0005f, -0.33f)); continue; }                                  // base dimple
                if (i == rings) { prof.Add(new Vector2(0.0005f, 0.3f)); continue; }                               // stem dimple
                if (i == 1) y += 0.03f; if (i == rings - 1) y -= 0.05f;
                prof.Add(new Vector2(r, y));
            }
            return ShapeMeshes.Lathe(prof, 40, false, (ang, p) => 1f + 0.035f * Mathf.Cos(ang * 5f) * Mathf.Sin(Mathf.PI * p / (float)rings), "GoldenFruit");
        }

        static Material PortalMat()
        {
            if (_portalMat != null) return _portalMat;
            return _portalMat = RuntimeMats.NewParticles("M_GatePortal", new Color(0.02f, 0.38f, 0.95f, 0.5f).linear);   // unlit, so it stays a clear blue at 50 % alpha under any light
        }

        static Material _flagMat;
        static Material FlagMat()
        {
            if (_flagMat != null) return _flagMat;
            var m = RuntimeMats.NewLit("M_FlagPurple", new Color(0.64f, 0.52f, 0.96f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.64f, 0.52f, 0.96f) * 0.1f);
            return _flagMat = m;
        }

        static Material EmberMat()
        {
            if (_emberMat != null) return _emberMat;
            var m = RuntimeMats.NewLit("M_Ember", new Color(0.9f, 0.3f, 0.08f));
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f) * 1.6f);
            return _emberMat = m;
        }

        static void BuildFlames(GameObject fire)
        {
            var ps = fire.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); main.maxParticles = 150;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f, 0.9f), new Color(1f, 0.85f, 0.3f, 0.9f));
            main.gravityModifier = -0.15f;
            var em = ps.emission; em.rateOverTime = 45f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.4f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
            var r = fire.GetComponent<ParticleSystemRenderer>();
            var m = RuntimeMats.NewParticles("M_Flame", new Color(3f, 1.6f, 0.5f, 1f), additive: true);
            m.SetFloat("_Blend", 1f); m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            r.sharedMaterial = m;
            ps.Play();

            // sparks drifting up from the flames
            var sg = new GameObject("Sparks"); sg.transform.SetParent(fire.transform, false); sg.transform.localPosition = new Vector3(0, 0.3f, 0);
            var sp = sg.AddComponent<ParticleSystem>(); sp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sm = sp.main; sm.loop = true; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f); sm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f); sm.maxParticles = 60; sm.gravityModifier = -0.05f;
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.35f, 1f), new Color(1f, 0.5f, 0.15f, 1f));
            var se = sp.emission; se.rateOverTime = 14f;
            var ss = sp.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 28f; ss.radius = 0.35f;
            var sc = sp.colorOverLifetime; sc.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            sc.color = grad;
            var sr = sg.GetComponent<ParticleSystemRenderer>();
            var sMat = RuntimeMats.NewParticles("M_Spark", new Color(3f, 2f, 0.8f, 1f), additive: true);
            sr.sharedMaterial = sMat;
            sp.Play();
        }
    }
}
