using UnityEngine;
using PlantGame.Inventory;

namespace PlantGame.Core
{
    /// <summary>Anything that E-with-a-selected-item can act on (soil patch, ...).</summary>
    public interface IUsableTarget
    {
        bool CanUseItem(ItemDefinition item, GameObject actor);
        void UseItem(ItemDefinition item, GameObject actor);
    }
}
