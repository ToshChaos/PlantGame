using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Inventory
{
    /// <summary>
    /// T04: an item lying in the world (layer Interactable). Q picks it up into the actor's Inventory and destroys it.
    /// The world prefab of an ItemDefinition carries this component with `item` pointing back at the definition.
    /// </summary>
    public class WorldItem : MonoBehaviour, IInteractable
    {
        public ItemDefinition item;

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
            gameObject.SetActive(false);   // gone for the sensor / markers this very frame (Destroy only happens at frame end)
            Destroy(gameObject);
        }
    }
}
