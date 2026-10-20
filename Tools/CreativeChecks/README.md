# Survival inventory with Too Many Items sidebar

E opens one menu: the 176x166 Beta survival panel and the right-hand block grid
are always visible together. Layout follows the original ContainerPlayer slot
coordinates (27 backpack cells and nine hotbar cells, with 18-pixel pitch).
Four crafting inputs, a read-only result, a skinned player preview and four
equipment positions occupy the upper area. The project has block types but no
armor items. Equipment rejects ordinary blocks while accepting Beta's pumpkin
helmet exception (limit one), which also appears on the preview. Armor items/stats
and the first-person pumpkin mask remain separate future systems.

The sidebar uses the item catalogue: 131 block counterparts and 115 standalone
items/variants. The complete 246-entry catalogue uses pages, including at the
scene's normal 1080p/3x GUI scale. Smaller windows use pages
while retaining both panels. Hover displays the editable item name;
left-click adds up to the item's stack limit, right-click one, merging into available storage rather
than replacing the selected hotbar slot. This follows the original TMI right
sidebar give controls; time/weather buttons, presets and unlimited stacks are not
implemented. No Block Data asset, scene or existing texture is rewritten.

Reference: Beta 1.7.3 ContainerPlayer/Container at
https://github.com/jacobo-mc/mc_b1.7.3_release/tree/740c583901e1ff1150e9ef37e37dab5bc0e4f807
and the original author's TMI controls:
https://www.minecraftforum.net/forums/mapping-and-modding-java-edition/minecraft-mods/1272385-toomanyitems-the-inventory-editor-and-more-1-8

## Inventory behavior

- Left-click picks up, merges or swaps; right-click takes the rounded-up half or
  deposits one. Different IDs or damage values swap. Item-specific limits apply.
- Shift transfers between hotbar and backpack, merging partial stacks first and
  leaving any remainder at the source. Slots no longer pick up items merely when
  the pointer enters while the mouse is held.
- AddItem's bool API is atomic: insufficient space returns false without changing
  anything. The catalogue's separate partial-insert path adds what fits.
- All 151 Beta 1.7.3 crafting recipes are registered, including tools, armor,
  redstone, transport, food, dye mixing and wool dyeing. The inventory grid is
  2x2; right-click a crafting table for its 3x3 grid. Shaped recipes translate
  and mirror horizontally. Shapeless recipes require exactly their ingredient
  multiset. Source block ingredients accept metadata variants (all log species,
  any wool in beds/paintings); dye recipes require white wool. Beta quantities
  and shapes apply: three slabs, two ladders, six powered/detector rails, vertical
  mushroom stew, and gold blocks in golden apples. This implements recipes,
  not the use behavior of every crafted item.
- Result clicks consume ingredients only after the entire output fits; shift
  repeats while ingredients and space remain. Milk buckets become empty buckets.
  Result slots cannot receive cursor items. Table inputs support normal clicks,
  half-stack pickup, single-item placement and shift transfer back to storage.
- The table closes when removed, more than eight blocks away, after death, or
  after a network disconnect. Table ingredients return to storage; overflow
  becomes saved recovery stacks. Saving with the table open snapshots its inputs
  into recovery, preserving the existing four-slot save format without duplication.
- Closing returns cursor and crafting inputs to free storage. If storage is full,
  they remain persisted and reappear on reopening; they are not destroyed. The
  game does not yet have dropped-item entities, so vanilla outside-click drops
  and crafting-grid drops on close are deliberately not simulated by deleting.
- New survival inventories start empty; starter cheat stacks and K-key grants
  were removed. The sidebar remains a deliberate item-giving tool.

## Saving and old worlds

The live inventory is now exactly 36 slots. Loading a previous 72-slot save keeps
the first 36, inserts surplus stacks into free space, and preserves any remaining
stacks in a recovery list. A Recover saved stacks button appears only when needed.
Click it after making space. Saved cursor, crafting inputs, equipment and recovery stacks
have their own optional fields; missing fields in old JSON default safely.
Short/missing inventory arrays and oversized old stacks are also handled.
Every save snapshots stacks so background serialization cannot observe live UI
mutations. Both server and client builds need the updated message schema.

## Item definitions and stacks

Select Minecraft > Select Item Data, or open Assets/Resources/Item Data.asset.
The searchable inspector filters by display name or ItemType. Expand a matching
entry and assign its icon. This separate catalogue contains ItemDefinition entries: stable ItemType ID,
display name, individual Texture2D icon, maximum stack, maximum damage and optional
placedBlock. Block Data is untouched. Blank block icons reuse the existing mesh
and textures; standalone entries with no image show two-letter placeholders.
All Beta 1.7.3 item definitions are present: five tool/sword tiers, five armor
sets (including chainmail), food, materials, buckets, transport, fishing rod,
shears, compass, clock, map, both music discs, all sixteen dyes and charcoal.
Each variant has a separate image field. Existing block entries represent the
seven overlapping native items: sign, wooden/iron doors, sugar cane, cake, bed
and repeater. Technical world-block counterparts are retained for compatibility.
Definitions and catalogue grants do not implement tool actions, armor equipment,
food use, bucket interactions, projectiles, vehicles, maps, music playback or new
recipes/drops. Icons remain empty for the user to assign.

BetaItemCatalog maps all 106 native item registrations and 122 item/metadata
variants to stable project IDs, with source stack limits and maxDamage defaults.
The independent tab-separated reference is beta-items.tsv, reviewed against
Item.java and item subclasses in the pinned Beta source above. Tool durability
comes from EnumToolMaterial; armor uses (base * 3) << tier. Native signs stack to
one, cookies eight, eggs/snowballs sixteen. Beta bows have no durability and maps
retain the original 64-item limit. No later-version items are included.

InventoryStack and serialized PlayerInventory.Slot now store ItemType, count and
damage used. Existing numeric IDs 0..132 and the serialized field name "type"
are retained, so old JSON inventory arrays and Unity-serialized slots map directly
to the matching block item; missing damage defaults to zero. Item-only IDs begin
at 1000. Never renumber either enum's existing values. These are project IDs,
not vanilla Java item IDs. World chunks continue storing BlockType and state.

Placement explicitly resolves ItemCatalog.TryPlacedBlock; a nonplaceable item
cannot be cast into a world block. The old AddItem(BlockType) overload maps block
drops to their corresponding item before inserting. New callers can use
AddItem(ItemType, count). Damage travels with clicks, transfers, crafting-grid
returns, snapshots, equipment and recovery; differing damage cannot merge.
Durable definitions force stack limit one, but no tool wear/actions or durability
bar is added yet. Native per-item limits apply, bounded to sixty-four.
Oversized old stacks are split into available slots/recovery.

The runtime snapshots resource definitions on first use each play session.
Missing entries receive defaults without modifying assets. Invalid/duplicate
entries are ignored and stack limits are bounded to 1..64. Restart Play Mode
after editing the catalogue. The menu's individual image references are shared;
destroying UI icons never destroys their source texture.

## Verification

Run `python Tools/CreativeChecks/run.py --mcs <compiler> --mono <runtime>`.
The runner compiles the complete inventory/slot/rules/preview code against Unity
UI, input, resources and block-icon doubles. It checks legal stack-count pairs
for left/right clicks, conservation and bounds, shift remainders, crafting and
failed output takes, actual menu construction, both visible panes, responsive
pagination, icon reuse/depth, old-save recovery and owned-resource cleanup.
Item checks cover all legacy ID mappings, standalone placement rejection,
editable limits/images/names, damage preservation, oversized-stack recovery,
and cursor versus shift-crafting capacity. The runner also validates that all
246 asset entries match the enum without duplicates, all 122 native variants
have the expected stack/durability settings, and all catalogue pages are reachable.
Unity shader compilation, Mirror weaving and live pointer/rendering still require
Play Mode validation. The skinned cuboid preview is projected by InventoryPlayerGraphic
through MaskableGraphic and the default UI material. It uses canvas draw order rather
than a world MeshRenderer, supports camera and overlay canvases, and allocates no
extra camera, render texture, material clone or native mesh. Visible triangles are
culled, lit and sorted back to front before their depth is flattened onto the canvas.
Unchanged cursor poses reuse the graphic. Equipped pumpkin faces use the same path
with their assigned individual block images. The source skin/material stay shared.
The custom graphic explicitly requires CanvasRenderer; MaskableGraphic/Graphic
do not add that component automatically. The harness models RequireComponent and
checks that the player and every helmet face receive a renderer when constructed.

PlayerHudAssets.inventoryPlayerSkin is a direct texture reference, populated by
the editor from the player's material or an existing Steve image when empty.
Explicit assignments are preserved; the full texture auto-assigner updates this
field when changing the player skin. The legacy inventory material is optional.
A missing material never skips model creation: missing artwork uses the shared
white texture with an actionable warning. Tests build and populate the preview
with a direct HD skin and with both the skin and material absent.

If a preview remains invisible in Unity, open the inventory in Play Mode and run
Minecraft > Diagnostics > Copy Inventory Preview Report. It copies live setup,
skin references, populate/vertex counts, canvas renderer culling/alpha/depth,
camera clipping, transforms and canvas groups to the clipboard and also writes
Logs/InventoryPreviewReport.txt. The report is read-only except for forcing a
canvas update and writing that report; it does not modify assets or the scene.

The survival panel uses PlayerHudAssets.inventory, automatically bound from
Assets/Textures/Minecraft/gui/container/inventory.png. It samples only the upper-left
176x166 pixels of the 256x256 layout (also supports HD multiples), leaving the
sheet's extra widgets unused. Slot hit targets remain active over the printed
borders; crafting/result controls align with their sheet locations. The player
preview fills the black window and looks toward the mouse relative to that window.
Missing artwork retains the procedural panel. Tests cover both paths, HD cropping,
clickable slots, emitted canvas triangles, skin UVs and all allowed player poses;
actual Unity rendering needs Play Mode.

## Crafting reference and validation

`beta-crafting.tsv` is independently extracted from `CraftingManager.java` and
its seven recipe families in reference commit
`740c583901e1ff1150e9ef37e37dab5bc0e4f807`. Regenerate it with
`python Tools/CreativeChecks/extract_crafting_reference.py /path/to/mc_b1.7.3_release`.
The inventory runner compares all 151 definitions against this fixture and tests
both grid sizes, every valid shaped offset and mirror, shapeless order, extra
inputs, recipe consumption, empty milk buckets, output capacity, shift repeats,
full-inventory closure, save recovery, durability preservation, reach/removal and
disconnect closure. UI checks use native/HD container textures and procedural
fallbacks. These checks mock Unity UI and transport; actual Unity rendering and
multiplayer still need Play Mode validation.

The table uses `PlayerHudAssets.craftingTable`, linked to
`Assets/Textures/Minecraft/gui/container/crafting_table.png`. Startup binds a
missing reference without replacing a custom assignment. The 176x166 panel is
cropped from the 256x256 atlas and uses the original ContainerWorkbench slot
coordinates. Inventory/hotbar slots move into the table panel and return when it
closes; the existing TMI sidebar remains available. No modern recipe book or
outside-click item dropping is added.
