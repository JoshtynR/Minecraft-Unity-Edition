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
- Right-click a rail with a minecart; boats can be placed above source water or
  a targeted block with sufficient free space (lava placement is rejected).
- Right-click the hull to ride. One player can occupy each vehicle.
- Boats: W/S and A/D provide forward/strafe rider input relative to the camera.
  The hull turns toward actual travel, not directly with A/D. Minecarts: W/S give a starting push in the
  camera-relative direction along the track; momentum, slopes and rail state
  then determine movement. Holding forward is not an engine.
- Left Shift dismounts when a nearby loaded, unobstructed floor or water surface exists.
  If the shore is unavailable, the rider stays aboard rather than spawning
  inside a block or into unloaded terrain.
- Attack a boat to build hit damage (bare hands need several quick hits; weapons
  hit harder). A broken boat drops exactly one boat item and releases its rider.
  Wall impacts never break it. Minecarts use the same damage threshold, rocking
  and damage decay; destroying an occupied cart releases its rider and drops
  exactly one minecart.

No prefab or scene registration is needed. Recipes and item entries already
exist; hulls are generated at runtime under the dimension renderer root.

## Integration and limits

- Fixed 20 Hz boat simulation in source tick order; minecarts retain five collision substeps. Presentation
  interpolates separately. Solo actions execute directly on the host.
- Remote clients receive dimension-tagged state snapshots at 10 Hz; mounting,
  placement, control and dismount requests are checked by the authority.
- Rail states 0–9 provide straight, sloped and diagonal corner paths. Powered rails use
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
  native meshes/materials are disposed with their visual.
- This is source-guided boat movement, not a complete 1:1 entity-engine port. Mob
  passengers, oars, chest/furnace carts and vehicle
  portal transport are not implemented. Storage is capped at 128 per dimension.
- These checks do not exercise Mirror weaving, actual transport, GPU rendering,
  Unity scene timing, or full on-disk save/load. Test those in Play Mode and a
  standalone build, including server/client riding and repeated dimension trips.

## Beta boat pass

Compared against EntityBoat, Entity.updateRidden/moveFlying, EntityLiving movement,
World.isAABBInMaterial and ModelBiped in `jacobo-mc/mc_b1.7.3_release`.

- Hull radius .75 and height .6; five whole-AABB water slices shifted down .125,
  using metadata/8 rather than the separate eye-immersion rule.
- Partial-submersion lift .04*(2*fraction-1) blocks/tick; full immersion halves
  downward motion then adds .007. Horizontal cap .4 blocks/tick per axis,
  ground halving, and post-collision .99 horizontal/.95 vertical damping.
- Camera-relative rider input approximates the reset-per-tick rider's .02
  movement, .98 input damping, .91 air/.8 water friction and 20% transfer.
  This does not run the full rider EntityLiving collision/current/ladder path.
- Heading follows displacement with a maximum 20-degree turn per tick. Boats
  exchange separation impulses, clear thin snow at hull corners, and leave
  source-pattern side/stern splash particles above .15 blocks/tick.
- Hit rocking/damage decay uses the source tick rate and >40 break threshold.
  Damage is included in live snapshots but omitted from saved state.
- Deliberate exceptions: no collision destruction; player destruction returns
  one boat instead of three planks/two sticks. Safe Shift dismount is retained,
  including an unobstructed water-surface exit. Lava placement remains rejected.
- Entity Data defaults are updated, with the same configurable tuning fields.
  Max speed is per-axis; turn speed limits travel-following yaw; buoyancy controls
  full-immersion rise, while gravity controls partial-immersion lift.
- `BetaMotionPose.Seated()` is a shared, mount-agnostic pose. `Player.IsMounted`
  currently queries vehicle ownership for both boats and carts; future pig rides
  can extend that state query and use the same animator. This does not add pig
  riding yet. Player feet/eye coordinates use lower classic boat/cart seats.

Focused tests include 2,000 deterministic source-formula cases, water-edge sampling,
collisions without destruction, damage/drop accounting, rider release, water exits,
shared pose transitions and actual custom splash emission. Unity transforms,
rendering and transport are mocked; verify seat alignment, input feel, rocking and
remote spectators in Play Mode/builds. Entity push ordering, network interpolation,
water currents affecting rider motion, entity damage from environment and vehicle
portal transport are not full source parity. Particle work is bounded at 40 per
wake tick and pooled by the existing renderer; vehicles remain capped at 128.

## Regular minecart fixes

- Corner projection uses the Beta exit-to-exit chord and an analytic projection.
  Rail redirection preserves horizontal speed instead of repeatedly multiplying
  it by the angle between consecutive track pieces.
- Default slope acceleration is 3.125 blocks/second squared (0.0078125
  blocks/tick added each tick); powered acceleration is 24 (0.06 blocks/tick
  added each tick). Both the tuning fallback and shipped Entity Data are updated.
- Unpowered rails stop speed below 0.6 blocks/second; powered straight rails
  start stopped carts away from a solid end bumper. Powered rails in the open
  do not arbitrarily launch stationary carts. Existing activation bits are used;
  circuit power propagation and detector output are still not implemented.
- Derailed carts have a per-axis speed cap, strong ground friction and airborne
  damping. Terrain sweeps and rail-support exclusions remain in use.
- Cart pairs exchange momentum once per pair per simulation tick, with loaded
  area, height, distance and track-direction checks. This is a bounded vehicle
  collision approximation, not a full Beta entity collision port; player/mob
  pushing and automatic mob pickup are not included.
- Minecart hits accumulate damage, rock the hull, and decay once per tick.
  Occupied carts can be destroyed and release their rider; duplicate attacks
  after removal cannot create another item. Disk snapshots omit transient hits.
- Hull visuals tilt to the current slope in either facing direction. The shared
  sitting pose remains in use for cart and boat riders.

Regression cases cover powered launches on both axes and both X directions,
open-station idling, exact low-speed braking, corner momentum, ground stopping,
cart momentum transfer, occupied destruction, visual pitch and all four downhill
slope directions. The existing boat, interaction, chunk, model, save-hook and
riding-pose checks also pass. Unity rendering, Mirror weaving and real multiplayer
transport still require an editor/build smoke test. This retains the five-substep
voxel simulation and camera-relative rider starting nudge; it is not full 1:1
Beta minecart tick order. Chest/furnace carts remain unimplemented.

## Vehicle lighting

Boats and minecarts use the existing `Voxel/Mob` lightmap shader with `_Unlit`
disabled, including the fallback material path. A reusable material property
block supplies each hull's sky/block light sample at its rendered center height
(position + 0.3 blocks). Sampling updates immediately on crossing a cell and at
least every 0.2 seconds while stationary, so torch placement/removal also updates
parked vehicles. Unchanged samples do not upload another property block; missing
chunk data retains the last valid sample. Day/night and dimension brightness
continue to come from the shared world lightmap, and mesh normals provide the
existing directional entity shading. This is one light sample per hull, matching
the other entity visuals, rather than per-vertex terrain AO or Unity scene lights.

The vehicle suite exercises both kinds through their actual Initialize/LateUpdate
methods: initial daylight, stationary torch/darkness updates, moving between light
cells, unchanged-property caching and unavailable-data behavior. Shader/GPU visual
verification still needs Play Mode: compare daylight, night, a torch-lit tunnel,
a dark cave and Nether travel.
