using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.Inventory
{
    public enum ItemKind { Seed, Key, Misc }

    /// <summary>STUB from T00 (contract 5.5). Owner A1 extends this in T05.</summary>
    [CreateAssetMenu(menuName = "PlantGame/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject worldPrefab;
        public ItemKind kind;
        public PlantDefinition plant;   // set if kind == Seed
    }
}
