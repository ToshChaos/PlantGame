using UnityEngine;

namespace PlantGame.Inventory
{
    public enum ItemKind { Seed, Key, Misc }

    /// <summary>An inventory item (contract 5.5). Seeds map to their plant kind by id (World.SeedItems).</summary>
    [CreateAssetMenu(menuName = "PlantGame/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject worldPrefab;
        public ItemKind kind;
    }
}
