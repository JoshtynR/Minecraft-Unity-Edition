# Survival HUD

World's GameUI has PlayerSurvivalHUD next to the existing Hotbar. It builds ten
health hearts, ten food icons, a 182x5 XP bar and a level label once, inherits the
existing CanvasScaler, follows the hotbar offset, and reads only
GameManager.localPlayer. Background icons remain visible at zero; odd health/food
values show a half icon. Food fills from right to left. XP crops both geometry
and UVs to whole pixels, and level zero has no label. Graphics ignore raycasts.
The generated UI is destroyed by its owner, and unchanged values reuse it.

The user-owned sprite sheet is expected at
`Assets/Textures/Minecraft/gui/icons.png`. It is not present in the remote
repository when this change was written. The inventory background is separately
bound from `Assets/Textures/Minecraft/gui/container/inventory.png` into the same
Resources asset; importing that sheet or reloading the editor binds it automatically.
PlayerHudAssetSetup binds the icons into
`Assets/Resources/PlayerHudAssets.asset` on editor load/import and saves only that
asset. It sets point filtering, no mipmaps, uncompressed pixels and no NPOT
rescaling. Resources preserves the texture/font references in builds. The actual
PNG and its generated .meta, plus the configured PlayerHudAssets.asset, must be
committed for other checkouts to have the artwork. No copied/generated Minecraft
artwork is included. A missing sheet leaves the existing hotbar usable and logs
one actionable warning; importing it allows the HUD to appear.

Sampling uses the classic 256x256 layout (integer multiples support HD sheets):
heart/food background (16,0)/(16,27), full (52,0)/(52,27), half (61,0)/(61,27),
XP background (0,64), XP fill (0,69). Source rectangles use top-left coordinates;
Unity UVs are converted to bottom-left. Reference layout:
https://github.com/Marcelektro/MCP-919/blob/main/src/minecraft/net/minecraft/client/gui/GuiIngame.java
The user's actual sheet has not been inspected, so its layout still needs a
Unity visual check.

Player now has Mirror SyncVars health/hunger (default 20), experienceLevel (0)
and experienceProgress (0). Server gameplay code can call SetHealth(points),
SetHunger(points), or SetExperience(level, normalizedProgress). Points clamp to
0..20; levels clamp to nonnegative; progress clamps to 0..1. This change provides
HUD state and display, not damage/death, food drain/eating, XP reward/leveling,
or persistence. Existing saves do not contain these new fields.

Run `python Tools/HudChecks/run.py --mcs <compiler> --mono <runtime>`.
The harness compiles production HUD/assets/value code against Unity UI doubles;
it checks every point count, half icons, XP bounds/cropping, label updates,
hotbar following, local-player changes, visibility, input and owned UI cleanup.
This does not run Unity Editor compilation, texture importing, TMP rendering or
Mirror weaving. In Unity, confirm the sheet binds, start a host, inspect the
spawned player's values at 19 health/5 hunger/level 7/progress 0.5, change GUI
scale, and reconnect. Test server setters with a remote client before gameplay
systems rely on replication.
