# Nether dimension implementation and checks

Build a classic 4x5 obsidian frame (2x3 opening; corners optional), use Flint and Steel
on its interior, then stand inside for four seconds. Portals search within 128 blocks
of the scaled destination and create a supported exit when none is available.
The 15-second cooldown counts down after leaving portal blocks.

## Scope

- A lazy Nether World instance shares the seed and original save root, with its own
  chunk dictionaries, generator, fluid scheduler/RNG, dropped items and containers.
- Overworld files retain their original locations. Nether chunks and metadata live
  under `DIM-1`; chest/furnace stores use that same dimension key. Player data stays
  shared and includes a dimension ID. Old player JSON defaults to the Overworld.
- Client block/fluid/drop/mob packets and chest lid RPCs carry dimension identity.
  The server resolves player actions against their world. Inactive render roots,
  particles, sounds, avatars, colliders and nameplate canvases stay hidden.
- Generation/meshing handoffs are awaited. Portal travel pauses movement, hazards,
  mining and transform interpolation behind the existing loading screen. Reliable
  chunk snapshots reconcile server edits before the arrival is acknowledged. A bounded
  inventory checkpoint saves the source before travel and reconciles the arrival;
  disconnecting mid-transfer retains the source checkpoint.
- Nether sky transport is zero; the brightness floor is .1. Red fog replaces the
  sky background. Lava uses Nether decay, bucket water evaporates, beds explode,
  and Nether deaths respawn in the Overworld. No Nether mobs were added.
- Normal exit-to-title awaits saves for all loaded dimensions.

## Accuracy boundary

126 independent Java-reference hashes cover raw terrain, surface and caves across
six seeds and seven chunks: zero, positive/negative coordinates, Far Lands region,
and signed 64-bit seed extremes. The production adapter is also checked against
all 42 final Nether chunk hashes, including the X reflection and air above Y=127.
The existing 56-case Overworld fixture suite remains unchanged.

Population includes lava springs, fire, both glowstone passes, and mushrooms.
Historical Beta Nether population reused mutable provider RNG state, so decorations
were load-order dependent. This implementation deliberately uses per-chunk seeding
to make each population owner’s RNG reproducible; overlapping features can still
depend on population order. **Decoration positions are not certified vanilla 1:1**.
Portal placement and environmental interactions are implemented, but their full
live-game behaviour is not certified as byte-for-byte vanilla parity.

## Automated checks

`python Tools/NetherChecks/run.py --mcs /path/to/mcs --mono /path/to/mono`

Compiles the new runtime classes and the complete production WorldServer against
Unity/Mirror doubles. Exercises frames in both orientations, missing corners and
broken frames, scaling/clamping, existing exits, loading waits, transform resets,
cooldowns, lazy construction, ownership isolation and deterministic population.
It also verifies explicit RPC signatures and save-on-exit wiring.

The pinned Java reference is emortaldev/Minestom173 commit
`4c665027560d63e2afa86a5424fc7d9dce974be6`. To reproduce the hashes, first run
`Tools/BetaParity/reference.py` to build its support classes, then run:

```
python Tools/NetherChecks/reference.py --reference /path/to/Minestom173 --support /path/to/java-parity --work /tmp/nether-reference
```

The reference harness retains the Java terrain/surface/cave method bodies and
adapts only server interfaces and block storage. It does not read C# generator output.

## Unity Play Mode verification still required

- Compile with the installed Unity/Mirror versions; check the Nether fog, portal
  sheet/overlay orientation, arrival pose, and sounds with the assigned textures.
- Travel both ways, break/re-light frames, and test forced exits over lava.
- Save/reload and quit in each dimension; check blocks, fluids, items and containers.
- Run host plus client, with players in different dimensions. Check simultaneous
  edits at matching coordinates, late joins, return travel, and disconnect/cancel
  during loading. Unity rendering and the actual Mirror transport/weaver are not
  executed by the command-line doubles.
