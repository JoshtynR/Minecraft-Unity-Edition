# Asset moves and link audit

```bash
python3 Tools/AssetLinkChecks/run.py --baseline a487e7749f1ce98e670256e32ebbc2bfc04d58c0
```

Read-only check for duplicate GUIDs, missing required Resources keys, literal
material loads, and references to project GUIDs lost since the given revision.
The baseline is the last entity-catalogue commit before the folder reorganization;
use another revision for future moves. References supplied by Unity packages
cannot all be resolved from this source checkout and are counted separately.
This does not validate arbitrary Unity subasset fileIDs, shader compatibility,
GUIDs already missing at baseline, or optional/dynamically constructed paths.

Current layout:

- Runtime materials: `Assets/Resources/Materials`, loaded with `Materials/…`.
- `InventoryPlayerMaterial` intentionally remains at the Resources root.
- Block, item, entity and HUD data remain under `Assets/Resources`.
- Imported Minecraft images: `Assets/Textures` (block/item/entity/gui subfolders).
- UI widget sprites: `Assets/Resources/UI/Title/widgets.png`.
- Player skin/material: `Assets/Resources/Player`.
- Optional editor-generated images: `Assets/Textures/Generated`.

The move preserved most .meta GUIDs. Two deleted-image references were repaired:
the player material's stale generated-skin MainTex now uses its existing player
skin, and the scene's legacy inventory sprite now references the imported single
inventory sprite. No existing textures or configuration assets were recreated.

The old BlockTextures atlas migration runner still needs the deleted legacy
`Assets/Textures/sprites.png` fixture. That is not a runtime texture dependency;
this repair does not reintroduce the old atlas just to satisfy that legacy test.
The standalone FirstPersonChecks harness also retains an outdated `_Brightness`
assertion against the newer lighting path; it is not claimed green by this repair.

Open Unity to finish verifying imports, package-provided references and the scene's
single-sprite subasset after pulling. The source audit cannot certify those.
