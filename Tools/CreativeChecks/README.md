# Survival inventory with Too Many Items sidebar

E opens one menu: the 176x166 Beta survival panel and the right-hand block grid
are always visible together. Layout follows the original ContainerPlayer slot
coordinates (27 backpack cells and nine hotbar cells, with 18-pixel pitch).
Four crafting inputs, a read-only result, a skinned player preview and four
equipment positions occupy the upper area. The project has block types but no
armor items. Equipment rejects ordinary blocks while accepting Beta's pumpkin
helmet exception (limit one), which also appears on the preview. Armor items/stats
and the first-person pumpkin mask remain separate future systems.

The sidebar uses the item catalogue: 131 block counterparts and eight standalone
items, fitting the scene's normal 1080p/3x GUI scale. Smaller windows use pages
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
- The current block registry supports log/spruce-log/birch-log to four planks,
  crafting tables, sandstone, wooden/stone pressure plates, the two-stone vertical
  Beta button recipe, and pumpkin-over-torch jack-o'-lanterns. Result clicks
  consume ingredients only after the whole output fits; shift repeats while
  ingredients and space remain. Two vertical planks now yield four sticks, and
  coal or charcoal above a stick yields four torches. A 3x3 crafting table,
  tool actions, food actions and editable recipe assets remain future work.
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
This separate catalogue contains ItemDefinition entries: stable ItemType ID,
display name, individual Texture2D icon, maximum stack, maximum damage and optional
placedBlock. Block Data is untouched. Blank block icons reuse the existing mesh
and textures; standalone entries with no image show two-letter placeholders.
The eight initial standalone entries are Stick, Coal, Charcoal, IronIngot,
GoldIngot, Diamond, RedstoneDust and Flint. Their material definitions and catalogue
grants exist; new ore-drop rules and furnace smelting are not implemented here.

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
bar is added yet. Bed/cake/doors default to one, signs sixteen, other current
entries sixty-four. Oversized old stacks are split into available slots/recovery.

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
and cursor versus shift-crafting capacity.
Unity shader compilation, Mirror weaving and live pointer/rendering still require
Play Mode validation. The skinned cuboid preview is a lightweight separate model;
it does not clone a networked player or allocate an extra camera/render texture.
