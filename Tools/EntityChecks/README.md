# Entity catalogue

Open **Minecraft → Entities → Select Entity Data**. The searchable custom
Inspector has Mobs, Vehicles and Chests tabs. The committed runtime asset is
`Assets/Resources/Entity Data.asset`; current gameplay values are the defaults.

## Settings

- All eight mobs: max health, walking speed in blocks per 20 Hz simulation tick,
  relative natural-spawn weight, and the maximum successful spawns per eligible
  chunk/pass (the spawn pack cap). Placement, biome/light checks and global
  category caps still apply. Weight zero disables natural spawning only.
- Mob skins edit the existing Resources materials, with Undo; sheep fur and
  skeleton bow materials are also exposed. Open Material selects the existing
  asset for shader/tint options. Model geometry/UV layouts remain unchanged.
- Boat: maximum horizontal speed, thrust, turning rate, buoyancy, gravity and
  horizontal water drag (retention per 20 Hz tick). Vehicle speeds are in
  blocks/second. Cart: max speed, slope acceleration and powered-rail acceleration.
- Boat/cart skin fields edit existing PlayerHudAssets references in place.
- Chests: normalized lid progress per second (2 = half-second opening), single
  and double-half skins, container GUI image, and access to the chest material.

Changes take effect on the next Play Mode session or build launch. Editing is
disabled while playing. Runtime startup loads validated copies, including with
domain reload disabled; it does not mutate the data asset. Missing catalogues or
entries retain the built-in defaults. Add Missing Entries preserves edits.

Validation limits: health 1–1024; mob speed 0–1 block/tick; spawn weight 0–1000;
pack cap 1–12; vehicle max speed 0.1–20 blocks/s; thrust/slope/powered acceleration
0–50; boat turn 0–360 degrees/s, buoyancy 0–100, gravity 0–50, water drag 0–1;
chest lid speed 0.1–20. Nonfinite floats fall back to defaults. Lower health caps
clamp existing saved mobs on authority restoration rather than deleting them.

This is tuning for existing entities, not a custom model/AI authoring framework.
Collision dimensions, drops, recipes, brain type, global caps, chest slot counts,
vehicle storage limits and entity IDs are intentionally not editable here.
Use the same configuration in host/client builds; settings are not a networked
server-settings protocol. Defaults preserve the current Beta spawn rolls,
including the reserved, unimplemented slime weight.

## Checks

```bash
python3 Tools/EntityChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
python3 Tools/MobChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
python3 Tools/VehicleChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
```

EntityChecks compiles the production asset, custom editor, mob definitions and
vehicle physics against Unity/editor doubles. It checks catalogue creation,
preserved edits, runtime application, bounds/nonfinite inputs, resetting defaults,
actual configured boat movement, zero spawn weights and duplicate handling.
MobChecks covers saved-health clamping and weighted selection. Editor GUI Undo,
Inspector rendering, actual asset imports, and runtime initialization timing
still need verification in Unity; these are not a Unity build certification.
