# Redstone rendering and connectivity audit

Reference: `jacobo-mc/mc_b1.7.3_release`, client `RenderBlocks.renderBlockRedstoneWire`
and `BlockRedstoneWire.isPowerProviderOrWire`.

## Implemented

- Isolated dust retains the Beta cross topology, not a modern toggleable dot.
- One-axis connections use a full straight strip, including single-ended joins.
- Elbows, T-junctions and crosses use connected arms with the Beta 5/16 crop.
- Surface height is 1/64; climbing strips use the existing 1.021875 height.
- Solid overhead blocks prevent upward joins. Non-solid neighbours allow
  downward joins. Only actual upper dust produces the climbing strip.
- Repeaters use Beta's directional output-side remap, independent of delay
  bits; the previous both-ends-of-axis approximation was incorrect.
- Both redstone torch states retain connection eligibility. Ordinary torches
  and unrelated solid blocks are not wire connections.
- The existing strength-dependent colour formula matches Beta's RGB formula.
- Legacy cross/line artwork remains supported and takes precedence when bound.
- Modern dot/line0/line1 artwork is composed into the classic topology instead
  of treating a line sprite as a complete cross. Optional line overlays use the
  same cropped UVs and orientation. This is texture adaptation, not a claim
  that modern artwork becomes pixel-identical to Beta's terrain atlas.

The new parts are appended to the enum, preserving all existing serialized IDs.
Only the redstone-wire special-image bindings were added to Block Data; existing
face assignments and all other blocks are untouched. The supplied dot is copied
unchanged; the other three supplied PNGs already match repository files exactly.

## Functional gaps found during review

The current project stores dust strength, torch/repeater variants, and component
state, but has no general redstone signal-update engine. Lever toggles and
repeater delay selection do not establish complete working circuits by themselves.
This rendering/connectivity correction does not implement those missing systems.

A complete Beta functionality pass still needs weak/strong directional power,
15-level dust propagation and removal, solid-block conduction, scheduled torch
inversion/burnout, repeater delays, buttons/pressure plates, and updates to
consumers such as doors, pistons, TNT and powered rails. Those require dedicated
simulation, persistence/network validation and reference fixtures—not rendering
changes or a proximity scan that simply lights every nearby wire.

## Validation

`python3 Tools/ShapeChecks/run.py` covers all sixteen connection masks with both
legacy and modern parts, UV/layer consistency, all repeater facing/delay states,
and climbing/overhead rules. Unity Play Mode is still required to verify the
actual imported textures, shaders and appearance.
