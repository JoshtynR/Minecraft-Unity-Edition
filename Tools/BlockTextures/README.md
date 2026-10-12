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

`migrate_atlas.py` is a one-time helper for an **old-schema** asset. Do not run
it on the updated asset. `run.py` checks all migrated image references/pixels
and compiles production data/manager/UV code against small Unity API doubles.
It does not replace a Unity shader compile or Play Mode check.
