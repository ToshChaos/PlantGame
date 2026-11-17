using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Inventory
{
    /// <summary>
    /// An item lying in the world (layer Interactable). Q picks it up into the actor's Inventory and destroys it.
    /// The world prefab of an ItemDefinition carries this component with `item` pointing back at the definition.
    /// </summary>
    public class WorldItem : MonoBehaviour, IInteractable
    {
        public ItemDefinition item;

        /// <summary>A pickup fell off the islands and is gone (its source, if it has one, grows another: SeedTree).</summary>
        public static event System.Action<ItemDefinition> Lost;

        /// <summary>Below this height a pickup has left the level: the player's own kill plane (30 m under the lowest island).</summary>
        static float LostBelow => PlantGame.Player.PlayerController.Instance != null ? PlantGame.Player.PlayerController.Instance.killPlaneY : -200f;

        /// <summary>Dropped, rolled or bounced off the side: the pickup disappears instead of falling for ever.</summary>
        void Update()
        {
            if (transform.position.y >= LostBelow) return;
            Lost?.Invoke(item);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public InteractAction Action => InteractAction.Pickup;
        public string Prompt => "Pick up " + (item != null ? item.displayName : name) + (item != null && item.id == "parachute_leaf" ? " - try gliding" : "");   // the HUD only shows prompts with Hints on

        public bool CanInteract(GameObject actor)
        {
            var inv = actor != null ? actor.GetComponent<Inventory>() : null;
            return item != null && inv != null && !inv.IsFull;
        }

        public void Interact(GameObject actor)
        {
            var inv = actor != null ? actor.GetComponent<Inventory>() : null;
            if (item == null || inv == null || !inv.TryAdd(item)) return;
            GameEvents.RaiseItemPickedUp(item);
            PickupFly.Launch(TryGetComponent<ItemBob>(out var bob) ? bob.visual : null, actor.transform);   // the look outlives the pickup for a moment and flies to the hand
            gameObject.SetActive(false);   // gone for the sensor / markers this very frame (Destroy only happens at frame end)
            Destroy(gameObject);
        }
    }
}
