using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Plants;
using PlantGame.Player;

namespace PlantGame.Audio
{
    /// <summary>
    /// T18: listens to GameEvents (and PlantInstance hooks) and plays the matching SFX through AudioManager.
    /// Footsteps are driven by distance walked (surface from the island material under the feet), climbing by height gained.
    /// </summary>
    public class SfxBinder : MonoBehaviour
    {
        PlayerController _pc;
        Inventory.Inventory _inv;
        Vector3 _lastPos;
        float _stepDist, _climbDist, _creakTimer; int _bridgeSteps;
        int _lastSecond = -1, _lastSelected;

        void OnEnable()
        {
            GameEvents.ItemPickedUp += OnPick; GameEvents.ItemDropped += OnDrop; GameEvents.InventoryFull += OnFull;
            GameEvents.ItemUsed += OnUsed; GameEvents.UseRejected += OnReject;
            GameEvents.PlayerJumped += OnJump; GameEvents.PlayerLanded += OnLand;
            GameEvents.Watered += OnWatered; GameEvents.CanRefilled += OnRefill; GameEvents.CanEmptyTried += OnDry;
            GameEvents.CollectorReady += OnCollectorReady;
            GameEvents.GameWon += OnWin; GameEvents.PlayerRespawned += OnRespawn;
            PlantInstance.Sprouted += OnSprout; PlantInstance.GrowthStarted += OnGrowStart; PlantInstance.Grown += OnGrown;
        }

        void OnDisable()
        {
            GameEvents.ItemPickedUp -= OnPick; GameEvents.ItemDropped -= OnDrop; GameEvents.InventoryFull -= OnFull;
            GameEvents.ItemUsed -= OnUsed; GameEvents.UseRejected -= OnReject;
            GameEvents.PlayerJumped -= OnJump; GameEvents.PlayerLanded -= OnLand;
            GameEvents.Watered -= OnWatered; GameEvents.CanRefilled -= OnRefill; GameEvents.CanEmptyTried -= OnDry;
            GameEvents.CollectorReady -= OnCollectorReady;
            GameEvents.GameWon -= OnWin; GameEvents.PlayerRespawned -= OnRespawn;
            PlantInstance.Sprouted -= OnSprout; PlantInstance.GrowthStarted -= OnGrowStart; PlantInstance.Grown -= OnGrown;
            if (_inv != null) _inv.Changed -= OnInvChanged;
        }

        static void P(string n, float v = 1f, float interval = 0.05f, Vector3? at = null) => AudioManager.PlaySfx(n, v, 0.05f, interval, at);

        void OnPick(ItemDefinition i) => P("pickup", 0.9f);
        void OnDrop(ItemDefinition i) => P("drop", 0.8f);
        void OnFull(ItemDefinition i) => P("inventory_full", 0.9f, 0.3f);
        void OnUsed(ItemDefinition i) { }
        void OnReject() => P("dry_click", 0.5f, 0.3f);
        void OnJump() => P("jump", 0.7f, 0.1f);
        void OnLand() { P("land", 0.8f, 0.1f); if (_pc != null && Surface(_pc.transform.position) == "footstep_bridge") P("bridge_creak", 0.7f, 0.2f); }
        void OnWatered() => P("water_pour", 0.9f, 0.3f);
        void OnRefill() => P("refill_splash", 1f, 0.3f);
        void OnDry() => P("dry_click", 0.8f, 0.3f);
        void OnCollectorReady() => P("water_ready_bell", 0.9f, 0.5f);
        void OnWin() => P("win_fanfare", 1f, 1f);
        void OnRespawn() => P("respawn", 0.7f, 0.5f);
        void OnSprout(PlantInstance p) => P("plant_seed", 0.9f, 0.1f, p.transform.position);
        void OnGrowStart(PlantInstance p) => P("grow_loop", 0.8f, 0.1f, p.transform.position);
        void OnGrown(PlantInstance p) => P("grown_chime", 1f, 0.1f, p.transform.position);
        void OnInvChanged()
        {
            if (_inv != null && _inv.Selected != _lastSelected) { _lastSelected = _inv.Selected; P("select_slot", 0.6f, 0.04f); }
        }

        void Update()
        {
            if (_pc == null)
            {
                _pc = PlayerController.Instance;
                if (_pc == null) return;
                _lastPos = _pc.transform.position;
                _inv = _pc.GetComponent<Inventory.Inventory>();
                if (_inv != null) { _lastSelected = _inv.Selected; _inv.Changed += OnInvChanged; }
            }
            if (Time.timeScale <= 0f) { _lastPos = _pc.transform.position; return; }

            Vector3 pos = _pc.transform.position;
            Vector3 d = pos - _lastPos; _lastPos = pos;
            if (d.sqrMagnitude > 4f) return;   // teleport / respawn
            if (_pc.CurrentState == PlayerController.State.Grounded)
            {
                _stepDist += new Vector2(d.x, d.z).magnitude;
                float stride = _pc.Input != null && _pc.Input.RunHeld ? 2.3f : 1.8f;
                string surf = null;
                if (_stepDist >= stride)
                {
                    _stepDist = 0f; surf = Surface(pos); P(surf, surf == "footstep_bridge" ? 0.6f : 0.55f, 0.12f);
                    if (surf == "footstep_bridge" && ++_bridgeSteps % 4 == 0) P("bridge_creak", 0.5f, 0.2f);   // every few steps the bridge groans
                }
                // standing still on a bridge: a soft sway creak now and then
                if (d.sqrMagnitude < 0.0001f)
                {
                    _creakTimer += Time.deltaTime;
                    if (_creakTimer > 6f) { _creakTimer = 0f; if (Surface(pos) == "footstep_bridge") P("bridge_creak", 0.3f, 0.2f); }
                }
                else _creakTimer = 0f;
            }
            else { _stepDist = 0f; _creakTimer = 0f; }
            if (_pc.IsClimbing)
            {
                _climbDist += Mathf.Abs(d.y);
                if (_climbDist >= 0.5f) { _climbDist = 0f; P("climb_rustle", 0.6f, 0.15f); }
            }
        }

        string Surface(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var pi = hit.collider.GetComponentInParent<Plants.PlantInstance>();
                if (pi != null && pi.kind == Plants.PlantKind.Bridge) return "footstep_bridge";
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer(Layers.PlantName)) return "footstep_plant";
                var r = hit.collider.GetComponent<Renderer>();
                var n = r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "";
                if (n == "M_Stone" || n == "M_TerrainRock") return "footstep_stone";
                if (n == "M_TerrainMud") return "footstep_mud";
                if (n == "M_TerrainSnow") return "footstep_snow";
                if (hit.collider.transform.parent != null && hit.collider.GetComponentInParent<Plants.PlantInstance>() != null) return "footstep_plant";
            }
            return "footstep_grass";
        }
    }
}
