# Water checks

Run with Mono installed:

```sh
python3 Tools/WaterChecks/run.py
```

The runner compiles production water simulation, surface heights, block metadata,
section serialization/deserialization, world adapter and block RPC methods. Unity
and Mirror storage/transport use small test doubles. Mono 6's compiler requires
lowering the existing BlockMapping switch expression; the branches are unchanged.

Checks cover five-tick scheduling, deduplication, seven horizontal flow levels,
falling water and draining, supported two-source regeneration, shortest downhill
paths and ties, obstacles, unavailable chunks, weighted corner heights, actual
mesh vertices, flow-level RLE, old saves, server deltas, deferred client updates,
and applying a host's player edit once.

These are regression scenarios, not an exhaustive comparison with a running
Minecraft Beta 1.7.3 instance. Unity Editor compilation, Mirror weaving, visual
rendering, and multiplayer transport still need a play-mode check.

## Play-mode check

1. Place water on a flat platform. Check its spreading height and seven-block
   reach, then remove the source and watch the flow drain.
2. Place water above a drop. Check that it falls and spreads at the bottom.
3. Put two sources on opposite sides of a supported empty cell; check that the
   middle regenerates. Check a nearby hole pulls flow toward it.
4. Save/reload while water is flowing. Repeat near a chunk boundary and with a
   second connected player.

This change covers water block flow and its surface mesh. Swimming, entity
currents, flowing texture animation, and water/lava reactions are separate systems
and are not added here. All existing non-air blocks block water displacement.
