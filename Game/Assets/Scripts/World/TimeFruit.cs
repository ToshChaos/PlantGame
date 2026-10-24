using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// T52: a golden fruit that adds seconds to the Challenge countdown when the player walks into it. Never required to finish a level
    /// (the generator only places it in Challenge mode). Built by WorldGenerator.
    /// </summary>
    public class TimeFruit : MonoBehaviour
    {
        public float seconds = 30f;
        bool _taken;

        public static TimeFruit Build(Transform parent, Vector3 pos, float seconds)
        {
            var root = new GameObject("TimeFruit"); root.transform.SetParent(parent, true); root.transform.position = pos;
            var sc = root.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 1.1f; sc.center = Vector3.up * 1.0f;
            var f = root.AddComponent<TimeFruit>(); f.seconds = seconds;
            var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere); fruit.name = "Fruit";
            Object.DestroyImmediate(fruit.GetComponent<Collider>());
            fruit.transform.SetParent(root.transform, false); fruit.transform.localPosition = Vector3.up * 1.0f; fruit.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            var gold = new Color(1f, 0.78f, 0.25f); var m = RuntimeMats.NewLit("M_TimeFruit", gold);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", gold * 1.6f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.8f);
            fruit.GetComponent<Renderer>().sharedMaterial = m;
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere); leaf.name = "Leaf"; Object.DestroyImmediate(leaf.GetComponent<Collider>());
            leaf.transform.SetParent(fruit.transform, false); leaf.transform.localPosition = new Vector3(0.1f, 0.55f, 0f); leaf.transform.localScale = new Vector3(0.45f, 0.12f, 0.25f);
            leaf.GetComponent<Renderer>().sharedMaterial = Scenery.Mat("M_Foliage");
            var lt = fruit.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.8f, 0.4f); lt.range = 5f; lt.intensity = 1.4f;
            var bob = fruit.AddComponent<FloatBob>(); bob.amplitude = 0.18f; bob.period = 2.4f; bob.spin = 60f;
            return f;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_taken || other.GetComponentInParent<Player.PlayerController>() == null) return;
            var rs = RunSession.Instance;
            if (rs == null || rs.Config.mode != GameMode.Challenge || rs.State != RunState.Running) return;   // inert otherwise
            _taken = true; rs.AddTime(seconds); Destroy(gameObject);
        }
    }
}
