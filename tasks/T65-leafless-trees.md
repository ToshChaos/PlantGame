# T65 — Bare trees: snow-level leaf trees and a rare leafless variant

**Owner:** A5 Cartographer + A6 Tech Artist · **Status:** DONE (accepted 2026-10-27) · **Depends on:** T36, T39, T45, T56

## Goal
Leafy (non-fir, non-palm) trees look wrong on the snow level. Make them bare, and use the same bare look as a rare variation elsewhere.

## Deliverables
- **Snow levels** (`LevelTheme.Snow`; fir trees stay as they are): every broadleaf tree is leafless except for exactly **two yellow leaves** (or two small yellow leaf clusters) left on the tree, chosen deterministically per tree.
- **Other levels:** a rare leafless variation (about 1 tree in 12, per-island weight, deterministic from the layout seed through a position hash like the size variation in T56, so layouts and golden seeds do not change). Autumn levels may weight it up a little.
- Falling-leaf effects (`FallingBits.AddLeaves`, `_leafTrees`) and the `leaf_rustle` loop skip bare trees; bare trees keep their trunk collider.

## Notes
- Leaf crowns are swapped per material in `Scenery` (`Q_Leaves_*`, `M_LeafTint`); a bare tree can hide or drop the leaf submesh, or use a fully clipped material. Two yellow leaves can be small quads using `Palette.LeafYellow`.
- Use an own random / position hash, never the generation stream, so `WorldGenerationTests.GoldenSeedsKeepTheirWorld` still passes.
- Check: Snowed In (level 7) in play, an autumn level for the rare variant, console clean.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
