# Runtime resource and rebuild cleanup checks

Run `python Tools/MemoryChecks/run.py --mcs <compiler> --mono <runtime>`.
The check compiles production MeshData, ChunkRenderer, the chunk dispatcher,
and the production player lighting/cleanup methods with small Unity API doubles.
It verifies pooled renderer mesh reuse/destruction, player mesh reuse/destruction,
idempotent transparent uploads, texture/light UV preservation, stale chunk
results, queue deduplication, changes arriving during a rebuild, and reset/disable
behavior. These are ownership and scheduling checks, not GPU memory measurements.

For Unity validation, capture Memory Profiler snapshots after a warmed world,
a repeat exploration route, inventory changes, and returning to/reopening the
world. Native Mesh counts should stabilize; inactive chunk pooling intentionally
retains meshes up to the pool's previous peak. Also check sky/block light changes,
water flow, inventory icons, Steam lobby callbacks, and editor preview windows.

Meshing still visits each visible block and recalculates normals. Neighbor lighting
and block mutations still use the project's existing lighting implementation;
this cleanup does not replace it with Jobs/Burst or alter terrain/tree RNG rules.
