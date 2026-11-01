# Leaf decay and apple drops

Compared against `BlockLeaves`, `BlockLog` and random block ticking in
`jacobo-mc/mc_b1.7.3_release` (1.7.3-LTS).

- Removing a log marks leaves in a radius-four cube; removing a leaf marks
  leaves in a radius-one cube. Other neighbor updates do not start decay.
- Support requires a log within four face-connected steps through leaves.
  Winding paths count their actual length, not Manhattan displacement.
- Pending bit 8 is saved in packed block state and resumed on chunk activation.
  Connected leaves clear the flag. Unloaded chunks defer the check.
- Timers approximate the marginal distribution of Beta's 80 random samples per
  32768-block column per tick, rather than destroying leaves after 0.5–1.5 seconds.
  This is not the original world LCG sequence or exact cross-block RNG ordering.
  Work remains capped at eight due checks per update; population writes pause it.
- Existing player-placed leaf protection (bit 4) is retained intentionally.
  This is not a claim that the reference's decay callback checks that bit.
- Saplings retain their independent 1/20 roll. Beta did not drop apples.
  Our intentional addition is a separate 2% oak-only apple roll, adjustable via
  `World.leafAppleDropPercent` (0 disables it). Spruce/birch and intact sheared
  leaves do not drop apples. Both survival mining and natural decay use it;
  explosion drops are unchanged. A sapling and apple can drop together.

Run ShapeChecks for production connectivity/metadata/drop/timing rules, and
CreativeChecks for inventory regressions. These use Unity/network mocks, not
the Unity editor. In Play Mode, fell a tree, retain a neighboring log, unload
and reload a pending canopy, then test hand harvest versus shears. Set apples
to 100% temporarily to verify delivery, and restore 2% afterward.
