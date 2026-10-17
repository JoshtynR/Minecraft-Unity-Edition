# Block face images

## Assign a local Minecraft texture folder

Put the extracted texture tree in Assets/Textures/Minecraft and let Unity import
it. Keep its block/blocks, item/items, gui, entity and colormap subdirectories.
Nested assets/minecraft/textures folders are supported too. Exit Play Mode and
run **Minecraft > Textures > Assign From Minecraft Folder**.

The tool updates the existing Block Data and Item Data assets by matching exact
names/known aliases, including every current block, special-model slot and item.
It also assigns GUI icons, combines ten destroy_stage images into the existing
breaking sheet when available, and maps a Steve skin to the two existing player
materials. A square modern Steve skin is cropped to the classic upper-half layout.
Animated item images become their first square frame for the current static UI.
Ordinary block items keep their existing 3D mesh icons.

Missing/ambiguous matches keep existing links. Nothing is inferred from an
arbitrary first file, and GUI/item names cannot replace identically named block
images. IDs, counts, stack limits, transparency/lighting/collision settings, tints,
recipes, world data, original source images and catalogue ordering are preserved.
Matching images intentionally replace the old image links. Undo records the edits.
Animation sheets use per-part settings and read mcmeta frametime where supplied;
custom arbitrary frame sequences and interpolated playback are not imported.

Derived icon/skin/breaking images go in Assets/Textures/Minecraft_Generated,
using stable names so reruns retain GUIDs. This tool does not flatten entity skins
onto ordinary faces: modern bed/chest layouts lacking matching individual face
images are reported and retain the old assignments. Fonts, menu layouts, gameplay
for tools/vehicles/maps, and biome tint behaviour are not changed by image linking.

Open **Minecraft > Textures > Open Assignment Report** for all matches, unresolved
names and a source-file inventory with dimensions. The file is
Logs/MinecraftTextureAssignment.txt in the Unity project. This is the report to
use when finishing pack-specific exceptions. Restart Play Mode after assignment.

The Windows directory must exist on the machine running Unity. This editor tool
can process local files that are not committed to GitHub; remote code changes
alone do not supply those files or update a separate local asset automatically.
Tests compile the production mapper and editor assignment flow against Unity
doubles, verifying full name coverage, category isolation, fallback preservation,
special slots, crops, breaking stages, source ownership and idempotent output.

## Special model image slots

Select Minecraft > Select Block Data (or click the existing asset), search for a
block and expand its entry. **Special model images** exposes only the applicable
part slots. Minecraft > BlockDataEditor provides the same fields. The fields
appear automatically on existing assets; no asset regeneration or texture relinking
is required. Assigning a slot uses serialized properties and Unity undo. Simply
searching/opening the editor does not populate or replace the asset's list.

Every part image is optional. Empty slots retain the previous Top/Bottom/Side
and directional face lookup. The original asset and all image references are
left unchanged by this code update. Restart Play Mode after assigning images.

| Block/model | Dedicated slots |
| --- | --- |
| Doors | Lower and upper halves |
| Bed | Head/foot top, side and outer end, plus bottom |
| Rail | Straight and corner artwork |
| Powered/detector rails | Inactive straight and active/powered artwork |
| Redstone dust | Cross and straight line, plus optional untinted overlays |
| Lever | Base and handle |
| Repeaters | Top, side, bottom and torch image |
| Pistons | Face, sticky face where applicable, sides, bottom, exposed inner face and head rod |
| Standing/wall signs | Front, back, edge and standing post where applicable |
| Fire | Two flame sheets |
| Water/lava | Still (top/bottom) and flow (sides) |
| Cake | Inner cut face, selected by bite metadata |
| Adjacent chests | Left/right front, back and top half artwork |
| Grass | Optional side overlay |
| Plants, crops, leaves, torches, fence, ladder, web, button, trapdoor, portal | Optional single Surface image |

For plants and crops, each existing variant/growth-stage entry has its own Surface
slot. Standard cuboids, cactus, stairs, slabs and pressure plates already use the
appropriate Top/Bottom/Side images; furnaces, dispensers, pumpkins and single chests
already have Front/Back/Left/Right overrides. These fields remain available.
Moving pistons still require their future moving-entity renderer.

Use transparent PNGs for cutout artwork and overlay layers. Grass overlay receives
the block tint; redstone base artwork receives power tint while its optional
overlay is white/uncoloured. Overlays are offset slightly to avoid z-fighting,
with the same lighting and UV streams as their base model.

Special images join the existing shared texture array, including image
deduplication and animation timing. Enable Animate Faces for animated fire/fluid
sheets. Vertical fluid images detect rows/frame counts independently, even if an
older block-wide Rows/Frames value is set: a 16x320 still strip has 20 frames while
a 32x1024 flow strip has 32. Other images inherit the block's settings.
An assigned part exposes Playback and Override Sheet Settings for independent
columns, rows, frame count, speed, or static images. Automatic still-lava playback
uses 0..last..1 to avoid a hard last-to-first snap; flowing lava loops forwards.
Choose Loop explicitly for a still-lava sheet authored as a seamless forward loop.

Assigned animated images are checked after editor reload for non-power-of-two
scaling, compression, mipmaps and insufficient maximum import size. Importing
common lava/water/fire strip filenames also preserves source dimensions. Run
Minecraft > Repair Animated Texture Imports to repeat the check after assigning
images. This changes image import settings, never block/image links or source PNGs.
Custom platform import overrides still need to preserve strip dimensions.

Floor fire now uses the original eight leaning planes spanning the full voxel
footprint at 1.4 blocks high, rather than the narrow crossed-plant placeholder.
Both fire sheets retain their animation settings.
Fluid flow-vector UV rotation, side/ceiling fire attachment geometry, redstone simulation,
sign text, piston motion, chest inventory merging and cake eating are not added by
these image slots. Cake's existing metadata can now select the cut geometry/image.
Full pixel parity for special models is not certified; inspect assigned art in Unity.

Verification covers every exposed special-model slot across metadata and neighbour
patterns, blank fallbacks, UV/colour/light channel alignment, source-image
ownership, shared animated frame ranges and world/inventory dispatch.

Open `Assets/Block Data.asset`. Expand a block's Texture Data and drag a
Texture2D/PNG into **Top**, **Bottom**, and **Side**. Use the same image in all
three slots for a uniform block. Side is used for all four horizontal faces.
Restart Play Mode after changing assignments. Existing worlds use these images
immediately; no terrain/save migration or fresh world is required.

The asset already references separate images in `Assets/Textures/Blocks`.
They were cropped losslessly from the previous atlas (including tree-specific
grid overrides), preserving existing face selection and tint. Original atlas
files remain for unrelated UI use. `migration.json` records the exact crops.

Use Default texture type and sRGB for color images. Sources may have different
sizes and need not enable Read/Write. `Face Texture Resolution` controls the
shared rendering resolution (16–512, default 32). Images are scaled with point
filtering; square images work best. Empty slots render a magenta/black checker.

Internally, BlockDataManager prepares one texture array on the main thread
before chunk jobs start. The array is shared globally by chunks and inventory
cubes. UV0 contains (u, v, image layer), with full 0–1 face UVs. Sky/block light
still uses UV1, AO uses UV2. The two chunk submeshes and existing tint/lighting
are retained. No per-face materials or atlas coordinates are required.
The Voxel and VoxelVertexLit shaders require texture-array support (shader
model 3.5). Dedicated UNITY_SERVER builds skip GPU setup.

## Water/lava animation sheets

Select Water or Lava in Minecraft > BlockDataEditor. Assign the complete sheets
to the existing Top/Bottom/Side slots (for example, still sheets on Top/Bottom
and the flowing sheet on Side), then enable **Animate face sheets**.

- Vertical Minecraft strip: Columns **1**, Rows **0** (auto), Frames **0** (all).
- Horizontal strip: Columns = number of frames, Rows **1**.
- Grid: set columns/rows; frames run left-to-right, then top-to-bottom.
- Frames per second controls playback independently for each block definition;
  20 FPS is one frame per Minecraft tick, 0 holds the first frame.
- A smaller frame count excludes unused cells at the end of a grid.

All assigned faces use the selected layout, including optional directional
overrides. Auto rows determine square frame height separately for each image,
so still/flow sheets may use different source resolutions/frame counts. A single
square image is one frame. Invalid grids warn and fall back to a static image.
No asset assignments are rewritten automatically. Restart Play Mode to apply.

Frames are point-resampled individually into consecutive array slices at startup.
Non-readable sheets use one temporary readback at source resolution, avoiding
giant intermediate upscaled strips. Each unique image/layout/count/FPS group is
shared. A small linear, point-filtered floating-point timing texture lets the
shared vertex shader select slices with the global clock. No per-frame uploads,
mesh rebuilds, material instances or CPU animation updates are needed. Both GPU
textures are replaced/released on rebuild and disable. Chunk water transparency
and lava's existing opaque/emissive settings remain unchanged. This adds sheet
playback, not lava flow physics or Beta's procedural fluid texture simulation.

Tests cover vertical/horizontal/grid order, unused cells, static/animated sharing,
independent water/lava speeds, unreadable images, alpha, resampling and cleanup.
Actual shader compilation and playback still need checking in Unity.

`migrate_atlas.py` is a one-time helper for an **old-schema** asset. Do not run
it on the updated asset. `run.py` checks all migrated image references/pixels
and compiles production data/manager/UV code against small Unity API doubles.
It does not replace a Unity shader compile or Play Mode check.
