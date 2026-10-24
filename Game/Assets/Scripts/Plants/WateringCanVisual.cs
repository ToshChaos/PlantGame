using System.Collections;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// A small blue watering can in the player's right hand, shown while the can holds water (charges > 0). A droplet-coloured
    /// disc on top shows it is full; the can tips forward for a moment when a sprout is watered. Built from primitives at
    /// runtime and parented to the right arm joint (it swings with the arm).
    /// </summary>
    public class WateringCanVisual : MonoBehaviour
    {
        WateringCan _can;
        GameObject _root;
        Transform _tilt, _fill;
        Coroutine _pour;

        void Start()
        {
            _can = GetComponent<WateringCan>();
            var pc = GetComponent<Player.PlayerController>();
            Transform model = pc != null ? pc.model : transform;
            Transform arm = null, glove = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Joint_ArmR") arm = t;
                else if (t.name.StartsWith("Glove_R")) glove = t;
            }
            Vector3 pos = model.position + model.right * 0.4f + Vector3.up * 0.9f;
            if (glove != null) { var gr = glove.GetComponentInChildren<Renderer>(); pos = gr != null ? gr.bounds.center : glove.position; }   // the glove's pivot sits at the shoulder, its mesh at the hand
            _root = new GameObject("WateringCanHolder");
            _root.transform.SetPositionAndRotation(pos + model.forward * 0.14f + Vector3.up * 0.04f, model.rotation);
            _root.transform.SetParent(arm != null ? arm : model, true);
            _tilt = new GameObject("Tilt").transform; _tilt.SetParent(_root.transform, false);
            Build(_tilt);
            _root.SetActive(false);
        }

        void Build(Transform p)
        {
            var body = MaterialBuilder("M_CanBody", Palette.SkyBlue, 0.55f, 0.4f);
            var trim = MaterialBuilder("M_CanTrim", Palette.Gold, 0.7f, 0.6f);
            var water = MaterialBuilder("M_CanWater", Palette.GlowCyan, 0.9f, 0f);
            Piece(p, "Body", PrimitiveType.Cylinder, new Vector3(0, 0, 0), new Vector3(0.22f, 0.09f, 0.22f), body, Quaternion.identity);
            Piece(p, "Rim", PrimitiveType.Cylinder, new Vector3(0, 0.09f, 0), new Vector3(0.235f, 0.012f, 0.235f), trim, Quaternion.identity);
            _fill = Piece(p, "Water", PrimitiveType.Cylinder, new Vector3(0, 0.095f, 0), new Vector3(0.19f, 0.01f, 0.19f), water, Quaternion.identity);
            Piece(p, "Spout", PrimitiveType.Cylinder, new Vector3(0, 0.07f, 0.2f), new Vector3(0.05f, 0.13f, 0.05f), body, Quaternion.Euler(60f, 0, 0));
            Piece(p, "Rose", PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0.31f), new Vector3(0.09f, 0.012f, 0.09f), trim, Quaternion.Euler(60f, 0, 0));
            Piece(p, "Handle", PrimitiveType.Cube, new Vector3(0, 0.1f, -0.16f), new Vector3(0.03f, 0.2f, 0.07f), trim, Quaternion.Euler(-20f, 0, 0));
        }

        static Material MaterialBuilder(string name, Color c, float smooth, float metal)
        {
            var m = RuntimeMats.NewLit(name, c);
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (name == "M_CanWater") { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 0.8f); }
            return m;
        }

        static Transform Piece(Transform parent, string n, PrimitiveType t, Vector3 lp, Vector3 scale, Material m, Quaternion rot)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = lp; g.transform.localScale = scale; g.transform.localRotation = rot;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        void OnEnable() { GameEvents.Watered += OnWatered; }
        void OnDisable() { GameEvents.Watered -= OnWatered; }

        void OnWatered() { if (_root != null && _root.activeInHierarchy) { if (_pour != null) StopCoroutine(_pour); _pour = StartCoroutine(Pour()); } }

        IEnumerator Pour()
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float k = Mathf.Sin(t * Mathf.PI);
                _tilt.localRotation = Quaternion.Euler(55f * k, 0, 0);
                yield return null;
            }
            _tilt.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (_root == null || _can == null) return;
            bool show = _can.Charges > 0;
            if (_root.activeSelf != show) _root.SetActive(show);
            if (_fill != null) _fill.gameObject.SetActive(_can.IsFull);
        }
    }
}
