# Beta 1.7.3 lighting changes and verification

The default Voxel shader now uses grayscale Beta brightness rather than a later
RGB lightmap approximation: `max(blockLight, skyLight - skylightSubtracted)`,
lookup curve with 0.05 ambient, and face multipliers 1 / 0.8 / 0.6 / 0.5.
Brightness is evaluated before vertex interpolation. In a Unity Linear project,
texture multiplication is performed in gamma space then converted back, matching
Beta's fixed-function color operation. Keep the brightness setting at zero and
MainMat's default face multipliers for the reference behavior.

Full cube smooth lighting averages four brightness samples from the face's
outside plane, with Beta's material-based diagonal fallback. It replaces the
front/back weighted level sampling and separate multiplicative voxel AO. UV2
now stores smooth brightness, flat brightness and a Beta sample marker. Raw
sky/block values in UV1 remain available to debug views and inventory/player
meshes. Half-slab sampling borrows brightness from above and four sides. Liquids
and partial shapes use flat sampling in this implementation.

World's `Beta Skylight Subtracted` is manual (0 daytime, 11 clear night).
Changing it queues chunk remeshes; this does not implement the celestial clock,
weather, fog, or sky renderer. F3's existing time controls adjust this value.
Source light propagation now subtracts destination opacity; water/ice opacity
is 3, leaves 1, glass 0, and full opaque blocks/slabs are represented by 15
(equivalent to Beta's 255 for light transport). Lava emits 15; ice emits zero.
The previously absent glass definition is restored. Block opacity changes
recast sky columns; changed-column air attenuation and stale queued source
removal are handled. The existing incremental queues and chunk streaming
scheduler remain: their transient update order is not claimed to match Java.
Existing saves recalculate their light maps when loaded; no new world needed.

Reference: https://github.com/jacobo-mc/mc_b1.7.3_release at
`740c583901e1ff1150e9ef37e37dab5bc0e4f807`, reconstructed Beta client sources:
`WorldProvider.java`, `RenderBlocks.java`, `MetadataChunkBlock.java`,
`Chunk.java`, `ChunkCache.java`, `Block.java` and material classes.

Run `python Tools/LightingParity/run.py --mcs <compiler> --mono <runtime>`.
The harness compiles production brightness math, face sampling, block geometry
and propagation against Unity storage doubles. The fixture generator extracts
unchanged reference Java brightness-table and full-cube AO methods into a
scratch runner. It checks 16 brightness values and 1,536 face cases over 256
random neighborhoods, darkness values and emissive minima. Per-face corner
color sets are compared sorted: this is not proof of matching vertex order or
quad triangulation. Additional tests cover mixed-opacity/cross-chunk transport,
source removal, initial/dynamic sky columns and daytime/night brightness.

Regenerate using `python Tools/LightingParity/generate_fixtures.py <reference-checkout>`.
Fixtures are compressed with deterministic gzip metadata; reference Java source
is never added to the Unity repo. Shader compilation, six-direction screenshot
comparisons, edits at loaded/unloaded chunk borders, translucent water rendering,
partial-shape AO, and a Unity play test are still required for full visual parity.
