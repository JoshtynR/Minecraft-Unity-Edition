# Boats and minecarts

Run from the repository root:

```bash
python3 Tools/VehicleChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
```

Compiles the actual vehicle physics, world simulation, player interaction,
voxel collision and visual scripts against lightweight Unity/network doubles.
Checks rail endpoints/projection for all ten states, traversal across straight
and curved pieces, slope supports, ordinary walls, unloaded chunk boundaries,
buoyancy/speed bounds, riding, authoritative item accounting, request replay,
break drops, and unoccupied save snapshots. Integration hooks are also checked.

## In-game use

- Craft or select the existing **Minecart** and **Boat** items.
- Right-click a rail with a minecart, or source water with a boat, to place it.
- Right-click the hull to ride. One player can occupy each vehicle.
- Boats: W/S thrust, A/D steering. Minecarts: W/S give a starting push in the
  camera-relative direction along the track; momentum, slopes and rail state
  then determine movement. Holding forward is not an engine.
- Left Shift dismounts when a nearby loaded, unobstructed standing spot exists.
  If the shore is unavailable, the rider stays aboard rather than spawning
  inside a block or into unloaded terrain.
- Left-click an empty hull to recover its item. Occupied vehicles are protected.

No prefab or scene registration is needed. Recipes and item entries already
exist; hulls are generated at runtime under the dimension renderer root.

## Integration and limits

- Fixed 20 Hz authority simulation with five collision substeps; presentation
  interpolates separately. Solo actions execute directly on the host.
- Remote clients receive dimension-tagged state snapshots at 10 Hz; mounting,
  placement, control and dismount requests are checked by the authority.
- Rail states 0–9 provide straight, sloped and curved paths. Powered rails use
  their existing activation bit; this does not add a redstone circuit simulator.
- The existing block sweep resolves terrain collisions. On track, only blocks
  directly supporting rails are excluded to accommodate the hull on slopes.
- Missing rendered chunks suspend a vehicle. A mounted player's position feeds
  the existing player-centred chunk streamer; unoccupied vehicles don't force
  extra chunks to stay loaded or generate terrain themselves.
- Vehicles persist per dimension through world metadata. Rider references and
  control input are transient and are not restored from disk. Old saves without
  a vehicle array load with none.
- Classic 64x32 hull models now use `entity/boat/boat.png` and
  `entity/cart/cart.png`, referenced by `PlayerHudAssets` for player builds.
  Meshes use the classic box UV layout, rotated floors and cart interior liner;
  native meshes/materials are disposed with their visual. Physics is unchanged.
- This is an initial gameplay implementation, not a 1:1 vehicle port. Vehicle/entity
  bumping, mob passengers, damage stages, oars, chest/furnace carts and vehicle
  portal transport are not implemented. Storage is capped at 128 per dimension.
- These checks do not exercise Mirror weaving, actual transport, GPU rendering,
  Unity scene timing, or full on-disk save/load. Test those in Play Mode and a
  standalone build, including server/client riding and repeated dimension trips.
