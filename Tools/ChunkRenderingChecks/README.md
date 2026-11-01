# Chunk rendering consolidation

Runtime responsibilities:

- `World_BlockChanges`: paired doors/beds, block effects and rail state updates.
- `World_RenderInvalidation`: one-cell render dependency halo, shared by block
  changes, falling blocks, fluids and lighting. Chunk corners include diagonals.
- `World_ChunkUpdates`: deduplicated runtime rebuild queue and dispatch budget.
- `ChunkMeshPipeline`: shared parallel build and failed-batch staging cleanup.
- `World_MeshPipeline`: shared upload ownership validation and yielding policy.
- `World_WorkLifecycle` / `WorldWorkLifetime`: cancellation ownership, result
  epochs, idle waits and leases covering scheduled/running mesh workers.
- `EntitySpatialIndex<T>`: shared spatial buckets; mob/drop stores retain their
  own validation, identity, save formats and gameplay rules.

Initial generation, runtime edits, tree refreshes and dimension rebuilds retain
their own ordering, but share build/validation/cleanup rules. Generation's gate
and gameplay simulation flags remain distinct: a paused snapshot application
must still be allowed to upload its replacement meshes.

`run.py` exercises actual scheduler, build wrappers, initial chunk publication,
upload validation, worker leases and invalidation against lightweight doubles.
Mob, inventory and rendering tests share `Tools/Shared/UnityVectors.cs` rather
than maintaining different vector math implementations.

These checks do not replace a Unity player/editor run. Mutable block/light data
is not snapshot-isolated by this refactor; section-level rebuilding is also a
separate optimization. The existing fluid visual smoke-quad assertion remains
a known failing test outside these changes.

## Conservative greedy meshing

`World.greedyMeshing` is enabled by default and appears in the World Inspector.
Disable it before entering Play Mode to compare the original unit-face path.
Changing it at runtime applies when sections are rebuilt; it does not invalidate
all existing meshes automatically. The Nether copies this field with the other
World settings when its World is created.

Each rebuilt 16x16x16 render section marks eligible opaque full-cube faces in
`BlockHelper`, then `MeshData.PrepareForRendering` greedily merges rectangular
runs before appending cutout, water and portal geometry. Matching requires the
same texture layer, tint, sky/block levels, AO and AO-off flat-light samples.
Every corner of an eligible face must have identical shader inputs. Gradients,
partial blocks, fluids, special models and grass side overlays stay unmerged.
Faces do not merge across section boundaries, and unexpected topology falls
back to the existing geometry.

Merged UVs span the rectangle's block dimensions. A per-face flag in the existing
`sides.y` channel makes the voxel shaders repeat these coordinates per pixel;
the texture array remains clamped for cropped special-model images. Animation
still resolves the same layer per vertex. Fog now interpolates view position and
calculates distance per pixel so its falloff does not depend on tessellation.
The packed vertex layout remains 56 bytes. Compaction runs in place and preserves
normals, draw-pass offsets, bounds, upload idempotence and staging-buffer cleanup.

This first pass reduces uploaded/rendered vertices and indices. It still emits
and lights individual faces before compacting them, and adds CPU work and bounded
section-local scratch allocations. It is not a claim of faster generation or
higher FPS; profile both settings in the same scene/route on the target hardware.

Validation:

- `Tools/LightingParity/run.py`: production emission, a 16x16 stone plate
  (576 exposed unit quads to 6 merged quads), all six windings, randomized hole/
  texture layouts, surface coverage, lighting/tint barriers, UV dimensions,
  secondary draw passes, malformed-topology fallback, and cleanup.
- `Tools/ChunkRenderingChecks/run.py`: section rebuild/toggle capture, packed
  tiled UVs and flags, scheduler, invalidation and upload ownership.
- `Tools/ShapeChecks/run.py` and `Tools/MemoryChecks/run.py`: existing special
  geometry/collision and mesh lifetime checks with updated math doubles.

Unity smoke testing is still required: inspect a flat platform and terrain at
near/far distances, torch gradients with smooth lighting both enabled and
disabled, biome boundaries, grass side overlays, water/glass, Nether travel,
block placement/removal at section boundaries, and wireframe/Stats counts.
Check for texture stretching, cracks/T-junction artifacts and fog changes; the
standalone suites do not run the Unity shader compiler or GPU.
