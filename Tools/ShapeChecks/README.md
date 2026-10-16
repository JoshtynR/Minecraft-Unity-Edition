# Code-defined Beta block models and state

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
use render bounds. Shapes now reconstruct from type and saved metadata. Older
saves keep stair facing and use safe defaults for other directional blocks.

## Special-model/state pass

`SpecialBlockGeometry` encodes type and native 4-bit state in existing `blockShape`
enum storage (values >=1024). The legacy `stairFacing` save/message field carries
the full nibble; `hasBlockState` distinguishes old files from an explicit zero.
Construction, copy/reset, mapping, RLE, load, placement and RPC preserve state;
RPC arguments remain nonoptional for Mirror. Old IDs/stair values are unchanged.

- Torches: original four image sheets, 2x2-pixel cap, native floor/wall attachments,
  lean and selection bounds; no collision.
- Fences: 4x4-pixel post and two rails per connected axis. Only other fences
  connect in Beta. Isolated posts retain short X rails. Collision is the original
  full-width, 1.5-block-high box, not modern connected posts.
- Dust: 1/64-block-high cross/line quads, cropped branches, solid-side climbs,
  native power tint, no collision. Connections derive from neighbors.
- Ladders: offset sheets and 1/8-thick directional selection/collision.
- Rails: flat/sloping quads, straight/curve images, basic neighboring turns and
  slopes saved before meshing. Ambiguous powered-junction logic is not ported.
- Doors/trapdoors: thin rotating/opening panels. Doors place/remove as two matched
  halves; beds likewise place/remove head/foot pairs.
- Signs: standing post/rotating board or wall board; no text editing yet.
- Levers/buttons: mounted bases/handle and directional/pressed bounds.
- Repeaters: thin base and two torches; delay moves the second torch.
- Pistons: six-direction base/head/rod models; extended base and original cap/rod
  collision boxes. MovingPiston emits no static model: it needs a moving entity.
- Webs: crossed sheets. Portal: thin directional volume. Fire currently uses static
  crossed sheets, not the complete attachment-sensitive animated model.

Right-click toggles wooden doors, trapdoors and levers, and cycles repeater delay.
Crouching bypasses interaction for placement. Iron doors are not hand-toggleable.
These are visual state changes, **not** a redstone simulation. Paired placement
checks the second cell before writing; breaking either half removes its matching
partner. Body checks use oriented pieces. Placement validates attachment support,
but automatic drops when support is removed are not ported.

`Assets/Block Data.asset` is never edited by this pass. Optional
Front/Back/Left/Right image fields fall back to Side when blank. Front is local -Z;
furnace/dispenser/chest/pumpkin metadata rotates those image choices. Existing
Top/Bottom/Side assignments remain valid.

Special artwork conventions (assign only if current images need changing):

| Model | Existing image fields |
| --- | --- |
| Torch, ladder, web, sign board | Side |
| Dust | Top = cross mask, Side = straight mask |
| Rail | Top = straight track, Side = curved track |
| Door | Side = lower panel, Top = upper panel |
| Repeater | Top = plate artwork, Side = torch artwork |
| Directional cube | Top/Bottom/Side plus optional directional overrides |

Full pixel parity is **not certified** for this whole pass. Tests verify geometry,
state, channels and connections, not a Java golden renderer for every model.
Fire attachment geometry, directional repeater connections, powered junction
decisions, piston animation, sign text and exact special-model AO remain follow-up
work. Special model images, including animated fire, dust overlays and separate
bed-head/foot artwork, can now be assigned in Block Data; see
Tools/BlockTextures/README.md. Cake bite metadata selects its cut bounds and inner
face image, but eating, snow stacking and other gameplay effects are unchanged.

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

LightingParity compiles actual world/inventory dispatch for every special type and
state. WaterChecks compiles copying, RLE/load, paired world edits and RPC methods.
The lighting runner accepts `--stair-check <path>` for testing an isolated intact
copy without changing an unrelated local edit.
