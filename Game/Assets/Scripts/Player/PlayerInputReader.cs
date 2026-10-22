using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantGame.Player
{
    /// <summary>
    /// T03: thin wrapper over the "Player" action map in PlantGameInput.inputactions (bindings per AGENTS.md 5.8).
    /// Other systems (T04 interaction, T05 inventory, T19 UI) read input from here instead of touching devices.
    /// Reads are polled per frame: call from Update.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;

        InputActionMap _map;
        InputAction _move, _look, _jump, _run, _pickupDrop, _cycle, _use, _water, _pause, _restart, _scroll;
        InputAction[] _slots;

        /// <summary>When false every read returns "nothing pressed" (pause menu, win screen, ...).</summary>
        public bool Enabled { get; set; } = true;

        void Awake()
        {
            if (actions == null) { Debug.LogError("[PlayerInputReader] No InputActionAsset assigned.", this); enabled = false; return; }
            actions = Instantiate(actions);   // per-instance copy: no shared state between players / domain reloads
            _map = actions.FindActionMap("Player", true);
            _move = _map.FindAction("Move", true);
            _look = _map.FindAction("Look", true);
            _jump = _map.FindAction("Jump", true);
            _run = _map.FindAction("Run", true);
            _pickupDrop = _map.FindAction("PickupDrop", true);
            _cycle = _map.FindAction("CycleSlot", true);
            _use = _map.FindAction("Use", true);
            _water = _map.FindAction("Water", true);
            _pause = _map.FindAction("Pause", true);
            _restart = _map.FindAction("RestartHold", true);
            _scroll = _map.FindAction("Scroll", true);
            _slots = new[] { _map.FindAction("Slot1", true), _map.FindAction("Slot2", true), _map.FindAction("Slot3", true), _map.FindAction("Slot4", true) };
        }

        void OnEnable() { _map?.Enable(); }
        void OnDisable() { _map?.Disable(); }

        public Vector2 Move => Enabled && _move != null ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public Vector2 Look => Enabled && _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => Enabled && _jump != null && _jump.WasPressedThisFrame();
        public bool JumpHeld => Enabled && _jump != null && _jump.IsPressed();
        public bool RunHeld => Enabled && _run != null && _run.IsPressed();
        /// <summary>Q: pick up the focused item, or drop the selected one (resolution in T04).</summary>
        public bool PickupDropPressed => Enabled && _pickupDrop != null && _pickupDrop.WasPressedThisFrame();
        /// <summary>E: use the selected item on the focused target, or interact with the focused Use-interactable.</summary>
        public bool UsePressed => Enabled && _use != null && _use.WasPressedThisFrame();
        /// <summary>F: water a sprout.</summary>
        public bool WaterPressed => Enabled && _water != null && _water.WasPressedThisFrame();
        public bool PausePressed => _pause != null && _pause.WasPressedThisFrame();   // not gated by Enabled: Esc must always work
        public bool RestartHeld => Enabled && _restart != null && _restart.IsPressed();

        /// <summary>+1 / -1 when the player pressed Tab / Shift+Tab or scrolled this frame, else 0.</summary>
        public int CycleDelta
        {
            get
            {
                if (!Enabled || _cycle == null) return 0;
                if (_cycle.WasPressedThisFrame())
                {
                    var kb = Keyboard.current;
                    return kb != null && kb.shiftKey.isPressed ? -1 : 1;
                }
                float s = _scroll.ReadValue<Vector2>().y;
                if (s > 0.01f) return -1;      // wheel up = previous slot
                if (s < -0.01f) return 1;
                return 0;
            }
        }

        /// <summary>0..3 when 1..4 was pressed this frame, else -1.</summary>
        public int SlotPressed
        {
            get
            {
                if (!Enabled || _slots == null) return -1;
                for (int i = 0; i < _slots.Length; i++) if (_slots[i].WasPressedThisFrame()) return i;
                return -1;
            }
        }
    }
}
