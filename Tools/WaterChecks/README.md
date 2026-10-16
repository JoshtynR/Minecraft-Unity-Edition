# Water/lava checks

Run with Mono installed:

```sh
python3 Tools/WaterChecks/run.py
```

The runner compiles production water simulation, surface heights, block metadata,
section serialization/deserialization, world adapter and block RPC methods. Unity
and Mirror storage/transport use small test doubles. Mono 6's compiler requires
lowering the existing BlockMapping switch expression; the branches are unchanged.

Checks cover five-tick scheduling, deduplication, seven horizontal flow levels,
falling water and draining, supported two-source regeneration, shortest downhill
paths and ties, obstacles, unavailable chunks, weighted corner heights, actual
mesh vertices, flow-level RLE, old saves, server deltas, deferred client updates,
and applying a host's player edit once.

These are regression scenarios, not an exhaustive comparison with a running
Minecraft Beta 1.7.3 instance. Unity Editor compilation, Mirror weaving, visual
rendering, and multiplayer transport still need a play-mode check.

## Play-mode check

1. Place water on a flat platform. Check its spreading height and seven-block
   reach, then remove the source and watch the flow drain.
2. Place water above a drop. Check that it falls and spreads at the bottom.
3. Put two sources on opposite sides of a supported empty cell; check that the
   middle regenerates. Check a nearby hole pulls flow toward it.
4. Save/reload while water is flowing. Repeat near a chunk boundary and with a
   second connected player.

Water and lava share the legacy-named WaterSimulation through different material
views. Water advances every five game ticks; lava every thirty. Overworld lava
increases horizontal decay by two (source levels 0/2/4/6, three-block reach), falls
with the 8..15 flag, does not regenerate sources, and has Beta's three-in-four
hesitation when increasing decay. Falling lava initially emits level one and its
next update recalculates the normal decay. The optional pure-model Nether flag
uses decay one, but the running game currently uses the Overworld configuration.

## Beta reactions (not modern Java rules)

Original reference: jacobo-mc/mc_b1.7.3_release at
740c583901e1ff1150e9ef37e37dab5bc0e4f807, BlockFluid.checkForHarden,
BlockFlowing.updateTick/liquidCanDisplaceBlock and Material/MaterialLogic.

| Lava state | Touching water on side or above |
| --- | --- |
| Source, level 0 | Obsidian |
| Flowing, levels 1..4 | Cobblestone |
| Levels 5..7 or falling 8..15 | No hardening from this rule |

Water below alone does not harden the lava above it. The original Beta
BlockFlowing lets downward lava replace water with falling lava; it does **not**
have the later stone-generator branch. Do not substitute the modern stone rule.
Hardening runs on change notifications/activation, without waiting thirty ticks.

Fluid displacement follows canonical materials, not user collider/opacity flags.
Flowers, crops, torches, dust, rails, repeaters, fire, portal and snow
layers wash away. Reeds, ladders, signs, doors, webs, pressure plates and partial solid blocks stop
flow. Dropped items for washed-away blocks, fizz/smoke, fire spread, burning
entities, swimming and currents are not implemented by this pass.

Lava levels use the existing waterLevel field for backwards-compatible save data;
RLE separates different levels for both fluids. Lava uses the weighted corner
heights but stays in the solid rendering pass; its changing emission uses normal
lighting updates. All changes are server-authoritative. The legacy water delta
batch now encodes water 0..15, lava 32..47, obsidian 64, cobblestone 65, air 255.
Clients defer deltas until chunk/population readiness and never simulate reactions.
Both host and joining clients must use the updated build.

The gameplay RNG for lava hesitation is independent of generation/population RNG
and restarts on world load. Live vanilla tick order/RNG parity is **not certified**;
tests are controlled regression scenarios, not a full Java-world oracle. Scheduled
pending ticks are not serialized. This does not port generation-time immediate
liquid physics into the population oracle.


## Transparent water rendering

Chunk submeshes are now solid (0), cutout leaves/glass (1), and blended water (2).
The Chunk prefab binds Water_Material to slot 2. Its Voxel/Water shader samples
the same face texture array and shares Beta lighting code with Voxel, but draws
in the Transparent queue with SrcAlpha/OneMinusSrcAlpha blending, no depth writes,
and both face orientations visible. Water Opacity defaults to 0.6 and multiplies
image alpha and block tint alpha. It does not change water's lighting opacity (3).
Terrain under water remains in the solid pass; internal water faces are culled.

LightingParity checks production routing (water, cutout blocks, adjacent water,
and submerged terrain). MemoryChecks verifies three-way vertex/light merging,
water index offsets, repeat uploads, and removing water on a dry mesh rebuild.
Shader compilation and above/below-water views need verification in Unity. The
rendering uses Unity's normal transparent renderer sorting; intersecting or
layered waterfalls can still require finer face sorting. Underwater fog,
and refraction are not included. Animation sheets are supported separately through
Block Data's Animate face sheets settings; no texture assignments are changed here.
