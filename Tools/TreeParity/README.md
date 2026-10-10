# Beta 1.7.3 tree parity

```sh
python3 Tools/TreeParity/run.py
```

The production generators implement small oak, large oak, birch, tall spruce,
and full spruce. Reference checks compare success/failure, final changed block
coordinates and species, and the next value of the shared population RNG.

The gzip fixture contains 10,144 Java reference cases: 7,680 generated trees,
1,408 biome selections, and 1,056 density/attempt counts. The runner checks 35,648
values and also exercises the production Unity adapter, X reflection, height
map, neighbor readiness, and saved deferred/completed population state.

The oracle uses the following pinned source:

- [Spottedleaf/OldGenerator, commit 8c4ad258a380e9461d00d72ec7c9af9006bc050a](https://github.com/Spottedleaf/OldGenerator/tree/8c4ad258a380e9461d00d72ec7c9af9006bc050a)
- `WorldGenTrees173`, `WorldGenBigTree173`, `WorldGenForest173`,
  `WorldGenTaiga1173`, `WorldGenTaiga2173`, `BiomeBase173`, `MathHelper173`,
  and the tree-density section of `ChunkProviderGenerate173`.

Those Java tree methods run unchanged with small Bukkit storage doubles. Modern
leaf-distance postprocessing is a no-op, since it does not belong to Beta's block
shape or random stream. The large-tree generator receives `scale(1,1,1)`, as it
does during Beta population. Fixture worlds cover flat ground, negative/chunk-edge
coordinates, trunk and canopy obstructions, height limits, water, existing leaves
and logs, glass, and invalid soil.

To reproduce the fixtures with a JDK and the pinned checkout:

```sh
python3 Tools/TreeParity/generate_fixtures.py --reference /path/to/OldGenerator
```

## What is and isn't matched

Tree generator shapes, growth checks, species selection, and density rules match
this reference in the tested scenarios. That is not proof of complete world
population parity. Tree locations still use an isolated preview RNG because
preceding lake/dungeon/clay/ore stages have not all been ported. Full matching of
tree positions requires that complete shared population stream and the same
population order as the comparison world.

`Enable Beta Trees` enables this tree stage; the old hidden preview field is
retained for serialization compatibility. Generated trees wait for their 2x2
population neighborhood, and overlapping passes run sequentially in a fixed
canonical order. Cross-border changes refresh existing neighbor meshes.

Previously saved chunks keep their existing trees. Use a fresh world to inspect
all five new generators. New saved edge chunks remember whether their tree pass
was deferred so it can resume once neighbors load.

Spruce/birch have separate block IDs appended to the existing enum, with atlas
entries and fixed leaf tints. Existing block IDs remain valid. Tree atlas entries sample their complete tiles; other blocks keep their existing grid.
The added species entries select 16px tiles from the checked-in 1024px atlas.
Leaf opacity is 1, while leaves remain collidable.

Unity Editor compilation, species texture appearance, Mirror's serializers, and
live border loading still need a play-mode check. The oracle's mocks do not
validate those systems or leaf decay.
