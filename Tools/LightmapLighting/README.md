# Colored voxel lightmap

The renderer has one lighting path: separate sky and block light channels, a
colored dynamic lightmap, clock-driven sunlight, cool nights, warm flickering
block light, smooth vertex occlusion, and gamma shaping.

Chunk meshes store raw sky/block coordinates and vertex occlusion. The shader
interpolates those coordinates before sampling the bilinear lightmap in display
color space, then converts back for Unity Linear projects. The same lightmap
supplies terrain, the hand, held and dropped items, mobs, projectiles, player
meshes, and chests. Each world owns one reusable 16x16 texture; only the active
dimension publishes it and the texture is released with its world.

Run:

```text
python3 Tools/LightmapLighting/run.py --mcs mcs --mono mono
python3 Tools/LightingParity/run.py --mcs mcs --mono mono
```

The first suite compares the production lightmap math against 18,432 reference
samples, then checks texture reuse, clock and gamma updates, ownership, and
active-dimension isolation with Unity doubles. LightingParity covers all six
face orientations, independent light levels, corner occlusion, emitting blocks,
special models, transport, fluids, and biome colors.

Engine play checks remain useful for daylight terrain, enclosed caves, mixed
sun and block light, sunset/night, gamma endpoints, slabs/stairs, fluids,
special models, entities, dimension travel, and world reloads.
