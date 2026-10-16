# Block face images

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
