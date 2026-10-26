using UnityEngine;
using UnityEngine.Serialization;
using PlantGame.Inventory;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>
    /// Sandbox soil patch (T04 dummy, replaced by SoilPatch / PlantInstance wiring in T11): accepts a seed with E, shows a sprout,
    /// grows the baked bridge when watered. The bridge is turned to point along `growDirection` and nudged so its two ends sit
    /// about level with the start floor.
    /// </summary>
    public class SandboxTarget : MonoBehaviour, IUsableTarget, IWaterable
    {
        public bool planted, watered;
        public int useCount;
        public PlantKind kind = PlantKind.Bridge;
        [Tooltip("Only this item id plants here (empty = any seed).")]
        public string acceptedItemId;
        [Tooltip("Baked Plant_<Kind>_NN prefabs; one is picked per plant.")]
        [FormerlySerializedAs("bridgePrefabs")] public GameObject[] plantPrefabs;
        [Tooltip("-1 = cycle through the prefabs, else always this index.")]
        public int variantIndex = -1;
        [Tooltip("Bridge: direction it grows. Ladder: direction the climber's side (and the top pad) faces. Platform: direction of the last pad.")]
        public Vector3 growDirection = Vector3.right;
        [Tooltip("Where the bridge anchor is placed, relative to this object's pivot (floor level + a hair).")]
        public Vector3 spawnOffset = new Vector3(0f, -0.1f, 0f);

        [Tooltip("Bridge: stretch the plant along its length so the tip is this far from the anchor (0 = as baked).")]
        public float bridgeReach;
        [Tooltip("Bridge: height of the tip above the anchor (the bridge slopes up or down; far lower ends are a short drop).")]
        public float bridgeRise;

        [Tooltip("Snow levels: frozen soil takes no seed until a Heat Seed (E) thaws it.")] public bool frozen;
        PlantInstance _plant; Material _thawedMat;

        public void Freeze()
        {
            var r = GetComponent<Renderer>(); if (r == null) return;
            frozen = true; _thawedMat = r.sharedMaterial;
            var ice = new Material(_thawedMat) { hideFlags = HideFlags.DontSave };
            var c = new Color(0.82f, 0.92f, 1f); ice.SetColor("_BaseColor", c); ice.SetColor("_Color", c);
            r.sharedMaterial = ice;
        }

        public bool CanUseItem(ItemDefinition item, GameObject actor)
        {
            if (planted || item == null) return false;
            if (frozen) return item.id == "heat_seed";
            return item.kind == ItemKind.Seed && (string.IsNullOrEmpty(acceptedItemId) || item.id == acceptedItemId);
        }

        public void UseItem(ItemDefinition item, GameObject actor)
        {
            if (frozen)
            {
                frozen = false; var tr = GetComponent<Renderer>(); if (tr != null && _thawedMat != null) tr.sharedMaterial = _thawedMat;
                GameEvents.RaiseHint("The soil thaws: you can plant here now");
                return;
            }
            planted = true; useCount++;
            StartCoroutine(Vanish());
            if (plantPrefabs != null && plantPrefabs.Length > 0)
                _plant = Spawn(plantPrefabs[(variantIndex >= 0 ? variantIndex : useCount - 1) % plantPrefabs.Length]);
            if (kind == PlantKind.Collector) { watered = true; if (_plant != null) _plant.StartGrowing(); DisableCollider(); }
        }

        /// <summary>Level-authored dew patch: the Water Collector stands grown at once and holds one charge it never refills.</summary>
        public void PlantSingleUse()
        {
            if (planted || plantPrefabs == null || plantPrefabs.Length == 0) return;
            planted = true; watered = true; useCount++;
            _plant = Spawn(plantPrefabs[0]);
            DisableCollider();
            if (_plant == null) return;   // the soil patch stays as the plant's base, like the planter on the next island
            _plant.GrowInstantly();
            var wc = _plant.GetComponent<WaterCollector>(); if (wc != null) wc.MakeOneShot();
        }

        public bool NeedsWater => planted && !watered && kind != PlantKind.Collector;   // collectors grow without water

        public void Water(GameObject actor)
        {
            watered = true;
            if (_plant != null) _plant.StartGrowing();
            DisableCollider();
        }

        PlantInstance Spawn(GameObject prefab)
        {
            var go = Instantiate(prefab, transform.position + spawnOffset, Quaternion.identity);
            var anchor = Find(go.transform, "Anchor"); var tip = Find(go.transform, "Tip");
            if (kind == PlantKind.Bridge && bridgeReach > 0f && anchor != null && tip != null)
            {
                var along = tip.position - anchor.position; along.y = 0f;
                // longer / shorter, not wider; a tilted bridge is longer along its slope so its horizontal reach stays bridgeReach
                if (along.magnitude > 0.5f) go.transform.localScale = new Vector3(1f, 1f, Mathf.Sqrt(bridgeReach * bridgeReach + bridgeRise * bridgeRise) / along.magnitude);
            }
            if (kind == PlantKind.Collector)
            {
                go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);   // upright, any yaw: it does not point anywhere
                go.transform.localScale = Vector3.one * 0.75f;   // the Water Collector is a smaller plant than baked
            }
            else if (kind == PlantKind.Ladder)
            {
                Vector3 f = growDirection; f.y = 0f;
                go.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);   // prefab forward (+Z) = climber side
            }
            else if (kind == PlantKind.Platform && anchor != null && tip != null)
            {
                // yaw so the last pad (Tip) lies along growDirection; no tilt
                Vector3 d = tip.position - anchor.position; d.y = 0f;
                go.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(d, growDirection.normalized, Vector3.up), Vector3.up);
            }
            else if (anchor != null && tip != null)
            {
                Vector3 d = tip.position - anchor.position; d.y = 0f;
                go.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(d, growDirection.normalized, Vector3.up), Vector3.up);
                // tilt a few degrees around the anchor so the tip ends level with the start, then put the anchor back
                Vector3 full = tip.position - anchor.position, level = full; level.y = bridgeRise;   // 0 = tip level with the start
                Vector3 pivot = anchor.position;
                go.transform.rotation = Quaternion.FromToRotation(full, level) * go.transform.rotation;
                go.transform.position += pivot - anchor.position;
            }
            // the plant (sprout and grown) rides with the island it is planted on: islands bob up and down (FloatBob)
            var bob = GetComponentInParent<PlantGame.World.FloatBob>();
            var host = bob != null ? bob.transform : transform.parent;
            if (host != null) go.transform.SetParent(host, true);
            var p = go.GetComponent<PlantInstance>();
            if (p != null) { p.kind = kind; p.ShowSprout(); }
            return p;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>The grow zone sinks into the ground and is gone once a seed is planted; its collider stays until the sprout is watered so F / E still find it.</summary>
        System.Collections.IEnumerator Vanish()
        {
            Vector3 a = transform.position, b = a + Vector3.down * 0.3f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                transform.position = Vector3.Lerp(a, b, t * t);
                yield return null;
            }
            var r = GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        void DisableCollider()
        {
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
        }
    }
}
