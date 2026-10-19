# Beta 1.7.3 generation parity

The chunk-local pipeline matches the pinned Java reference in **56 cases and 1064 exact stage checks**. The complete population pipeline is active; its independent feature and RNG fixtures are documented in [PopulationParity](../PopulationParity/README.md). Full live-world parity with vanilla's world services is not certified.

## Run the checks

In Unity, use **Tools → Minecraft → Validate Beta 1.7.3 Parity**. This runs the actual production generator classes without creating a scene or changing a world.

Alternatively, install Mono and run from the repository root:

```bash
python Tools/BetaParity/run.py
```

The CLI compiles the production C# sources with overflow checking enabled. It checks all 56 production TerrainGenerator outputs against the cave fixtures using mocked Unity storage, including the air ceiling above Y=127. It repeats those fixtures with a shared generator under parallel startup and seed switching, rejects unsupported world geometry, and checks population deferral, resumption, and idempotence. All double arrays are checked by SHA-256 of their little-endian IEEE754 representations; block arrays use Beta IDs in `(x*16+z)*128+y` order. There are no numeric tolerances. `--output <directory>` exports every stage for element-by-element diagnosis:

```bash
python Tools/BetaParity/run.py --output /tmp/csharp-parity
python Tools/BetaParity/compare.py /tmp/java-parity/fixtures/0_0_0-density.bin /tmp/csharp-parity/0_0_0-density.bin --doubles
```

The cases cover seeds `0`, `1`, `-1`, `12345`, `8675309`, `-1446162294`, and both signed 64-bit extrema, at chunks `(0,0)`, `(1,-1)`, `(-1,1)`, `(-3,-7)`, `(17,29)`, `(784426,0)`, and `(-784426,0)`. The last two exercise the Far Lands boundary.

With `dumpBetaParity` enabled on TerrainGenerator, chunk `(0,0)` also exports the intermediate double arrays alongside the existing block dumps. Disable the toggle to stop these diagnostic files.

The 19 stages are temperature, humidity, biomes, scale noise, depth noise, selector noise, minimum noise, maximum noise, density, scalar tree-count noise, mixed Java RNG operations, seed parsing, coordinate conversion, raw blocks, sand noise, gravel noise, stone-depth noise, surface blocks, and cave blocks.

## Independent Java reference

The oracle is the Beta generator in [emortaldev/Minestom173](https://github.com/emortaldev/Minestom173), pinned to `4c665027560d63e2afa86a5424fc7d9dce974be6`. This is a Java port of the legacy generator, not a claim that the entire modern server reproduces vanilla Beta gameplay.

`reference.py` retains the reference constructor, noise, climate, biome lookup, density, interpolation, surface, and cave method bodies. It removes modern server loading/population interfaces and supplies a small block-array adapter with Beta IDs and height 128. It does not derive reference values from the C# code. It does not use the port's modified population routines as an oracle.

To regenerate with Java 17:

```bash
git clone https://github.com/emortaldev/Minestom173.git /tmp/minestom173
git -C /tmp/minestom173 checkout 4c665027560d63e2afa86a5424fc7d9dce974be6
python Tools/BetaParity/reference.py --reference /tmp/minestom173 --work /tmp/java-parity
```

Compare the generated manifest with `fixtures.tsv` before intentionally replacing it. Java binaries are temporary outputs, not files to copy into `Assets`.

## Audit and fixes

| Component | Result |
| --- | --- |
| JavaRandom | Matches mixed unbounded/bounded ints, signed `nextLong`, floats, and doubles. LCG arithmetic, signed conversion, and rejection overflow explicitly wrap even in checked C# builds. |
| Improved/Octave noise | Bulk 2D/3D arrays match bit-for-bit. Preserve the mixed 2D/3D gradients and cached Y gradients. Fix scalar two-argument sampling to `(x, second, 0)` with all offsets; it is a different path from bulk 2D. |
| Climate and biomes | Region climate arrays and quantized biome lookup match. |
| Density and interpolation | All 425 density samples and all 32768 raw block IDs match. `16/5 == 3` climate stride is intentional. |
| Surface | Preserve Beta's noise/biome index `k+l*16` and block index `(l*16+k)*128+y`. The previous implementation wrote to transposed physical columns. |
| Caves | Child widths use the tunnel RNG; child seeds use the source RNG. The previous implementation seeded child tunnels from the tunnel RNG. Preserve the original one-block cursor offset. |
| Java double-to-int conversion | Saturates like Java, including NaN, before the original floor/decrement logic. This preserves Far Lands behavior. |
| Coordinate boundary | Raw, surface, and caves run in canonical Beta coordinates. Unity's global X reflection is applied once when copying into ChunkData. |
| Seed parsing/lifecycle | Numeric parsing uses invariant signed decimal syntax; other strings use Java's UTF-16 hash. The 64-bit seed now persists in world saves and is sent to joining clients before generation. |
| World geometry | Reject chunks other than 16×16 and worlds below 128 blocks instead of silently truncating Beta terrain. Worlds taller than 128 keep air above the Beta ceiling. |

### Comparison with the previously supplied world

At seed `-1446162294`, chunk `(0,0)`, the raw terrain is unchanged from the supplied raw dump. The supplied region's chunk is marked populated. Comparing unpopulated cave-stage output against that populated chunk is **not a same-stage parity test**, but it corroborates the fixes: mismatching block IDs fell from **6951 to 1343**. Most remaining pairs involve population additions such as dirt/gravel deposits and ores. The region file does not itself encode the world seed, so this comparison assumes it belongs to the seed recorded in the supplied dumps.

## Runtime pipeline and remaining limits

`TerrainGenerator.GenerateChunkData` runs density, surface, and caves, then copies into Unity coordinates. `PopulateChunk` runs the full population pipeline exactly once after its 4×4 raw-data neighborhood is ready. World generation visits all ready owners, including data-only halo chunks, in canonical X/Z order. Population writes remain serial; chunk-local terrain generation runs in parallel using an atomically published set of generators for each seed.

The original noise settings, domain warping, chained block layers, biome objects, experimental density generator, and isolated tree/lake previews have been removed along with their assets and scene objects. The inspector retains only the parity dump toggle. Old tree-completion fields remain solely in the save schema for compatibility; active generation uses one population-completion flag.

- Generation-time immediate fluid updates and exact vanilla light-update timing remain outside the fixture guarantees. See the population suite for the mocked world services and coverage.
- Existing saves do not contain a recoverable Beta world seed. New saves persist it. For an old save, set its `betaWorldSeed` in `world.json` to the original known seed before extending terrain; otherwise the absent field defaults to zero. The old spatial `seedOffset` cannot reconstruct the original long.
- These standalone checks cover the actual C# algorithms under Mono, not Unity scene integration, Mirror-generated serialization, an IL2CPP build, or every possible seed/coordinate. Run the Unity menu check and regenerate test chunks after pulling. Existing saved chunks are not rewritten by the generator fixes.

Existing saves and block/item texture assignments are preserved. The generator does not rewrite existing saved terrain.
