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


## Transparent water rendering

Chunk submeshes are now solid (0), cutout leaves/glass (1), and blended water (2).
The Chunk prefab binds Water_Material to slot 2. Its Voxel/Water shader samples
the same face texture array and shares Beta lighting code with Voxel, but draws
in the Transparent queue with SrcAlpha/OneMinusSrcAlpha blending, no depth writes,
and both face orientations visible. Water Opacity defaults to 0.6 and multiplies
image alpha and block tint alpha. It does not change water's lighting opacity (3).
Terrain under water remains in the solid pass; internal water faces are culled.

LightingParity checks production routing (water, cutout blocks, adjacent water,
and submerged terrain). MemoryChecks verifies three-way vertex/light merging,
water index offsets, repeat uploads, and removing water on a dry mesh rebuild.
Shader compilation and above/below-water views need verification in Unity. The
rendering uses Unity's normal transparent renderer sorting; intersecting or
layered waterfalls can still require finer face sorting. Underwater fog,
refraction, and water texture animation are not included in this change.
