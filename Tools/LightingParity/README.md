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
