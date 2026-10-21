# First-person rendering checks

Run `python Tools/FirstPersonChecks/run.py --mcs /path/to/mcs --mono /path/to/mono`.
The checks compile the production renderer and mesh builder against small Unity doubles.
They cover sprite topology and pixel-center edge UVs, both skin aspect ratios, hand/HUD
camera ordering, equip switching and caching, menu/loading/death visibility, lighting,
and ownership cleanup. They also verify the local-player startup hook and shader link.

Unity Play Mode visual checks (not covered by the doubles):

- Start or load a world: the empty hand should use the inventory preview skin.
- Switch between an empty slot, a solid block, stairs/slabs, a plant, and a tool.
  Check equip lowering/raising, texture orientation, and placement on screen.
- Walk, punch, mine, and place blocks; stand against a wall to check clipping.
- Open inventory, pause, die, and respawn. HUD and fluid overlays must remain in front.
- Check caves/day/night and a network host/client; only the local player gets a hand camera.
- Leave and reload the world; generated meshes/materials and camera objects must disappear.

The view is created at runtime by Player.Start; no prefab or data-asset setup is needed.
`Resources/FirstPersonMaterial` retains its shader in builds. Layer 7 (`FirstPerson`)
is reserved for the held model. Geometry is cached per item and released with the player.
