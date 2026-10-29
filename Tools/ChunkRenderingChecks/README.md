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
