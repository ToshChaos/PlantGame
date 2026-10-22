using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PlantGame.Core;

namespace PlantGame.Player
{
    /// <summary>
    /// T03: first-person CharacterController with a tight state machine: Grounded, Airborne, Climbing.
    /// Numbers from AGENTS.md 5.1: walk 4 m/s, run 6 m/s, jump height 1.6 m, coyote 0.12 s, jump buffer 0.12 s.
    /// With gravity 20 m/s^2 the jump is 8 m/s up, 0.8 s in the air: ~4.8 m flat gap at run speed (+ coyote), so 5 m clears and 7 m does not.
    /// Controls (5.8): W/S climb, Space jumps off the vine (+~2 m/s away, 0.3 s re-grab cooldown).
    /// Self-contained: needs CharacterController + PlayerInputReader on the same object and a camera pivot child.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public enum State { Grounded, Airborne, Climbing }

        [Header("Movement")]
        public float walkSpeed = 4f;
        public float runSpeed = 6f;
        public float groundAccel = 60f;
        public float airAccel = 14f;
        public float jumpHeight = 1.6f;
        public float gravity = 20f;
        public float maxFallSpeed = 40f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        [Tooltip("Airborne shorter than this does not count as a landing (stairs, small steps).")]
        public float minAirTimeForLanding = 0.15f;

        [Header("Climbing")]
        public float climbSpeed = 3f;
        public float climbJumpPush = 2f;
        public float regrabCooldown = 0.3f;
        public float climbSnapSpeed = 8f;
        [Tooltip("Within this distance of the top of a volume, W does not grab; S starts a descent.")]
        public float topZone = 1.0f;

        [Header("Look / camera")]
        public Transform cameraPivot;
        public float lookSensitivity = 0.1f;   // degrees per mouse count
        public float pitchLimit = 85f;

        [Header("Falling")]
        [Tooltip("Respawn when y drops below this. T15 sets it per world: island bottom - 30 m.")]
        public float killPlaneY = -50f;
        [Tooltip("T15 assigns the last visited island Spawn. Falls back to the 'Respawn' tag, then the start position.")]
        public Transform respawnPoint;

        [Header("Misc")]
        public float restartHoldSeconds = 1f;

        public State CurrentState { get; private set; } = State.Airborne;
        public bool IsClimbing => CurrentState == State.Climbing;
        public Transform EyeTransform => cameraPivot != null ? cameraPivot : transform;
        public PlayerInputReader Input { get; private set; }
        public bool CursorLocked { get; private set; }
        /// <summary>Raised when R was held for restartHoldSeconds. T15 wires this to RunSession.Restart.</summary>
        public event Action RestartRequested;

        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        CharacterController _cc;
        Vector3 _hVel;
        float _vVel;
        float _coyote, _jumpBuffer, _regrab, _airTime, _pitch, _restartTimer;
        bool _restartFired;
        ClimbableVolume _touching, _climb;
        Vector3 _startPos;
        float _startYaw;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Input = GetComponent<PlayerInputReader>();
            _cc.height = 1.8f; _cc.radius = 0.35f; _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.stepOffset = 0.35f; _cc.slopeLimit = 45f; _cc.skinWidth = 0.03f; _cc.minMoveDistance = 0f;
            _startPos = transform.position;
            _startYaw = transform.eulerAngles.y;
        }

        void Start() { SetCursorLocked(true); }

        void OnDisable() { SetCursorLocked(false); }

        void Update() { Tick(Time.deltaTime); }

        /// <summary>One simulation step. Public so editor tests can drive the controller deterministically.</summary>
        public void Tick(float dt)
        {
            UpdateTouching();
            HandleCursorAndLook();
            HandleRestartHold(dt);

            _regrab = Mathf.Max(0f, _regrab - dt);
            if (Input.JumpPressed) _jumpBuffer = jumpBufferTime; else _jumpBuffer = Mathf.Max(0f, _jumpBuffer - dt);

            if (CurrentState == State.Climbing) UpdateClimb(dt);
            else UpdateMove(dt);

            if (transform.position.y < killPlaneY) Respawn();
        }

        // ---------------------------------------------------------------- ground + air

        void UpdateMove(float dt)
        {
            Vector2 mi = Input.Move;
            bool grounded = CurrentState == State.Grounded;

            // Climb entry: W grabs below the top zone, S at the top edge (standing) starts a descent.
            if (_touching != null && _regrab <= 0f)
            {
                bool nearTop = transform.position.y >= _touching.TopY - topZone;
                if ((mi.y > 0.5f && !nearTop) || (mi.y < -0.5f && nearTop && grounded))
                {
                    StartClimb(_touching);
                    return;
                }
            }

            Vector3 wish = transform.right * mi.x + transform.forward * mi.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 target = wish * (Input.RunHeld ? runSpeed : walkSpeed);
            _hVel = Vector3.MoveTowards(_hVel, target, (grounded ? groundAccel : airAccel) * dt);

            if (grounded) _coyote = coyoteTime; else _coyote = Mathf.Max(0f, _coyote - dt);

            if (_jumpBuffer > 0f && _coyote > 0f)
            {
                _vVel = JumpVelocity;
                _jumpBuffer = 0f; _coyote = 0f;
                CurrentState = State.Airborne; _airTime = 0f; grounded = false;
                GameEvents.RaisePlayerJumped();
            }
            else if (grounded) _vVel = -2f;                       // keep isGrounded true on flat ground and slopes
            else _vVel = Mathf.Max(_vVel - gravity * dt, -maxFallSpeed);

            var flags = _cc.Move((_hVel + Vector3.up * _vVel) * dt);
            if ((flags & CollisionFlags.Above) != 0 && _vVel > 0f) _vVel = 0f;

            bool nowGrounded = _cc.isGrounded && _vVel <= 0f;
            if (nowGrounded)
            {
                if (CurrentState == State.Airborne && _airTime >= minAirTimeForLanding) GameEvents.RaisePlayerLanded();
                CurrentState = State.Grounded; _airTime = 0f; _vVel = -2f;
            }
            else
            {
                if (CurrentState == State.Grounded) { CurrentState = State.Airborne; _airTime = 0f; }
                _airTime += dt;
            }
        }

        // ---------------------------------------------------------------- climbing

        void StartClimb(ClimbableVolume vol)
        {
            _climb = vol;
            CurrentState = State.Climbing;
            _hVel = Vector3.zero; _vVel = 0f; _airTime = 0f; _jumpBuffer = 0f;
        }

        void ExitClimb(State next)
        {
            _climb = null;
            CurrentState = next;
            _regrab = regrabCooldown;
        }

        void UpdateClimb(float dt)
        {
            if (_climb == null || _touching != _climb) { ExitClimb(State.Airborne); return; }   // left the volume

            // Space: jump off the vine (normal jump velocity + a small push away from the surface).
            if (Input.JumpPressed)
            {
                Vector3 push = _climb.SurfaceNormal * climbJumpPush;
                ExitClimb(State.Airborne);
                _vVel = JumpVelocity; _hVel = push; _coyote = 0f; _jumpBuffer = 0f;
                GameEvents.RaisePlayerJumped();
                return;
            }

            float input = Input.Move.y;   // W up, S down; A / D ignored
            Vector3 toLine = _climb.ClimbLinePoint - transform.position;
            toLine.y = 0f;
            bool snapped = toLine.magnitude < 0.05f;
            Vector3 horiz = Vector3.ClampMagnitude(toLine, climbSnapSpeed * dt);
            float vert = snapped ? input * climbSpeed * dt : 0f;   // no vertical motion until on the line (avoids scraping the lip)

            _cc.Move(horiz + Vector3.up * vert);

            // Top: step onto Land.
            if (input > 0.1f && snapped && transform.position.y >= _climb.TopY - 0.05f)
            {
                var land = _climb.LandPosition;
                Teleport(land);
                ExitClimb(State.Grounded);
                _regrab = 0.05f;   // not the full cooldown: holding S right after arriving must start the descent, not walk off the edge
                _vVel = -2f;
                return;
            }
            // Bottom: touching the floor while descending on the line.
            if (input < -0.1f && snapped && _cc.isGrounded) ExitClimb(State.Grounded);
        }

        // ---------------------------------------------------------------- climb volume lookup

        /// <summary>The climb volume containing the player's lower body, or null. Polled each tick (deterministic; no trigger callbacks).</summary>
        void UpdateTouching()
        {
            _touching = null;
            Vector3 probe = transform.position + Vector3.up * 0.5f;
            var all = ClimbableVolume.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Contains(probe)) { _touching = all[i]; break; }
        }

        // ---------------------------------------------------------------- respawn / teleport

        public void Teleport(Vector3 position)
        {
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = true;
        }

        /// <summary>Respawn at respawnPoint, else the object tagged Respawn, else the start position.</summary>
        public void Respawn()
        {
            Vector3 pos = _startPos; float yaw = _startYaw;
            Transform p = respawnPoint;
            if (p == null)
            {
                var go = GameObject.FindGameObjectWithTag(Layers.RespawnTag);
                if (go != null) p = go.transform;
            }
            if (p != null) { pos = p.position; yaw = p.eulerAngles.y; }
            Respawn(pos, yaw);
        }

        public void Respawn(Vector3 position, float yaw)
        {
            Teleport(position);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _hVel = Vector3.zero; _vVel = 0f; _airTime = 0f; _climb = null; _touching = null;
            CurrentState = State.Airborne;
            GameEvents.RaisePlayerRespawned();
        }

        // ---------------------------------------------------------------- look / cursor / restart

        public void SetCursorLocked(bool locked)
        {
            CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void HandleCursorAndLook()
        {
            if (Input.PausePressed && CursorLocked) SetCursorLocked(false);                  // Esc releases the cursor
            else if (!CursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Input.Enabled)
                SetCursorLocked(true);                                                         // click to grab it back

            if (!CursorLocked) return;
            Vector2 look = Input.Look * lookSensitivity;
            transform.Rotate(0f, look.x, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - look.y, -pitchLimit, pitchLimit);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void HandleRestartHold(float dt)
        {
            if (Input.RestartHeld)
            {
                _restartTimer += dt;
                if (!_restartFired && _restartTimer >= restartHoldSeconds) { _restartFired = true; RestartRequested?.Invoke(); }
            }
            else { _restartTimer = 0f; _restartFired = false; }
        }
    }
}
