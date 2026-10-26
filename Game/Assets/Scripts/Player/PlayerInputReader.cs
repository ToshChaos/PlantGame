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
            if (GetComponent<TouchControls>() == null) gameObject.AddComponent<TouchControls>();   // T57: draws and reads the on-screen controls on touch devices only
        }

        void OnEnable() { _map?.Enable(); }
        void OnDisable() { _map?.Disable(); }

        /// <summary>Display string of the binding for an action ("Pickup", "Use", "Water", "Pause"), e.g. "Q".</summary>
        public string KeyLabel(string action)
        {
            var a = _map?.FindAction(action == "Pickup" ? "PickupDrop" : action);
            return a != null ? a.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions) : "?";
        }

        /// <summary>Climbing axis: W / S and the up / down arrow keys (plus the stick), -1..1.</summary>
        public float ClimbAxis
        {
            get
            {
                if (!Enabled) return 0f;
                float v = Move.y; var k = Keyboard.current;
                if (k != null) v += (k.upArrowKey.isPressed ? 1f : 0f) - (k.downArrowKey.isPressed ? 1f : 0f);
                return Mathf.Clamp(v, -1f, 1f);
            }
        }
        public Vector2 Move => Enabled && _move != null ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>() + TouchInput.Move, 1f) : Vector2.zero;
        /// <summary>Mouse delta, or the right-side drag while the touch controls are shown (a stray mouse must not spin the camera then).</summary>
        public Vector2 Look => !Enabled || _look == null ? Vector2.zero : TouchInput.Active ? TouchInput.Look : _look.ReadValue<Vector2>();
        public bool JumpPressed => Enabled && (TouchInput.JumpPressed || _jump != null && _jump.WasPressedThisFrame());
        public bool RunHeld => Enabled && (TouchInput.Run || _run != null && _run.IsPressed());
        /// <summary>Touch context button: pick up, use or drop, whichever PlayerInteraction says it is (T58).</summary>
        public bool ContextPressed => Enabled && TouchInput.ContextPressed;
        /// <summary>Q: pick up the focused item, or drop the selected one (resolution in T04).</summary>
        public bool PickupDropPressed => Enabled && _pickupDrop != null && _pickupDrop.WasPressedThisFrame();
        /// <summary>E: use the selected item on the focused target, or interact with the focused Use-interactable.</summary>
        public bool UsePressed => Enabled && _use != null && _use.WasPressedThisFrame();
        /// <summary>F: water a sprout.</summary>
        public bool WaterPressed => Enabled && (TouchInput.WaterPressed || _water != null && _water.WasPressedThisFrame());
        public bool PausePressed => TouchInput.PausePressed || _pause != null && _pause.WasPressedThisFrame();   // not gated by Enabled: Esc must always work
        public bool RestartHeld => Enabled && _restart != null && _restart.IsPressed();

        /// <summary>+1 / -1 when the player pressed Tab / Shift+Tab this frame, else 0.</summary>
        public int CycleDelta
        {
            get
            {
                if (!Enabled || _cycle == null) return 0;
                if (TouchInput.CyclePressed) return 1;
                if (_cycle.WasPressedThisFrame())
                {
                    var kb = Keyboard.current;
                    return kb != null && kb.shiftKey.isPressed ? -1 : 1;
                }
                return 0;   // the mouse wheel / trackpad scroll no longer cycles slots (accidental switching on laptops)
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
