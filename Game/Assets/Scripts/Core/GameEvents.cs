using System;
using PlantGame.Inventory;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>
    /// Static C# events: systems talk through these instead of hard references (AGENTS.md 5.5).
    /// Raise through the Raise* helpers so subscribers are never null-checked by callers.
    /// </summary>
    public static class GameEvents
    {
        // Inventory / items
        public static event Action<ItemDefinition> ItemPickedUp, ItemDropped, ItemUsed;
        // Plants
        public static event Action<PlantDefinition> PlantStarted, PlantGrown;
        // World / game flow
        public static event Action<int> WorldGenerated;                       // seed
        public static event Action GameWon, GameFailed, PlayerRespawned, PlayerJumped, PlayerLanded;
        public static event Action<GameMode> RunStarted;
        public static event Action<float, bool> RunFinished;                  // seconds, newBest
        // Water
        public static event Action<int> CanChanged;                           // charges left
        public static event Action Watered, CanRefilled, CanEmptyTried, CollectorReady;

        public static void RaiseItemPickedUp(ItemDefinition i) => ItemPickedUp?.Invoke(i);
        public static void RaiseItemDropped(ItemDefinition i) => ItemDropped?.Invoke(i);
        public static void RaiseItemUsed(ItemDefinition i) => ItemUsed?.Invoke(i);
        public static void RaisePlantStarted(PlantDefinition p) => PlantStarted?.Invoke(p);
        public static void RaisePlantGrown(PlantDefinition p) => PlantGrown?.Invoke(p);
        public static void RaiseWorldGenerated(int seed) => WorldGenerated?.Invoke(seed);
        public static void RaiseGameWon() => GameWon?.Invoke();
        public static void RaiseGameFailed() => GameFailed?.Invoke();
        public static void RaisePlayerRespawned() => PlayerRespawned?.Invoke();
        public static void RaisePlayerJumped() => PlayerJumped?.Invoke();
        public static void RaisePlayerLanded() => PlayerLanded?.Invoke();
        public static void RaiseRunStarted(GameMode m) => RunStarted?.Invoke(m);
        public static void RaiseRunFinished(float seconds, bool newBest) => RunFinished?.Invoke(seconds, newBest);
        public static void RaiseCanChanged(int charges) => CanChanged?.Invoke(charges);
        public static void RaiseWatered() => Watered?.Invoke();
        public static void RaiseCanRefilled() => CanRefilled?.Invoke();
        public static void RaiseCanEmptyTried() => CanEmptyTried?.Invoke();
        public static void RaiseCollectorReady() => CollectorReady?.Invoke();
    }
}
