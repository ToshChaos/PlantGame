# Art bible — Overgrown

Soft, glowing, stylized fantasy in **white / green / blue**. Calm and airy: you are a gardener among clouds, not a survivor.
Read time: ~2 minutes. Owner: A6 (T01). Palette source of truth in code: `Scripts/Core/Palette.cs`.

## Palette (exact hexes)
| Name | Hex | Use |
|------|-----|-----|
| Cloud white | `#F4F8F6` | clouds, plant tips, highlights, UI text |
| Mist blue | `#BFD9EE` | stone, fog, sky horizon, ambient sky |
| Sky blue | `#6FA8DC` | water, sky, UI accents |
| Deep blue | `#2C5E8F` | deep water, shadows in cold areas, UI panels |
| Leaf green | `#5DB46B` | plant bodies; island tops use a deeper mix of moss and leaf green (`M_Terrain`) |
| Seed lime | `#B8E35F` | the basic (vine) seed: must read against the grass |
| Moss | `#2F7D4F` | shaded foliage, undersides, ambient ground bounce |
| Glow cyan | `#7CF2E6` | **magic and accent only**: growth front, rim light, water edge, crystals. Never a base colour. |

Rule of thumb: ~60% mist/cloud/sky, ~30% greens, ~10% glow cyan.

## Shape language
- Rounded, chunky, low-poly. Bevel every hard edge a little. No sharp thin spikes.
- Plants are ribbons, vines and discs: readable silhouette against the sky from 20 m.
- Ruins are weathered and overgrown, never scary. Islands have a rocky underside that tapers like a root ball.
- Vertex colours and gradients instead of textures. No texture is required anywhere.

## Lighting recipe
- One soft directional light: warm white `(1, 0.97, 0.90)`, intensity ~1.1, rotation `(50, -30, 0)`, soft shadows.
- Ambient: trilight — sky mist blue, equator cloud white, ground moss (set by `SandboxPlantsBuilder.ApplyLightingPreset`).
- Fog: linear, mist blue, start 40 m, end 220 m (final sky/fog/bloom in T16).
- Rim light on plants (glow cyan, power 3) so they pop against the sky. Gentle bloom only; HDR emission is for crystals and the growth front.

## Materials (`Assets/Art/Materials`, built by `PlantGame > 2 Build Materials`)
| Material | Shader | Colour |
|----------|--------|--------|
| `M_Terrain` | URP Lit | leaf green |
| `M_Stone` | URP Lit | mist blue |
| `M_Foliage` | URP Lit | moss |
| `M_Cloud` | URP Lit + faint emission | cloud white |
| `M_Crystal` | URP Lit + HDR emission x2 | glow cyan |
| `M_UI` | URP Unlit | cloud white |
| `M_PlantGrow` | `PlantGame/PlantGrow` | leaf green -> cloud white, moss shade, cyan rim |

## `M_PlantGrow` (growth shader)
Reads **vertex colour R as `growth_t`** (0 at the base, 1 at the tip; AGENTS.md 5.3).

| Property | Meaning |
|----------|---------|
| `_Growth` 0..1 | how much of the plant is visible; geometry with `growth_t > _Growth` is clipped |
| `_TaperLength`, `_TaperAmount` | the tip within `_TaperLength` of the front is pulled inward along its normals (metres), so the growing end scales in to a point. Fades out in the last 8% of growth |
| `_FrontGlowWidth`, `_FrontGlowStrength` | glow band at the growth front (only while growing) |
| `_BaseColor`, `_TipColor`, `_ShadeColor` | two-tone gradient by `growth_t` (`_GradientStart/_End`), shade tint in the toon ramp |
| `_RimColor`, `_RimPower`, `_RimStrength` | rim light |
| `_ShadeSoftness` | softness of the toon band |

Usage in C#: `MaterialPropertyBlock.SetFloat("_Growth", t)` per renderer (see `Scripts/Plants/GrowSweep.cs`) or a material instance.
Main-light only (no additional lights), shadow-casting and depth-only passes honour the same clipping.

**Requests to A3 (Blender):** the reveal and taper are per vertex, so meshes need density along the growth axis:
**at least 24 segments (rings) per bridge/ladder, >= 16 per platform spiral turn**. Provide `growth_t` as a linear 0..1 ramp along the plant.
Surface normals must be clean (the taper moves vertices along them).

## Do / Don't
- Do keep silhouettes simple and glowing accents rare. Do keep contrast between walkable surface (light) and drop (dark blue).
- Don't use pure black, saturated red/orange, or noisy textures. Don't use glow cyan for large surfaces.
- Don't add photographic or realistic detail; stylized beats detailed.

## Mood board (text)
A floating garden at sunrise: pale mist-blue sky, cloud banks lit from below, islands crowned with soft green meadow,
white-stone ruins wrapped in vines, a thin waterfall dissolving into spray, small glowing cyan motes drifting up where a plant grows.
Think: gentle animated-film landscapes; harp and pad music.

## Textures (procedural, no sourced assets)
`PlantGame > 11 Build Textures` writes tileable PNGs to `Art/Textures`: `T_Terrain` (grass mottling), `T_Rock` (cracked rock), `T_Seed` (ridged seed coat), each with a `_N` normal map from the same height field. They multiply the palette colour of `M_Terrain`, `M_Stone` and the `M_Seed*` materials. Island slabs use `TexturedBox` (world-scale UVs, 1 tile per 3 m; grass top, rock sides).

## Reference mood (user screenshot, T16 art pass)
Lush and bright: dense round trees and bushes with flowers on the island rims, **white carved-stone ruins** (pillars, broken slabs, a portal arch with a glowing disc and gold runes), floating rocks and small tree-topped islets in a deep blue sky with fluffy clouds, warm sun and saturated colour, **glowing golden butterflies** drifting around the player. Waterfalls spilling off islands are planned for T29/T30.
Implemented by `Editor/Scenery.cs` (islands + sky debris), `Scripts/World/GoldenButterflies.cs`, `FloatBob.cs`, the Atmosphere prefab (more cloud cards, warmer sun, stronger bloom and saturation). Shots: `docs/shots/art_pass_*.png`.
Not yet: real modelled trees / ruins (T10 kit), volumetric god rays, waterfall, wind-moved foliage.
