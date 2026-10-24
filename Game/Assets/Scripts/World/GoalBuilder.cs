using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>Builds the four goal looks from primitives: flag, golden fruit on a bush, stone gate with doors, campfire.</summary>
    public static class GoalBuilder
    {
        public static GoalFlag Build(Transform parent, Vector3 pos, GoalKind kind, LevelTheme theme)
        {
            var stone = Kit.Mat("M_Stone"); var gold = Kit.Mat("M_PlayerGold"); var crystal = Kit.Mat("M_Crystal"); var glow = Kit.Mat("M_PortalGlow");
            var rune = Kit.Mat("M_RuneGold"); var bark = Kit.Mat("M_Bark"); var leaf = Scenery.Mat("M_FoliageLight"); var leaf2 = Scenery.Mat("M_Foliage");
            var root = new GameObject("Goal_" + kind); root.transform.SetParent(parent, true); root.transform.position = pos;
            GameObject Part(string n, PrimitiveType t, Vector3 lp, Vector3 sc, Material m, bool collide, Transform under = null)
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; g.transform.SetParent(under != null ? under : root.transform, false);
                g.transform.localPosition = lp; g.transform.localScale = sc; g.GetComponent<Renderer>().sharedMaterial = m;
                g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (!collide) Object.DestroyImmediate(g.GetComponent<Collider>());
                else { int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) g.layer = gl; }
                return g;
            }
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.center = new Vector3(0, 1.2f, 0); trigger.radius = 1.8f;
            int il = LayerMask.NameToLayer(Layers.InteractableName);
            if (il >= 0) root.layer = il;
            var goal = root.AddComponent<GoalFlag>(); goal.kind = kind;

            switch (kind)
            {
                case GoalKind.Fruit:
                {
                    Part("Pedestal", PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(2.4f, 0.3f, 2.4f), stone, true);
                    Part("Bush", PrimitiveType.Sphere, new Vector3(0, 1.0f, 0), new Vector3(2.6f, 1.6f, 2.6f), leaf2, false);
                    Part("Bush2", PrimitiveType.Sphere, new Vector3(0.9f, 0.9f, 0.4f), new Vector3(1.6f, 1.2f, 1.6f), leaf, false);
                    var fruit = Part("GoldenFruit", PrimitiveType.Sphere, new Vector3(0, 2.1f, 0), Vector3.one * 0.7f, rune, false);
                    Part("Stem", PrimitiveType.Cylinder, new Vector3(0, 2.55f, 0), new Vector3(0.06f, 0.18f, 0.06f), bark, false, fruit.transform).transform.localPosition = new Vector3(0, 0.55f, 0);
                    goal.cloth = fruit.transform; goal.raiseHeight = 1.2f; goal.raiseSeconds = 2.2f;
                    var lg = new GameObject("FruitGlow"); lg.transform.SetParent(fruit.transform, false);
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.35f); l.range = 7f; l.intensity = 1.6f;
                    break;
                }
                case GoalKind.Gate:
                {
                    Part("Base", PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(5.4f, 0.3f, 2.2f), stone, true);
                    Part("PillarL", PrimitiveType.Cube, new Vector3(-2.2f, 2.0f, 0), new Vector3(0.9f, 4.0f, 0.9f), stone, true);
                    Part("PillarR", PrimitiveType.Cube, new Vector3(2.2f, 2.0f, 0), new Vector3(0.9f, 4.0f, 0.9f), stone, true);
                    Part("Lintel", PrimitiveType.Cube, new Vector3(0, 4.3f, 0), new Vector3(5.6f, 0.8f, 1.1f), stone, true);
                    Part("Rune", PrimitiveType.Sphere, new Vector3(0, 4.3f, -0.6f), new Vector3(0.5f, 0.5f, 0.2f), rune, false);
                    var portal = Part("Portal", PrimitiveType.Cube, new Vector3(0, 2.0f, 0), new Vector3(3.5f, 3.6f, 0.08f), glow, false);
                    portal.SetActive(false);
                    var dl = Part("DoorLeft", PrimitiveType.Cube, new Vector3(-0.9f, 2.0f, 0), new Vector3(1.8f, 3.6f, 0.35f), stone, false);
                    var dr = Part("DoorRight", PrimitiveType.Cube, new Vector3(0.9f, 2.0f, 0), new Vector3(1.8f, 3.6f, 0.35f), stone, false);
                    Part("SealL", PrimitiveType.Sphere, new Vector3(-0.9f, 2.0f, -0.2f), Vector3.one * 0.35f, rune, false);
                    Part("SealR", PrimitiveType.Sphere, new Vector3(0.9f, 2.0f, -0.2f), Vector3.one * 0.35f, rune, false);
                    goal.doorLeft = dl.transform; goal.doorRight = dr.transform; goal.enableOnUse = portal; goal.raiseSeconds = 2.4f;
                    trigger.radius = 2.6f;
                    break;
                }
                case GoalKind.Fire:
                {
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * Mathf.PI * 2f / 9f;
                        Part("Stone" + i, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.15f, 0.2f, Mathf.Sin(a) * 1.15f), new Vector3(0.6f, 0.4f, 0.6f), stone, false);
                    }
                    Part("Ash", PrimitiveType.Cylinder, new Vector3(0, 0.06f, 0), new Vector3(1.9f, 0.03f, 1.9f), Kit.Mat("M_PlayerBlack"), true);
                    for (int i = 0; i < 4; i++)
                    {
                        var lg = Part("Log" + i, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(0.22f, 0.7f, 0.22f), bark, false);
                        lg.transform.localRotation = Quaternion.Euler(70f, i * 90f + 20f, 0f);
                    }
                    var fire = new GameObject("Flames"); fire.transform.SetParent(root.transform, false); fire.transform.localPosition = new Vector3(0, 0.35f, 0);
                    var l = fire.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.range = 12f; l.intensity = 2.5f;
                    BuildFlames(fire);
                    fire.SetActive(false);
                    goal.enableOnUse = fire;
                    break;
                }
                default:
                {
                    Part("Base", PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(2.2f, 0.25f, 2.2f), stone, true);
                    Part("Pole", PrimitiveType.Cylinder, new Vector3(0, 2.3f, 0), new Vector3(0.14f, 2.0f, 0.14f), gold, false);
                    Part("Orb", PrimitiveType.Sphere, new Vector3(0, 4.45f, 0), Vector3.one * 0.35f, crystal, false);
                    var cloth = Part("Cloth", PrimitiveType.Cube, new Vector3(0.7f, 0.9f, 0), new Vector3(1.3f, 0.8f, 0.05f), crystal, false);   // starts low, rises on use
                    goal.cloth = cloth.transform;
                    break;
                }
            }
            return goal;
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
        }
    }
}
