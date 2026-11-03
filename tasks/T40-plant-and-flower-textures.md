# T40 — Better vine-bridge flowers, flower texture, bamboo heights

**Owner:** A3 + A6 · **Depends on:** T06, T09, T33
**Status:** DONE

## Goal
- **Vine bridge flowers:** improve the texture and shape of the flowers on the vine bridge (petal detail, gradient, a glowing centre; stay in the white/green/blue palette with accents).
- **Flower texture upgrade** for the scenery flowers too (petals, centres, variety of colours).
- **Bamboo plant:** stems get different heights (seeded per variant), not one uniform height. Applies to the in-world bamboo plant and the bamboo seed-tree visuals if they share the mesh.

## Acceptance
- Screenshots before/after in `docs/shots/`.
- Bamboo variants differ visibly; reach envelopes in AGENTS.md 5.2 still hold.
- Blender scripts updated in `Source/Blender/scripts/`, exports re-synced.

## Handoff
Vine-bridge flowers (`plants_bridge.py` `flower()`): two rings of flattened cupped petals (7 outer, 5 inner) round a glowing centre, larger, density cut to stay under 4k tris (max 3.9k). `PlantGrow.shader` part ids: .995 glow centre (rim cyan), .985 white petals, .975 mist-blue petals, 1.0 unchanged. Bridges re-exported and synced. Scenery flowers: two colours per clump, 4 per clump, varied heights. Bamboo grove (`Scenery.BambooGrove`) now mixes short / medium / tall culms with joints scaled to height. Screenshot `docs/shots/T40_bridge_flowers.png`. Not done: texturing scenery flower petals (Kenney models keep their own colour map); the inner mist-blue petals are subtle.
