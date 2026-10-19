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
mesh vertices and fluid UVs, flow-level RLE, old saves, server deltas, deferred client updates,
and applying a host's player edit once.

These are regression scenarios, not an exhaustive comparison with a running
Minecraft Beta 1.7.3 instance. Unity Editor compilation, Mirror weaving, visual
rendering, and multiplayer transport still need a play-mode check.

The current rendering pass selects still or flow images from the local texture
folder, rotates flowing tops using Beta's effective-decay vector, and maps sides
at two-block scale cropped to corner height. Ice hides adjacent fluid faces;
fluid top faces remain visible beneath non-fluid blocks as in Beta. Material
solidity drives corner sampling, independent of collision/opacity settings.
Missing special fluid slots bind automatically on editor startup from
Assets/Textures/Minecraft; existing image links and animation overrides survive.
The binding can also be rerun with Minecraft > Textures > Bind Missing Fluid Textures.

Slope searches treat existing fluid below a neighbor as a downhill route, while
displacement still prevents water replacing lava. Two-source regeneration needs
a solid floor: Beta's source-water-below branch tests the current flowing cell's
metadata, so it does not regenerate there. Decaying orphan flow still executes
Beta's downward branch before returning. No per-update slope-cost array is allocated.

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
flow. Dropped item entities for washed-away blocks are still outside this system.

Lava levels use the existing waterLevel field for backwards-compatible save data;
RLE separates different levels for both fluids. Lava uses the weighted corner
heights but stays in the solid rendering pass; its changing emission uses normal
lighting updates. All changes are server-authoritative. The legacy water delta
batch encodes water 0..15, lava 32..47, obsidian 64, cobblestone 65,
fire age 80..95, and air 255.
Clients defer deltas until chunk/population readiness and never simulate reactions.
Both host and joining clients must use the updated build.

Water, lava and fire now share a time/insertion-ordered scheduler with one global
1000-update budget. SaveWorld captures pending positions/materials, due times,
ordering, game tick, Java gameplay RNG state and random-tick LCG state in world.json
alongside the chunk snapshot. Old saves have no queue and activate normally.
Unloaded fluid positions retry until available instead of losing a saved update.
WorldSettingsManager transfers this state only to its matching saved world.

Runtime RNG stays independent of generation/population RNG. Live vanilla-world
RNG/tick parity is **not certified**: our active chunk set and other world systems
differ, and this project still represents flowing/stationary liquids with one
block type per material. These tests do not port generation-time immediate
liquid physics into the population oracle.

## Movement, hazards and effects

BaseEntity uses a separate 20 Hz liquid motion accumulator, independent of Unity's
fixed step. Production checks cover both .02 and .05 second fixed intervals.
Water/lava acceleration is .02 per tick, drag .8/.5, gravity -.02 and held jump
buoyancy +.04. Water currents add .014 along the normalized aggregate flow vector,
including the falling-wall downward component. Solid collision and the .3 rim
escape impulse are retained; fluids do not act as collision cubes even when an
asset's collider toggle is enabled. Flight continues through the existing path.

Player hazards run only offline/on the server and replicate air/burning via
SyncVars. Air lasts 300 ticks, drowning deals 2 points after each subsequent
20 ticks, lava attempts 4 points per tick through Beta's damage-resistance rule,
and lava/fire refresh 600/300 burn ticks. Water extinguishes; flight is immune.
The HUD displays classic air bubbles. Existing hunger/sprint rules are retained.

BetaFireSimulation ports dry-weather lava ignition, fire age/support rules and
the original encouragement/burn table. Random ticks use the Beta 80-samples-per-
active-chunk LCG; fire age survives RLE saves and server deltas. Filled chests
retain their existing removal protection. Rain extinguishing and TNT explosions
need the corresponding weather/explosion systems and are not simulated here.

FluidCameraEffects scopes underwater (.02,.02,.2; density .1) and lava
(.6,.1,0; density 2) fog to the local player's camera, restoring global state,
skybox/clear settings and background afterward. Voxel, water, chest and particle
shaders consume it. Water-overlay artwork is optional; fire uses the existing
fire_0.png sheet from the texture folder. Missing particle references bind from
the existing particle folder without replacing user assignments.

Four reusable emitters cap particles at 256 each. Effects cover entry splashes,
bubbles, hardening smoke/fizz, lava pops and fire smoke. Materials, generated
audio clips, child emitters and audio objects have explicit owners/cleanup.
PlayerHudAssets accepts original fizz/splash/flow AudioClips; when none are
assigned, generated noise provides fallback audio, not original Minecraft audio.
Particle appearance/timing and Unity shader output still need in-game comparison.


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
layered waterfalls can still require finer face sorting. Underwater fog is now
implemented; refraction is not a Beta feature. Animation sheets use Block Data's existing
animation settings and special fluid slots; the missing-slot binder preserves
all existing assignments.
