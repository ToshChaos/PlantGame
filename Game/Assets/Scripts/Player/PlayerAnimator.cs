using UnityEngine;

namespace PlantGame.Player
{
    /// <summary>
    /// T31: procedural limb swing for the blocky player model (no rig, no Animator). Rotates the joint transforms exported
    /// from Blender (Joint_ArmL/R, Joint_LegL/R, Joint_Head) around their local X axis from PlayerController state:
    /// idle sway, walk/run swing (scaled by speed), an airborne pose and a hand-over-hand climb.
    /// Limbs hang along -Y, so a negative X angle swings them forward.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public PlayerController controller;
        [Tooltip("Root of the model; joints are looked up by name below it.")]
        public Transform model;

        [Header("Swing")]
        public float strideLength = 1.4f;     // metres per full step cycle
        public float walkSwing = 32f;
        public float runSwing = 50f;
        public float climbStride = 0.8f;      // metres per climb cycle
        public float blendSpeed = 14f;

        Transform _armL, _armR, _legL, _legR, _head;
        Quaternion _armLRest, _armRRest, _legLRest, _legRRest, _headRest;
        Quaternion _frame = Quaternion.identity;   // joint parent rotation in model space (the FBX root may carry an axis rotation)
        float _phase, _climbPhase, _t;

        void Awake()
        {
            if (controller == null) controller = GetComponentInParent<PlayerController>();
            if (model == null && controller != null) model = controller.model;
            var root = model != null ? model : transform;
            _armL = Find(root, "Joint_ArmL"); _armR = Find(root, "Joint_ArmR");
            _legL = Find(root, "Joint_LegL"); _legR = Find(root, "Joint_LegR");
            _head = Find(root, "Joint_Head");
            if (_armL != null) _frame = Quaternion.Inverse(root.rotation) * _armL.parent.rotation;   // all joints share one parent
            _armLRest = Rest(_armL); _armRRest = Rest(_armR); _legLRest = Rest(_legL); _legRRest = Rest(_legR); _headRest = Rest(_head);
        }

        static Quaternion Rest(Transform t) => t != null ? t.localRotation : Quaternion.identity;

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = Find(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        void LateUpdate()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            Vector3 v = controller.Velocity;
            float speed = new Vector2(v.x, v.z).magnitude;

            // angles (degrees, X rotation) for armL, armR, legL, legR, plus arm spread (Z) and head pitch
            float aL, aR, lL, lR, spread = 0f, head = 0f;
            switch (controller.CurrentState)
            {
                case PlayerController.State.Climbing:
                    _climbPhase += Mathf.Abs(v.y) / climbStride * Mathf.PI * 2f * dt;
                    float c = Mathf.Sin(_climbPhase);
                    aL = -150f + 25f * c; aR = -150f - 25f * c;
                    lL = -30f - 25f * c;  lR = -30f + 25f * c;
                    head = -15f;
                    break;
                case PlayerController.State.Airborne:
                    aL = -40f; aR = -40f; spread = 35f;
                    lL = -35f; lR = 15f;
                    break;
                default:
                    if (speed > 0.2f)
                    {
                        _phase += speed / strideLength * Mathf.PI * 2f * dt;
                        float k = Mathf.InverseLerp(controller.walkSpeed, controller.runSpeed, speed);
                        float amp = Mathf.Lerp(walkSwing, runSwing, k) * Mathf.Clamp01(speed / controller.walkSpeed);
                        float s = Mathf.Sin(_phase) * amp;
                        aL = s; aR = -s; lL = -s; lR = s;
                        head = 4f * k;
                    }
                    else
                    {
                        _phase = 0f;
                        float b = Mathf.Sin(_t * 1.8f) * 3f;   // idle breathing sway
                        aL = b; aR = -b; lL = 0f; lR = 0f; spread = 4f;
                    }
                    break;
            }

            float w = 1f - Mathf.Exp(-blendSpeed * dt);
            Pose(_armL, _armLRest, aL, -spread, w);
            Pose(_armR, _armRRest, aR, spread, w);
            Pose(_legL, _legLRest, lL, 0f, w);
            Pose(_legR, _legRRest, lR, 0f, w);
            Pose(_head, _headRest, head, 0f, w);
        }

        /// <summary>Applies a model-space rotation (X = swing forward/back, Z = spread sideways) on top of the rest pose.</summary>
        void Pose(Transform t, Quaternion rest, float x, float z, float w)
        {
            if (t == null) return;
            var target = Quaternion.Inverse(_frame) * Quaternion.Euler(x, 0f, z) * _frame * rest;
            t.localRotation = Quaternion.Slerp(t.localRotation, target, w);
        }
    }
}
