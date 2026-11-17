## Ideas not done

- **Bridge**: flowers that open as the growth front passes (needs a per-flower growth offset in the bake).
- **Prefab instead of runtime split**: `ItemBuilder` could build the pickups with the `Visual` child (and `ItemBob.visual`
  serialized) so `ItemBob.BuildVisual` only wires things up. Needs PlantGame > Assets > Items and a look at every user of
  `worldPrefab.GetComponentInChildren<MeshFilter>()` (`WorldGenerator.SeedSource`, `SeedTree.ShowBud`, `PlayerHud`).
- **Blender side** (A3): more silhouette variety per seed (`SeedMeshes` is code, the plants are bakes); a per-flower growth offset
  and a "distance from stem" attribute in vertex colour B would unlock opening flowers and a sway that keeps leaf roots still.
