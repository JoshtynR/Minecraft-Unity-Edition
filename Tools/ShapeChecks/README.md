# Code-defined Beta cuboids

This change does not modify Block Data.asset, enum block IDs, or texture assignments.
Geometry resolves from BlockType during construction/reset and save-load; the
existing texture/collision/opacity/emission settings continue to come from data.
Full cubes and double slabs remain full cubes. Existing single slabs remain half
height. Basic default cuboids now include:

| Type | Render bounds in block pixels (x/y/z) |
| --- | --- |
| All single slabs | 0..16 / 0..8 / 0..16 |
| Dry/moist farmland | 0..16 / 0..15 / 0..16 |
| Snow layer, initial state | 0..16 / 0..2 / 0..16 |
| Cactus | 1..15 / 0..16 / 1..15 |
| Cake, unbitten | 1..15 / 0..8 / 1..15 |
| Bed base, one cell | 0..16 / 0..9 / 0..16 |
| Unpressed pressure plates | 1..15 / 0..1 / 1..15 |
| Repeater bases | 0..16 / 0..2 / 0..16 |
| Closed horizontal trapdoor | 0..16 / 0..3 / 0..16 |
| Unpressed button, default wall | 5..11 / 6..10 / 14..16 |

Reference: reconstructed Beta 1.7.3 client at
https://github.com/jacobo-mc/mc_b1.7.3_release,
commit 740c583901e1ff1150e9ef37e37dab5bc0e4f807, relevant Block*.java bounds,
BlockFarmland/BlockSoulSand collision boxes and RenderBlocks face UV projection.
Cactus has the same inset side/top geometry but keeps complete face artwork as
Beta's special cactus renderer does.

Partial generic cuboid UVs project/crop the existing full-block image, retaining
face layer indices. Crack UVs use those bounds too. The optional enhanced smooth
shader now uses an independent normalized quad coordinate (mesh UV4) so image
cropping does not distort bilinear lighting. UV4 adds 8 bytes per vertex; it merges
and uploads with other channels on the existing owned/reused mesh. Inventory raw
lighting does not need that channel. No extra block materials/textures are created.

Occlusion requires the neighbor to cover the current boundary face; an inset
face is emitted even beside an opaque neighbor. Cactus cannot hide a full cube's
upper/lower face, and equal slab side faces can be suppressed. Partially covered
large faces are retained rather than split into polygons.

Player collision uses a swept axis-aligned body box against actual code bounds,
with Y/X/Z clipping, grounded probes, a half-block step and crouch edge support.
It keeps the user's generateCollider flag. Beta farmland's collision top remains
at 16 despite the drawn height of 15; soul sand draws a full cube but collides
at height 14. The old whole-cell/floor rounding collision logic is replaced so
slabs do not snap the player's feet to integer heights. This is a shape-aware
controller, not a full reproduction of Java movement physics. Initial penetration
recovery and arbitrary multipart collision need further work.

Targeting uses voxel DDA and ray/box intersections, so thin plates/buttons aren't
skipped by the former coarse sampling increment. Placement uses actual body/box
overlap and the hit face's adjacent cell. Selection outlines and crack geometry
use render bounds. Shapes reconstruct from type on save-load with no format change.

These are default cuboids, not completed nonstandard models or state mechanics.
Beds still need their second half/model, repeaters their torch details, cake bite
states, buttons their wall orientation/pressed state, trapdoor rotations/opening,
and snow stacking. Stairs, fence connectivity, crossed plants/crops, rails,
doors/signs, torches/levers, pistons and portals still need the multipart/plane
model and directional texture path. Cactus damage/soul-sand slowing and other
block effects are outside this geometry change. Existing three-face data fields
are preserved; a separate future model/texture override setup can cover exceptions.

Run `python Tools/ShapeChecks/run.py --mcs <compiler> --mono <runtime>`.
Checks compile production registry/geometry/UVs, collision, targeting, and the
actual BaseEntity velocity/collision methods against small world storage doubles.
They cover all 133 types, bounds, geometry channel counts, occlusion, UV cropping,
fall clipping, inset collision, collider flags, steps/headroom, thin-block rays,
placement overlap, standing on slabs and removed support. WaterChecks round-trips
all 133 types through production block save/load and checks reconstruction.
Other tests cover lighting, mining, texture startup, trees and mesh ownership.
Unity shader compilation, camera/controller feel, entity scaling and above/below
partial-block views still require play testing. Restart Play Mode after pulling;
existing saves can be reused.
