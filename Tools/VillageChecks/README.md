# Beta 1.8-style villages on Beta 1.7.3 terrain

Enabled by default for newly generated Overworld chunks through **Generate
Villages** on the World component. Disable this before generation to retain the
strict Beta 1.7.3 path. Existing saved chunks are loaded as they are, with no
retroactive village insertion. A village meeting an already-explored boundary
can therefore be cut off at that boundary; use fresh terrain for testing.

## Generation

- Independent Java-compatible RNG and deterministic region candidates. Regions
  span 32 chunks with a candidate offset in the first 24 chunks. Negative
  coordinates use floor division. This is a custom placement/layout adaptation,
  not vanilla Beta 1.8 structure seed parity.
- Root sites use Beta 1.7.3 plains, desert, savanna or shrubland. Ocean/lava,
  excessive elevation and extreme slopes reject a site. Building footprints
  average the unmodified terrain height, fill foundations down to the ground,
  and reject excessive relief. Building elevations can step with gentle hills.
- A central cobblestone/water well, four potential gravel streets, small houses,
  huts, larger houses, libraries, churches, blacksmiths, irrigated wheat fields
  and wool/fence/torch lamps. Building selection and road lengths vary by seed.
  Some roads/buildings may be rejected; a layout needs at least three buildings.
- The building templates are new Beta-era-style approximations, not a block-for-
  block port of the original component templates or its weighted expansion queue.
  They use existing oak/cobblestone blocks, full glass blocks and wheat, rather
  than adding modern paths, beds, themed palettes or glass panes. No villagers,
  trading or loot tables are added. Blacksmiths contain furnaces and a contained
  lava basin. Roof/door/ladder/torch metadata reflects into Unity coordinates.
- Roads follow gently changing surface heights, have supported gravel, and use
  planks over water. Buildings and the well reject water under their footprint.
  Steep streets are shortened or rejected rather than cutting long cliff tunnels.

## Chunk integration and persistence

Plans sample regenerated raw/surfaced/cave terrain, not mutable resident chunks.
They are prepared before obtaining the target chunk's shared raw-terrain scratch
array. Each component clips all block and metadata writes to its target chunk,
then uses the existing reflected-X storage boundary. There are no writes into
unloaded neighbors, no dependence on visit order, and no retained Unity objects.
Plans are cached for at most 64 regions; ground samples for at most 128 chunks.
Both caches are released with TerrainGenerator's existing generator lifetime.

Village generation runs after caves and before Beta population. A 4 KiB bit mask
is allocated only in chunks touched by village blocks/cleared air. The population
adapter respects it, so later trees, lakes and ores cannot overwrite a building
or fill its interior. Gameplay edits, crop growth, fluids, lighting and normal
block interactions remain independent of that population-only protection.
Chunk snapshots copy the mask; deserialization restores it, including deferred
population at streamed edges. Old saves without a mask keep their existing data.

The base Beta 1.7.3 terrain algorithm is unchanged. Outside a structure's written
footprint, raw terrain stays identical. Enabling structures can change decoration
outcomes near villages because features now encounter protected structures;
disabling villages retains the original population path and reference fixtures.
The generation toggle is an Inspector setting, not a new per-save menu option.

## Checks and finding a village

```bash
python3 Tools/VillageChecks/run.py
python3 Tools/VillageChecks/run.py --locate 0 --regions 2
```

The locator accepts a numeric signed 64-bit seed and searches region coordinates
from `-regions` through `+regions` on each axis (capped at 8). It prints the first
accepted village's well position in **Unity world X/Z**, using the same terrain
sampler/planner as gameplay. An empty search exits with code 1; larger searches
may take longer. Standard `--mcs` / `--mono` compiler/runtime options are supported.

Known fixture: **seed 0, Unity X=1015, Z=-840**. Create a fresh seed-0 world or
explore those coordinates if they have not already been generated.

The suite exercises seeded layout variety, signed region boundaries, all four
orientations, chunk clipping, reversed visit order, cache eviction/replanning,
stepped ground adaptation, rejected sites, paired doors, torch attachment,
hydrated crops, the real TerrainGenerator adapter, cold parallel generation,
raw-terrain scratch safety, metadata storage, population protection and the actual
ChunkSaveData snapshot path. The BetaParity, PopulationParity and TreeParity
reference suites also pass with villages disabled.

These tests mock Unity storage. Play Mode/build verification is still required
for visual resemblance, road/door accessibility, lighting, fluid containment,
real disk reload, multiplayer chunk transfer and generation latency. No exact
1.8 village probability, layout, template or cross-version seed parity is claimed.
