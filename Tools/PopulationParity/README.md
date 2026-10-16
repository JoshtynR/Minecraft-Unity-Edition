# Beta population coverage and validation boundary

The default `Use Complete Beta Population` option replaces the isolated tree
preview with this shared JavaRandom stream, in Beta 1.7.3 order:

1. Water/lava lakes; eight dungeon attempts.
2. Clay, dirt and gravel veins; coal, iron, gold, redstone, diamond and lapis.
3. Biome tree counts, selection and generation using the same remaining stream.
4. Flowers, tall grass/ferns, desert dead bushes, mushrooms, reeds, pumpkins, cacti.
5. Water/lava spring source attempts and temperature/altitude-based snow.

Ready owners run in canonical chunk X/Z order, including data-only halo owners.
The 2x2 feature window requires a 4x4 raw-data neighborhood including light-query margins. Completion is persisted so retries do not repeat
population. Legacy saves are considered already populated and are preserved;
create a new world to exercise the new pipeline. Water ticks pause while the
population worker is active. Player texture definitions are never rewritten.

Dungeon chest item IDs, slot/count/damage, spawner mob choices and pumpkin
metadata are retained in chunk `betaPopulationRecords`. Chest inventories,
spawner entities and pumpkin-facing rendering still require gameplay/rendering
consumers; these records preserve generation choices and RNG consumption.

## What the fixture comparison proves

`reference.py` pins `jacobo-mc/mc_b1.7.3_release` commit
`740c583901e1ff1150e9ef37e37dab5bc0e4f807`. It compiles original Java generator
bodies and the original populate method, with explicit world/material/tile
service shims. `fixtures.tsv.gz` contains 672 individual-feature cases and 264
whole-pipeline cases across all eleven biome identities, seeds (including negative seeds),
and six test environments. The C# checker compares success, subsequent RNG
output, changed block count, and a SHA-256 of final blocks plus dungeon records.

These are controlled feature and stream comparisons, **not a certificate of
full live-world seed parity**. The shims mock lighting, notifications, falling
sand, and fluid ticks. Runtime spring sources now use scheduled water/lava
flow and reactions; Beta's generation-time immediate spring updates are still
not ported into this generator adapter. The adapter computes generation lighting from the
current blocks, but exact vanilla light-update timing is not certified.
Exploration/population order also affects overlapping features in vanilla.
End-to-end region fixtures with those world services are the remaining work.

Regenerate with a Java runtime containing `jdk.compiler`:

```
python Tools/PopulationParity/reference.py --reference /path/to/pinned/reference --work /tmp/population-java
```

Compress its fixtures.tsv with gzip (mtime=0) into this directory, then run:

```
python Tools/PopulationParity/run.py --mcs /path/to/mcs --mono /path/to/mono
```

Existing terrain/cave and tree fixture suites remain separate regressions.
