# Mobile controls

Target: Android beta (phones first, tablets should work), later iOS. Landscape only. Desktop keeps working unchanged; the mobile layer is additive. This is a design note for the tasks `tasks/T57..T64`; AGENTS.md section 5.8 stays the desktop contract until T57 lands, then gains a mobile column.

## Principles
- **One-handed-thumb reachable, two thumbs maximum.** Left thumb moves, right thumb looks; a few big buttons sit in the right thumb's arc. No multi-finger gestures needed to finish the game.
- **No crosshair aiming.** Desktop focus is "the thing I look at". On touch the game picks the focus itself: nearest interactable in a cone in front of the player (and the camera), within reach. A single context button shows what it will do.
- **Fewer buttons than keys.** Collapse Q / E / F into context actions where unambiguous (see table), keep manual overrides.
- **Everything the HUD says is bound-label aware**: prompts print the touch button name / icon, not "Press Q".
- Controls auto-show on touch devices (and in the editor with Device Simulator or a toggle), hidden when a keyboard / gamepad is the last used device.

## Layout (v3, landscape, safe-area aware)
No stick and no action buttons. Only **Jump** (bottom right) and **Pause** (top left) are buttons; the hotbar is top right.
- **Look**: drag anywhere (not over a button or the hotbar).
- **Walk**: tap the ground. The player turns on the spot towards the tap, then walks there at walking speed (a ring marks the spot). Gives up when stuck, after 25 s, or on respawn.
- **Act**: tap the circle / diamond marker over a pickup, soil patch, sprout, water source, collector, frozen water or the goal flag. In reach (about 3.5 m) it runs the same checks as the keys (free slot, right seed, water in the can, not frozen, ...); out of reach the player walks up to it first and a second tap acts.
- **Climb**: tap the beanstalk (walks up, grabs, climbs; at the top edge it starts the descent). While climbing a tap above / below the player sets the direction, a tap on the player stops, Jump jumps off.
- **Glide**: with a parachute leaf selected, tap Jump again in the air.
- **Hotbar**: tap selects, double tap on a filled slot drops it.

## Action map (desktop key -> touch)
| Desktop | Touch |
|---------|-------|
| WASD / arrows | Tap the ground |
| Mouse look | Drag |
| Space | Jump button |
| Q (pick up) / drop | Tap the diamond / double tap the slot |
| E (plant, use, thaw, flag) | Tap the circle |
| F (water, refill) | Tap the sprout / water circle |
| E (glide) | Jump again in the air |
| Tab / 1-4 | Tap hotbar slot |
| Esc | Pause button / Android back |

## Auto-focus rule (T58)
Today `PlayerController` / the interaction system raycasts from the camera. For touch: score candidates (interactables, usable targets, waterables) within 3 m: distance, angle to the player's facing (cone 70 degrees), prefer the one the camera centre points at when tied. Hysteresis so the focus does not flicker. The same focus object feeds the prompt and the context button.

## Platform concerns (T63, T64)
- Performance budget: reference device is a 2022 phone at 1080 x 2340 (Galaxy S22 / A53, Pixel 6a class). Also Galaxy S23 and Honor 90 Pro (2700 x 1224). First run picks Medium (810 rendered lines: 0.75 scale at 1080p, 0.66 at 1224p; short shadows, merged decoration), 30 fps battery saver and no postprocessing; weak devices get Low (T63, `QualityTier`). Options > Quality switches Low / Medium / High.
- Build: Android IL2CPP ARM64, minimum API 26 (Vulkan with GLES3 fallback), App Bundle, landscape, `ScreenOrientation.LandscapeAuto`, sleep timeout never, Application target frame rate 60.
- Memory: the imported packs' textures need ASTC compression and mip limits; the generated runtime textures (palms, grass) must be small. Watch the `Read/Write` models (palms) — memory cost.
- Shaders: player build only carries shaders used by material assets (see AGENTS.md); verify `M_Keep_*` list on Android, no `Shader.Find` fallbacks. URP: no depth texture / opaque texture unless needed.
- Save / share: `persistentDataPath` works; seed-code copy / paste uses `GUIUtility.systemCopyBuffer` and a soft keyboard text field (TMP_InputField) for "Input seed"; the title menu needs a native keyboard hookup.
- Audio: interruptions (calls, app pause) pause music and the run; the Exploration clock stops on `OnApplicationPause`.
- Safe area (notches) for the HUD and the touch layer.

## Menus on wide screens (T60)
Touch builds keep the canvas height at 1080 reference units (`UiLayout.MatchHeight`) and use the extra width: the title screen and the pause menu are two columns. Desktop keeps the centred single stack.
