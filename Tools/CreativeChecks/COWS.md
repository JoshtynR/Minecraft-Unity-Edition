# Cow checks

Run `python Tools/CreativeChecks/run.py --mcs mcs --mono mono`.
CowCheck.cs covers the production cow state/store, save copying and serialization,
identity reuse, bounded population, damage/invulnerability, leather rolls, bucket
transfers, and the classic cow model's face layout, winding and UV ranges.
Unity UI and engine primitives are doubled in this suite.

Unity play checks (the runtime integration also needs a Unity/Mirror build):
1. Load an existing world. Use Minecraft > Entities > Spawn Cow Near Player while
   looking toward clear ground, or explore nearby grass in daylight for herds.
2. Check the face, horns, udder, four walking legs, lighting, and block occlusion.
3. Punch a cow: it should flash, flee, jump obstacles, and eventually die.
   Beta cows have 10 health and drop 0-2 leather; they do not drop beef.
4. Right-click with an empty bucket. The selected bucket becomes milk, with no
   changes when the held item is unrelated or a custom stacked-bucket inventory is full.
5. Verify that hitting/placing does not mine blocks behind the targeted cow.
6. Save/reload and stream away/back. Cow identity, health and position should persist;
   unloaded cows should stop simulating and their visual hierarchies should be released.
7. Join a host with another client. Check existing cows on join, movement, hits,
   death/drop uniqueness and milking inventory updates; attacking through a wall
   or beyond reach must be rejected.
8. Check swimming, falls, fire and lava, then exit/reload the world and inspect
   meshes/materials for cleanup.

The initial cow commit was prepared while the coding runtime was unavailable.
Static source/asset checks do not replace the compilation and play checks above.
