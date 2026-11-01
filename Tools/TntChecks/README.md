# TNT and explosion effects

Right-click a TNT block with flint and steel. The authoritative interaction removes
the block, wears the tool once and creates a primed entity with an 80-tick fuse
(four seconds). It jumps slightly, falls/collides using the existing voxel sweep,
emits packed-atlas smoke, flashes white every five ticks and plays `random/fuse`.
Detonation uses the existing strength-four ray blast, damage, resistance, drops
and container-protection logic, plus `random/explode` and shared burst particles.

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
