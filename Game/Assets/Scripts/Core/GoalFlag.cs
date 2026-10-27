using System.Collections;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// The goal on the last island; E finishes the run (RunSession.Win). Four looks, built by GoalBuilder: a flag that rises, a golden
    /// fruit that is picked, a gate that opens, a campfire that is lit. Only usable while a run is Running.
    /// </summary>
    public class GoalFlag : MonoBehaviour, IInteractable
    {
        public GoalKind kind;
        [Tooltip("Flag: the cloth; Fruit: the fruit. Rises / lifts on use.")]
        public Transform cloth;
        public float raiseHeight = 1.6f;
        public float raiseSeconds = 1.6f;
        [Tooltip("Gate: the two door panels slide apart.")]
        public Transform doorLeft, doorRight;
        [Tooltip("Gate: doors swing open about their parent y axis (degrees, left door +, right door -) instead of sliding apart.")]
        public float doorSwingDegrees;
        [Tooltip("Gate: portal glow; Fire: flames and light. Switched on at use.")]
        public GameObject enableOnUse;
        [Tooltip("Round trip levels: the item (heart_seed) the player must carry here; it is handed over on use.")]
        public string requiresItemId;
        bool _used;
        Inventory.Inventory _inv;

        Inventory.Inventory Inv { get { if (_inv == null) _inv = FindAnyObjectByType<Inventory.Inventory>(); return _inv; } }
        int HeldSlot()
        {
            var inv = Inv; if (inv == null || string.IsNullOrEmpty(requiresItemId)) return -1;
            for (int i = 0; i < Inventory.Inventory.Slots; i++) { var it = inv.Get(i); if (it != null && it.id == requiresItemId) return i; }
            return -1;
        }

        public InteractAction Action => InteractAction.Use;
        bool TooWet => kind == GoalKind.Fire && WeatherState.Raining;   // a campfire will not light in the rain
        public string Prompt => TooWet ? "Too wet to light the campfire: wait for the rain to stop" : !string.IsNullOrEmpty(requiresItemId) && HeldSlot() < 0 ? "You need the Heart from the far island" : kind == GoalKind.Fruit ? "Pick the golden fruit" : kind == GoalKind.Gate ? "Open the gate" : kind == GoalKind.Fire ? "Light the campfire" : "Raise the flag";
        /// <summary>T42: the vague goal hint (no directions, no distances) shown in the HUD and menus. All wording lives here.</summary>
        public string Objective
        {
            get
            {
                if (!string.IsNullOrEmpty(requiresItemId))
                    return HeldSlot() >= 0 ? "Bring the Heart home" : "Find the Heart";
                return kind == GoalKind.Fruit ? "Find the golden fruit" : kind == GoalKind.Gate ? "Open the gate" : kind == GoalKind.Fire ? "Light the campfire" : "Reach the flag";
            }
        }
        /// <summary>True while the player stands on the goal island and the goal can be used (drives the glow cue).</summary>
        public bool PlayerNear { get; private set; }
        const float IslandRadius = 14f;
        Light _cue;
        Player.PlayerController _pc;
        /// <summary>False on round-trip levels until the player carries the Heart.</summary>
        public bool RequirementMet => string.IsNullOrEmpty(requiresItemId) || HeldSlot() >= 0;
        void Update()
        {
            if (_pc == null) _pc = Player.PlayerController.Instance;
            var pc = _pc;
            bool near = !_used && pc != null && RequirementMet && Vector3.Distance(pc.transform.position, transform.position) < IslandRadius;
            PlayerNear = near;
            if (_cue == null)
            {
                if (!near) return;
                var go = new GameObject("GoalCue"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 2f;
                _cue = go.AddComponent<Light>(); _cue.type = LightType.Point; _cue.color = new Color(0.49f, 0.95f, 0.9f); _cue.range = 9f; _cue.intensity = 0f;
            }
            float target = near ? 2.2f + 0.8f * Mathf.Sin(Time.time * 2.4f) : 0f;
            _cue.intensity = Mathf.MoveTowards(_cue.intensity, target, Time.deltaTime * 3f);
            _cue.enabled = _cue.intensity > 0.01f;
        }

        public bool CanInteract(GameObject actor) => !_used && RunSession.Instance != null && RunSession.Instance.State == RunState.Running;

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return;
            if (TooWet) { GameEvents.RaiseHint("It is raining: the campfire will not light until the rain stops"); GameEvents.RaiseUseRejected(); return; }
            if (!string.IsNullOrEmpty(requiresItemId))
            {
                int slot = HeldSlot();
                if (slot < 0) { GameEvents.RaiseUseRejected(); return; }
                Inv.Remove(slot);
            }
            _used = true;
            RunSession.Instance.Win();
            if (enableOnUse != null) enableOnUse.SetActive(true);
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            Vector3 a = cloth != null ? cloth.localPosition : Vector3.zero, b = a + Vector3.up * raiseHeight;
            Vector3 la = doorLeft != null ? doorLeft.localPosition : Vector3.zero, ra = doorRight != null ? doorRight.localPosition : Vector3.zero;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / raiseSeconds)
            {
                float e = t * t * (3f - 2f * t);
                if (cloth != null)
                {
                    cloth.localPosition = Vector3.Lerp(a, b, e);
                    if (kind == GoalKind.Fruit) cloth.localScale = Vector3.one * (0.5f + 0.25f * Mathf.Sin(e * Mathf.PI * 3f) + 0.35f * e);
                }
                if (doorSwingDegrees > 0f)
                {
                    if (doorLeft != null) doorLeft.localRotation = Quaternion.Euler(0f, doorSwingDegrees * e, 0f);
                    if (doorRight != null) doorRight.localRotation = Quaternion.Euler(0f, -doorSwingDegrees * e, 0f);
                }
                else
                {
                    if (doorLeft != null) doorLeft.localPosition = la + Vector3.left * 1.05f * e;
                    if (doorRight != null) doorRight.localPosition = ra + Vector3.right * 1.05f * e;
                }
                yield return null;
            }
            if (cloth != null) cloth.localPosition = b;
        }
    }
}
