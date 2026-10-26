# Mobile controls (design for T57-T64, not implemented yet)

Target: Android beta (phones first, tablets should work), later iOS. Landscape only. Desktop keeps working unchanged; the mobile layer is additive. This is a design note for the tasks `tasks/T57..T64`; AGENTS.md section 5.8 stays the desktop contract until T57 lands, then gains a mobile column.

## Principles
- **One-handed-thumb reachable, two thumbs maximum.** Left thumb moves, right thumb looks; a few big buttons sit in the right thumb's arc. No multi-finger gestures needed to finish the game.
- **No crosshair aiming.** Desktop focus is "the thing I look at". On touch the game picks the focus itself: nearest interactable in a cone in front of the player (and the camera), within reach. A single context button shows what it will do.
- **Fewer buttons than keys.** Collapse Q / E / F into context actions where unambiguous (see table), keep manual overrides.
- **Everything the HUD says is bound-label aware**: prompts print the touch button name / icon, not "Press Q".
- Controls auto-show on touch devices (and in the editor with Device Simulator or a toggle), hidden when a keyboard / gamepad is the last used device.

## Layout (landscape, safe-area aware)
```
+--------------------------------------------------------------+
| [II pause]        time / goal hint                 [hotbar 4]|
|                                                              |
|                                         look: drag anywhere  |
|                                          on the right half   |
|                                                              |
|  (  floating                     [Jump]   [Context]          |
|   left stick  )                  [Water]  [Drop]   [Run]     |
+--------------------------------------------------------------+
```
- **Move**: floating virtual stick, left half of the screen, appears where the thumb lands (fixed-position fallback in options). Dead zone 0.15; pushing past ~0.85 = run, so there is no Shift button. Walk below that.
- **Look**: drag on the right half (not over a button). Sensitivity slider in options; separate invert-Y toggle. Touch delta uses the same smoothing as mouse look but scaled by screen DPI, not pixels.
- **Jump**: big button, right thumb. While climbing it jumps off the vine (same as Space).
- **Context button** (replaces Q-pickup and E-use): label/icon follows the current focus: Pick up seed, Plant, Raise flag, Thaw, Glide. If nothing is focused and the selected slot has an item it becomes Drop (long-press = drop always).
- **Water button** (F): small, only visible when it has a use (a sprout in 3 m, or a water source / collector in reach, or the can is dry near a source). Shows the charge droplets.
- **Hotbar**: four slots top right (built that way since T60; desktop keeps the bottom-centre bar, and on touch the selected item name sits under the row), tap a slot to select (replaces Tab); tap the selected slot again to drop it. Plus nothing for Shift+Tab.
- **Climbing**: the left stick's vertical axis is climb up/down (same as W / S); the context button shows "Grab" when inside a climbable volume (auto-grab by pushing up still works).
- **Pause**: small button top left; the pause menu is the same panel with bigger hit targets. **Restart**: inside the pause menu only (no hold-R on touch).
- Minimum touch target 48 dp (about 9 mm), hit area larger than the drawn icon, 16 dp gap. Opacity 55 percent idle, 90 percent while pressed. Layout scale + left-handed mirror in options.

## Action map (desktop key -> touch)
| Desktop | Touch |
|---------|-------|
| WASD / arrows | Left floating stick |
| Shift (run) | Stick pushed to the edge |
| Mouse look | Right-half drag |
| Space | Jump button |
| Q (pick up / drop) | Context button (pick up) / long-press or tap selected slot (drop) |
| E (plant, use, thaw, glide, flag) | Context button |
| F (water, refill) | Water button (contextual) |
| Tab / 1-4 | Tap hotbar slot |
| Esc | Pause button / Android back |
| hold R | Pause menu: Restart |

## Auto-focus rule (T58)
Today `PlayerController` / the interaction system raycasts from the camera. For touch: score candidates (interactables, usable targets, waterables) within 3 m: distance, angle to the player's facing (cone 70 degrees), prefer the one the camera centre points at when tied. Hysteresis so the focus does not flicker. The same focus object feeds the prompt and the context button.

## Platform concerns (T63, T64)
- Performance budget: 30 fps minimum, 60 target on a mid-range Android (Snapdragon 7-series class). Grass tufts, vines, decals, tree density, shadows, post-processing (bloom), water shader and transparent Lit overdraw need a Mobile quality tier. Render scale 0.75-0.85 option.
- Build: Android IL2CPP ARM64, minimum API 26 (Vulkan with GLES3 fallback), App Bundle, landscape, `ScreenOrientation.LandscapeAuto`, sleep timeout never, Application target frame rate 60.
- Memory: the imported packs' textures need ASTC compression and mip limits; the generated runtime textures (palms, grass) must be small. Watch the `Read/Write` models (palms) — memory cost.
- Shaders: player build only carries shaders used by material assets (see AGENTS.md); verify `M_Keep_*` list on Android, no `Shader.Find` fallbacks. URP: no depth texture / opaque texture unless needed.
- Save / share: `persistentDataPath` works; seed-code copy / paste uses `GUIUtility.systemCopyBuffer` and a soft keyboard text field (TMP_InputField) for "Input seed"; the title menu needs a native keyboard hookup.
- Audio: interruptions (calls, app pause) pause music and the run; the Exploration clock stops on `OnApplicationPause`.
- Safe area (notches) for the HUD and the touch layer.

## Menus on wide screens (T60)
Touch builds keep the canvas height at 1080 reference units (`UiLayout.MatchHeight`) and use the extra width: the title screen and the pause menu are two columns. Desktop keeps the centred single stack.
