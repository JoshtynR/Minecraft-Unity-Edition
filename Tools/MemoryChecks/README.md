# Runtime resource and rebuild cleanup checks

Run `python Tools/MemoryChecks/run.py --mcs <compiler> --mono <runtime>`.
The check compiles production MeshData, ChunkRenderer, the chunk dispatcher,
and the production player lighting/cleanup methods with small Unity API doubles.
It verifies GPU-only chunk mesh replacement/destruction, player mesh reuse/destruction,
idempotent transparent uploads, texture/light UV preservation, stale chunk
results, queue deduplication, changes arriving during a rebuild, and reset/disable
behavior. These are ownership and scheduling checks, not GPU memory measurements.

For Unity validation, capture Memory Profiler snapshots after a warmed world,
a repeat exploration route, inventory changes, and returning to/reopening the
world. Native Mesh counts should stabilize; pooled chunk objects do not retain
meshes or ChunkData after unloading. Also check sky/block light changes,
water flow, inventory icons, Steam lobby callbacks, and editor preview windows.

Mesh merging, lighting UV packing, normals and bounds are prepared on workers;
uploads reuse these results without Unity normal/bounds recalculation. The checks
cover submesh winding, negative-coordinate bounds, empty meshes and repeated upload.
Neighbor lighting
and block mutations still use the project's existing lighting implementation;
this cleanup does not replace it with Jobs/Burst or alter terrain/tree RNG rules.
