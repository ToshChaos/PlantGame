using System.Collections;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// The magic watering can in the player's right hand, shown while the can holds water (charges > 0). The meshes are part
    /// of the player model (Hold_Can under Joint_ArmR, see char_player.py), so they take the outfit's materials; the can tips
    /// forward (Can_Tilt) when a sprout is watered and the glowing water in the top (CanWater_Glow) shows it is full.
    /// </summary>
    public class WateringCanVisual : MonoBehaviour
    {
        WateringCan _can;
        GameObject _root;
        Transform _tilt, _fill;
        Coroutine _pour;
        Player.PlayerAnimator _anim;

        void Start()
        {
            _can = GetComponent<WateringCan>();
            _anim = GetComponentInChildren<Player.PlayerAnimator>(true);
            var pc = GetComponent<Player.PlayerController>();
            Transform model = pc != null ? pc.model : transform;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Hold_Can") _root = t.gameObject;
                else if (t.name == "Can_Tilt") _tilt = t;
                else if (t.name == "CanWater_Glow") _fill = t;
            }
            if (_root == null) { Debug.LogWarning("[PlantGame] Hold_Can missing from the player model; rebuild it with char_player.py."); enabled = false; return; }
            _root.SetActive(false);
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
            if (_fill != null) _fill.gameObject.SetActive(_can.IsFull && !filling);
            if (_anim != null) _anim.scoop = Mathf.MoveTowards(_anim.scoop, filling ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            if (_pour == null && _tilt != null && _anim != null) _tilt.localRotation = Quaternion.Euler(25f * _anim.scoop, 0, 0);   // can mouth tipped toward the water
        }
    }
}
