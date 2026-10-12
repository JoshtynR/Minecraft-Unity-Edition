# Texture-ready Beta 1.7.3 block catalog

The catalog has 133 local enum/data entries (112 additions). Existing IDs 0..20
and existing Block Data texture references are preserved. New IDs are explicit
and append only. Do not renumber them: chunk/inventory saves store these local
BlockType values. They are not vanilla numeric IDs.

All registered vanilla block IDs 0..96 are represented. Source flowing/still
water (8/9) map to the existing working Water implementation; lava 10/11 map to
Lava. Tree species remain the preexisting split enum entries. Additional texture
variants include the three saplings, sixteen wool colours, all four single and
double slab materials, tall grass/fern/dead grass, eight wheat stages and dry/
moist farmland. Planks remain a single Beta type; later species-specific plank
blocks, stone bricks, melons, glass panes and fence gates are not added. Technical
states such as moving piston, piston head, lit furnace, active redstone blocks
and the obsolete locked chest are included in the catalog.

Every new definition has empty top/bottom/side image fields, a white tint,
collision/opacity/emission defaults, vanilla source ID/metadata and mining
hardness/hand-harvest rules. Missing images use the existing checker layer.
Metadata in this catalog documents texture variants; it is not a new general
runtime orientation/state system. New stone/sandstone/cobblestone slabs use the
existing half-slab class and saved-block mapping. Double slabs use full cubes.
New circuit/plant material definitions participate in the Beta corner fallback.
Same-opacity emitter changes now remove stale block light (torch -> air).

Use **Minecraft > BlockDataEditor**. Search/select a type, assign Top/Bottom/Side,
inspect image previews, then Save Block Data. The inspector list works too. In
Play Mode, Give 64 to Local Player adds the selected entry without filling or
resizing the survival inventory. Restart Play Mode after image changes to build
the texture array. This editor now uses face-image previews rather than the old
3D preview that relied on a runtime-only texture array.

This is the complete definition/texture-assignment catalog, not 97 finished
block systems. Existing cube/slab/fluid geometry remains; new stairs, crossed
plants, thin fixtures, doors, rails, beds, fences, signs, crops, portals and cake
need their special models/orientation and collision behavior. Their current
geometry is a cube fallback except the supported single slabs. Front-specific
textures on furnaces/pumpkins and multipart models need a future directional/
model texture path beyond the current top/bottom/side fields. Redstone/pistons,
containers/crafting, growth/decay, crop drops, eating cake, portals, climbing,
spawn logic, support placement and multiblock updates are not implemented by
adding these definitions. Ores/plants are not newly inserted into world generation.
Non-block item drops remain unsupported. Do not treat catalog entries as full
playable Beta parity.

Reference: reconstructed Beta client
https://github.com/jacobo-mc/mc_b1.7.3_release at
740c583901e1ff1150e9ef37e37dab5bc0e4f807. Block.java registry, constructor/material
classes and block subclasses supply defaults. catalog.json retains the source
identity and checked defaults. generate.py takes a local reference checkout,
reproduces descriptors/enums and appends missing asset entries without changing
existing texture assignments. Changes to generation order require save-ID review;
do not regenerate from a different reference version. Existing assigned asset
settings are retained rather than forcibly overwritten by regeneration.

Run `python Tools/BetaCatalog/run.py --mcs <compiler> --mono <runtime>`.
The checks compile the production catalog and hand-mining rules, verify all
vanilla IDs/selected variants, original local IDs, finite zero-hardness breaking,
and compare serialized new entries/blank image fields with catalog defaults.
Other regressions cover old lossless face images, texture array startup, water
save/placement, torch light removal, mining, light corners and trees. Unity
Editor compilation, new slab placement/save-load and editor texture assignment
still need play-mode validation.
