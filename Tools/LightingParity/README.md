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
meshes. Half-slab sampling borrows brightness from above and four sides. The default uses Beta vertex interpolation, including slab corner lighting and original
flat fluid lighting (the water top samples its own cell). World's **Full Smooth
Lighting** remains an optional extension, disabled by default: it uses actual
vertex heights, inset face planes, and per-fragment bilinear interpolation across
all four face corners. MainMat's Smooth Lighting checkbox switches all faces to
flat brightness when disabled.

All loaded chunks, including the renderer-less data halo, receive initial lighting
before geometry is built. Initial transport runs sequentially after seeding, with
serialized live light changes to avoid competing packed-light writes. Populating
an already lit halo invalidates it for re-seeding before tree mesh refreshes.
LightTexBoot only allocates a LUT for legacy materials declaring `_LightTex`;
the current Voxel shader needs no LUT.

UV3 stores a repeated float4 of corner brightness values, ordered to match the
face's full-image UV winding. It adds 16 bytes per vertex to mesh attributes;
there are no extra texture reads or materials. The staging list is released
alongside the rest of MeshData. Inventory/player meshes use the existing light
path when the smooth sampling marker is absent.

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
random neighborhoods, darkness values and emissive minima. Both per-face corner sets and 6,144 positioned vertex colors from the actual
reference render-face methods are compared. This verifies corner placement as
well as brightness. The full-smooth bilinear interior is an intentional extension
of Beta's triangle interpolation. Additional tests cover mixed-opacity/cross-chunk transport,
source removal, initial/dynamic sky columns, daytime/night brightness, slab
heights, inset slab faces, deformed water surfaces, smooth-mode fallback, halo
initialization/sideways transport, invalidation and cancellation. The separate
LightTexBoot harness checks modern-material allocation avoidance and legacy
texture ownership/cleanup.

Regenerate using `python Tools/LightingParity/generate_fixtures.py <reference-checkout>`.
Fixtures are compressed with deterministic gzip metadata; reference Java source
is never added to the Unity repo. Shader compilation, six-direction screenshot comparisons, edits at loaded/unloaded
chunk borders, translucent water rendering and a Unity play test remain needed.
The option finishes smooth sampling for current cube/slab/liquid geometry; it
does not implement weather, the celestial clock, new block models or a different
light propagation scheduler.
