# TNT and explosion effects

Right-click a TNT block with flint and steel. The authoritative interaction removes
the block, wears the tool once and creates a primed entity with an 80-tick fuse
(roughly four seconds; the source post-decrement detonates on update 81). It jumps slightly, falls/collides using the existing voxel sweep,
emits packed-atlas smoke, flashes white every five ticks and plays `random/fuse`.
Detonation uses the strength-four ray blast, damage, resistance, drops
and container-protection logic, plus `random/explode` and shared burst particles.
The last ten ticks swell quartically; the five-tick flash uses the source's
increasing alpha and additive white lift rather than replacing the skin.

TNT caught by creeper, bed or TNT explosions receives a shortened 10–29 tick
fuse rather than dropping an ordinary TNT item. The store is capped at 128 and
detonations at four per simulation tick; excess expired fuses wait for later
ticks. TNT that cannot be primed because the store is full is left as a block.
Missing chunks suspend simulation instead of treating the void as air.

Primed states persist per dimension; old saves have none. Remote observers get
dimension-tagged 10 Hz snapshots and one-shot explosion effects. Solo ignition
uses the existing direct host inventory action path, not a readiness wait.
Owned TNT meshes/materials are disposed when their visuals are removed/reset.

Flint and steel can also ignite a creeper. This is an intentional newer-game
feature, not Beta-only behaviour. It costs one durability and makes the existing
30-tick creeper fuse irreversible even if the player backs away or hides. Normal
proximity-triggered creepers can still defuse. Ignition persists in mob saves.

All existing creeper and Nether-bed blasts now use the same custom, hand-ticked
particle renderer as TNT; no Unity ParticleSystem is introduced. These are a
bounded burst of classic atlas explosion and smoke particles, not a claim of
complete vanilla explosion-particle count/position parity.

This pass does not add redstone power propagation or fire-spread ignition of
TNT. Those are separate systems; direct flint-and-steel and blast chains work.

```bash
python3 Tools/TntChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
python3 Tools/MobChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
```

The focused runner compiles actual TNT state/simulation, flint-and-steel branch,
visual code and collision code against Unity/network doubles and a mocked blast
adapter. It verifies countdown timing, replay/rejection tool accounting, chain
fuses, save-copy isolation, invalid saves, unloaded chunks, authority and budgets.
MobChecks exercises actual creeper interaction/countdown logic and saved ignition.
Existing blast hooks, shader property and audio file existence are checked too.

Unity Play Mode/build testing is still required for real sound output, shader
compilation/flash appearance, particles, full disk save/load, Mirror weaving and
remote observation. Try multiple TNT blocks, submerged TNT, walls/containers,
dimension travel and quitting/reloading partway through the fuse.

## Source comparison follow-up

Reviewed the Beta reference's EntityTNTPrimed, RenderTNTPrimed, Explosion,
BlockTNT, World.getBlockDensity and Block resistance setters/getter.

- Retained the source's small initial-angle radians/degrees quirk and sine table.
- Corrected the post-decrement fuse boundary, entity centre/feet offset, smoke
  height and centred swelling; ground collision zeroes vertical motion before
  the nominal bounce multiplier, as Entity.moveEntity does in the reference.
- Blast-chain priming is silent; explicit ignition still hisses.
- Creeper/TNT exposure uses the bounding-box sample grid (45 samples for the
  standard player) and shape-aware raycasts before block removal. TNT damage
  does not receive the hostile-creeper difficulty multiplier.
- Corrected explicit/hardness-derived blast resistance, including noncolliding
  rails; block collision flags no longer incorrectly imply zero resistance.
- Creeper/TNT block-drop rolls use 30%, independent of blast strength.
- Explosion audio uses volume four and source pitch variation; the audio pool
  now permits the corresponding 64-block range without increasing clipped gain.
- Effect payloads include affected air cells. Samples use the reference's
  midpoint explosion position, cell smoke position and outward velocity formula.
- Beds remove their halves before the blast and measure player cover before
  destroying surrounding walls, rather than measuring through the new crater.

Not full parity: effects remain capped at 128 sampled cells, four detonations per
tick and 128 primed entities. World RNG/order, floating-point ray stepping, exact
explosion knockback/hurt handling, bed blast mob damage/drop/fire semantics,
redstone/fire ignition and the modern direct-use/creeper ignition interaction
still differ. Manual creeper ignition is deliberately retained at the user's
request. Focused fixtures are source-guided checks, not a complete Java-versus-
Unity live-world equivalence certification.
