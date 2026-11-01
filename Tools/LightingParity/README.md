# Voxel lighting verification

The renderer uses one colored lightmap path. Chunk meshes retain independent
sky and block levels plus vertex occlusion; the shader interpolates those levels
and samples the shared 16x16 lightmap. Smooth lighting can still be disabled by
the material's AO toggle without changing or rebuilding chunk data.

All loaded chunks, including the renderer-less data halo, receive initial light
transport before geometry is built. Transport subtracts destination opacity,
recasts changed sky columns, removes stale sources, and serializes live light
updates so packed-light writes cannot compete.

Run `python Tools/LightingParity/run.py --mcs <compiler> --mono <runtime>`.
The standalone suite requires Python/Pillow, Mono and Java 11+ for the independent
column reference evaluator; the game does not require Java.
The harness compiles production face sampling, special geometry, fluids, biome
coloring, and propagation against Unity storage doubles. It covers all six face
orientations, raw light streams, corner occlusion, emitting blocks, mixed-opacity
and cross-chunk transport, source removal, halo initialization, cancellation,
slabs, stairs, plants, and animated fluid geometry.

Biome colours use the supplied `Assets/Textures/Minecraft/grass.png` and
`foliage.png`, assigned on the world's BlockDataManager. Both are imported as
readable, uncompressed 256x256 maps. Startup snapshots their raw sRGB pixels
into immutable CPU tables and reverses Unity's bottom-up rows. Mesh workers
sample `(1-temperature, 1-temperature*humidity)` using Beta climate in canonical
coordinates. Chunk-owned caches cover grass and oak leaves, invalidate on seed
or palette changes, and are reclaimed with their chunks. No save data changes.
Grass top/side overlays, oak foliage, and tall grass/ferns use the maps; dirt
faces and dry grass stay neutral. Spruce and birch retain their fixed Beta colours.
Other blocks retain their asset tint. World colours replace the old configured
green instead of multiplying it a second time. Cube inventory icons use stable
default colours without needing a world or sampling the player's biome.

The colour rules were checked against `BlockGrass`, `BlockLeaves`,
`BlockTallGrass`, `ColorizerGrass`, and `ColorizerFoliage` in the pinned
`jacobo-mc/mc_b1.7.3_release` reference at
`740c583901e1ff1150e9ef37e37dab5bc0e4f807`. This harness tests the actual uploaded
map pixels, coordinate reflection, all 256 columns across several seeds/chunks,
plant hash overflow against Java evaluations, emitted grass overlay colours,
fixed leaf variants, and cache reuse/invalidation. Texture lookup checks keep
these root-level climate maps out of block/item face assignments.

## Beta lighting audit and regression fixes

Reference reviewed: `Chunk.generateSkylightMap`, `Chunk.relightBlock`,
`MetadataChunkBlock.func_4127_a`, `ChunkCache.getLightValueExt`, `RenderBlocks`,
`Block` light-opacity/emission definitions and `WorldProvider` in the pinned
Beta 1.7.3 repository above.

| Pass | Finding / current behavior |
| --- | --- |
| Initial vertical sky | Subtracts block opacity, keeping initial air columns at their current level; checked against an independent Java reference evaluator. |
| Changed vertical sky | Uses at least one attenuation below the first attenuating block, following Beta's relight rule; Overworld and no-sky cases checked. |
| Sky/block transport | Destination-opacity attenuation and source maxima agree with the reference rule. Sixty deterministic emitter/opaque/water edits plus repeated reseeds match an independently solved full light field. Queue scheduling is our adaptation. |
| New chunk borders | Fixed: newly initialized chunks import sky/block light from clean existing neighbors that have no pending queue. Already-bright borders do not add redundant transport entries. |
| Initial/reseed block light | Fixed: old propagated values are removed and actual emissions reseeded, so a removed source cannot survive a halo rebuild. |
| Population changes | Fixed: generated cell writes invalidate prior halo lighting. This is verified through the actual packed ChunkSection implementation in the fluid integration suite. |
| Mesh invalidation | Fixed: direct column changes refresh neighboring corner samples even when the neighbor's own stored light remains unchanged. Notifications are batched by vertical render section. |
| Partial block sampling | Fixed: both farmland types use the above/four-side maximum, alongside slabs and stairs, as Beta's ChunkCache does. Their inset top no longer samples an opaque zero-light cell directly. |
| Shipped block definitions | Fixed water opacity 1 to 3 and portal opacity 2 to 0. All 126 assigned light definitions now agree with the pinned catalogue; omitted legacy crop aliases use runtime defaults. Visual transparency remains controlled separately. |
| Greedy geometry | Existing flat-light/material/AO merge barriers and winding/coverage checks pass. Merging does not change the transported light field. |
| Palette and AO | The current colored 1.8-style lightmap and separate vertex occlusion remain the project's renderer. Beta 1.7.3 averages scalar brightness in its smooth renderer, so this audit does not certify exact Beta 1.7.3 pixels or AO. |

`BetaColumnReference.java` independently evaluates 256 initial/changed, sky/no-sky
columns. `TransportAuditCheck.cs` exercises the production transport and face
sampler, streaming imports, stale-source cleanup, direct-border invalidation,
farmland and reference field convergence. The runner also audits the assigned
opacity/emission values in the actual shipped Block Data asset.

Validation also includes the existing 18,432 colored-lightmap reference samples,
lightmap lifecycle, greedy mesh coverage, chunk upload/invalidation and the
3,394 fluid/block/save integration checks. The complete WaterChecks runner still
fails its separate portal-screen Surface-slot visual assertion; that visual
executable's source inputs are unchanged by this lighting patch. Updated storage
and math doubles allow its production integration phase to compile again.

Reload the world after updating: stored block data is retained and lighting is
reconstructed on load, so previously explored terrain benefits too. In Unity,
inspect cave entrances, torch removal/overlap, water depth, farmland, section and
chunk edges, day/night, entities and Nether travel. These suites mock Unity and
GPU execution; they cannot establish that an unspecified visual artifact is gone.
The transport/renderer timing adaptation and lack of immutable mesh-read snapshots
also remain limitations of the current engine, rather than claims of full Beta
lighting-engine parity.

## Foliage AO

Leaf cube faces already received world AO, but leaf neighbors previously returned
an occlusion value of 1, so they did not contribute to canopy corner shading.
Oak, spruce and birch now contribute soft occlusion (0.6 versus 0.2 for solid
opaque cubes). Isolated exposed leaves keep unoccluded corners; overlapping
foliage shades leaf and terrain corners through the existing smooth-light toggle
and AO intensity control. Foliage remains in the cutout submesh.

Soft leaf neighbors still allow the diagonal sky/block sample. Only two hard
opaque neighbors select the existing blocked-diagonal fallback. This is a visual
foliage shading enhancement to the current hybrid renderer; leaf opacity, light
transport, transparency, tint, decay metadata and greedy-merge eligibility are
unchanged. Reload the world to rebuild existing tree meshes after updating.

The face suite checks all three leaf species in all six orientations, mixed
canopies, isolated leaves, diagonal lighting, stronger solid-block occlusion,
glass exclusion, AO-off face-light inputs, and production cutout routing while
greedy meshing is enabled. In-engine canopy appearance still needs a visual check.
