using System.Collections;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// The magic watering can in the player's right hand, shown while the can holds water (charges > 0). The meshes are part
    /// of the player model (Hold_Can under Joint_ArmR, see char_player.py), so they take the outfit's materials; the can tips
    /// forward (Can_Tilt) when a sprout is watered and the glowing water in the top (CanWater_Glow) shows it is full.
    /// While the player climbs or glides (both hands busy) the can eases onto the back, spout up over the right shoulder;
    /// it stays a child of the arm and is placed after PlayerAnimator has posed the arm (hence the execution order).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class WateringCanVisual : MonoBehaviour
    {
        // the stowed can in model space (x right, y up, z forward): the grip sits behind the shoulder blades, the can's
        // bottom rests on the cloak and its top faces back and a little up, the spout points up over the right shoulder
        static readonly Vector3 BackGrip = new Vector3(-0.03f, 0.95f, -0.36f);
        static readonly Quaternion BackTurn = Quaternion.Euler(0f, 0f, -30f) * Quaternion.Euler(-70f, 0f, 0f);
        const float StowSpeed = 5f;   // 1 / seconds

        WateringCan _can;
        GameObject _root;
        Transform _tilt, _fill, _model, _body;
        Vector3 _holdPos; Quaternion _holdRot, _holdInModel;
        float _stow;
        Coroutine _pour;
        Player.PlayerAnimator _anim;
        Player.PlayerController _pc;

        void Start()
        {
            _can = GetComponent<WateringCan>();
            _anim = GetComponentInChildren<Player.PlayerAnimator>(true);
            var pc = _pc = GetComponent<Player.PlayerController>();
            Transform model = _model = pc != null ? pc.model : transform;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Hold_Can") _root = t.gameObject;
                else if (t.name == "Can_Tilt") _tilt = t;
                else if (t.name == "CanWater_Glow") _fill = t;
            }
            if (_root == null) { Debug.LogWarning("[PlantGame] Hold_Can missing from the player model; rebuild it with char_player.py."); enabled = false; return; }
            _root.SetActive(false);
            var hold = _root.transform;
            _holdPos = hold.localPosition; _holdRot = hold.localRotation;
            var arm = hold.parent; _body = arm != null ? arm.parent : null;
            // the can's rotation in model space when the arm hangs at rest (the joints carry no rest rotation of their own)
            if (_body != null) _holdInModel = Quaternion.Inverse(_model.rotation) * _body.rotation * _holdRot;
        }

        void OnEnable() { GameEvents.Watered += OnWatered; }
        void OnDisable() { GameEvents.Watered -= OnWatered; }

        void OnWatered() { if (_root != null && _root.activeInHierarchy) { if (_pour != null) StopCoroutine(_pour); _pour = StartCoroutine(Pour()); } }

        IEnumerator Pour()
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float k = Mathf.Sin(t * Mathf.PI);
                if (_tilt != null) _tilt.localRotation = Quaternion.Euler(55f * k, 0, 0);
                yield return null;
            }
            _pour = null;
            if (_tilt != null) _tilt.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (_root == null || _can == null) return;
            bool show = _can.Charges > 0 || _pour != null;   // the last charge is spent before the pour starts: keep the can until it has tipped
            if (_root.activeSelf != show) _root.SetActive(show);
            bool filling = _can.IsRefilling;
            bool full = _can.IsFull && !filling;
            if (_fill != null && _fill.gameObject.activeSelf != full) _fill.gameObject.SetActive(full);
            if (_anim != null) _anim.scoop = Mathf.MoveTowards(_anim.scoop, filling ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            if (_pour == null && _tilt != null && _anim != null) _tilt.localRotation = Quaternion.Euler(25f * _anim.scoop, 0, 0);   // can mouth tipped toward the water
        }

        void LateUpdate()
        {
            if (_root == null || _body == null) return;
            bool busy = _pc != null && (_pc.IsClimbing || _pc.IsGliding);
            _stow = _root.activeSelf ? Mathf.MoveTowards(_stow, busy ? 1f : 0f, Time.deltaTime * StowSpeed) : (busy ? 1f : 0f);   // a hidden can switches at once
            var hold = _root.transform;
            hold.localPosition = _holdPos; hold.localRotation = _holdRot;   // in the hand, wherever the animator put the arm
            if (_stow <= 0f) return;
            float k = _stow * _stow * (3f - 2f * _stow);
            Vector3 backPos = _model.TransformPoint(BackGrip);
            Quaternion backRot = _model.rotation * BackTurn * _holdInModel;
            hold.SetPositionAndRotation(Vector3.Lerp(hold.position, backPos, k), Quaternion.Slerp(hold.rotation, backRot, k));
        }
    }
}
