# Passive animal checks

The shared mob entity system holds cows, pigs, sheep, chickens and hostiles. Old
cow-only saves default to adult cows. New foods append stable IDs 1115–1119;
existing block/item assignments are not rewritten.

Implemented additions: species-specific skins and model boxes, mixed natural
herds, wheat breeding (seeds for chickens), 30-second love windows, five-minute
parent cooldowns, 20-minute baby growth, baby scaling and collision bounds,
adult meat/cooked meat drops, egg laying, chicken slow falling/fall immunity,
sheep shearing, dye colors, and grass-based fleece regrowth. New food entries
support eating and beef/chicken smelting. Rotten flesh is registered for the
later zombie batch; food-poisoning effects are not implemented here.

Remaining: pig saddle/riding. The entity architecture, bounded navigation,
food-following and visual effects are documented in ../MobChecks/README.md.
Visual/transport behavior needs Unity testing.

Run `python Tools/CreativeChecks/run.py --mcs mcs --mono mono`.
MobCheck.cs covers the production mob state/store, save copying and serialization,
identity reuse, bounded population, damage/invulnerability, leather rolls, bucket
transfers, and the classic mob model's face layout, winding and UV ranges.
Unity UI and engine primitives are doubled in this suite.

Unity play checks (the runtime integration also needs a Unity/Mirror build):
1. Load an existing world. Use Minecraft > Entities > Spawn Mob Near Player while
   looking toward clear ground, or explore nearby grass in daylight for herds.
2. Check the face, horns, udder, four walking legs, lighting, and block occlusion.
3. Punch a mob: it should flash, flee, jump obstacles, and eventually die.
   Cows have 10 health, drop 0-2 leather and 1-3 beef under the requested hybrid rules.
4. Right-click with an empty bucket. The selected bucket becomes milk, with no
   changes when the held item is unrelated or a custom stacked-bucket inventory is full.
5. Verify that hitting/placing does not mine blocks behind the targeted mob.
6. Save/reload and stream away/back. Mob identity, health and position should persist;
   unloaded mobs should stop simulating and their visual hierarchies should be released.
7. Join a host with another client. Check existing mobs on join, movement, hits,
   death/drop uniqueness and milking inventory updates; attacking through a wall
   or beyond reach must be rejected.
8. Check swimming, falls, fire and lava, then exit/reload the world and inspect
   meshes/materials for cleanup.

9. Use the corresponding spawn menus for pigs, sheep, and chickens. Feed two
   adults wheat (chickens: seeds), check a baby appears and no further food is
   consumed during the parent cooldown. Save/reload before and after a birth.
10. Shear/dye sheep, check matching wool and fleece visibility. Observe grass
    changing to dirt when a sheared sheep regrows fleece. Wait for chicken eggs;
    drop a chicken from height and compare its slow descent with other animals.

The automated suite covers state/save/model/UV/inventory rules, not a running
Unity renderer, the world simulation loop, or live Mirror transport.
