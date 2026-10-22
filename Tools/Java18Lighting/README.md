# Java Edition 1.8 lighting

Enabled by default through `World.java18Lighting`, explicitly enabled in the
World scene. Gamma zero corresponds to Moody; one uses the vanilla Bright
curve. Disable the option to return to the previous Beta lighting. Changing
the style rebuilds existing meshes; changing time/gamma does not in 1.8 mode.
The existing material Smooth Lighting toggle controls terrain corner AO.

The 16x16 RGB lightmap combines independent sky/block values, warm block light,
cooler night skylight, vanilla gamma shaping, eight-bit quantization and subtle
torch flicker. It updates at most once per rendered frame, normally at 20 Hz.
The Nether uses its fixed celestial angle and its own ambient brightness table.
Flicker uses a separate visual RNG and cannot affect generated terrain or mobs.

Chunk meshes store raw sky/block coordinates and separate vertex AO. The shader
interpolates coordinates before sampling the bilinear lightmap in gamma space,
then converts back for Unity Linear projects. The same map supplies the hand,
held/dropped items, mobs, projectiles, multiplayer player meshes and chests.
Non-cube special models retain their individual face shading. Each world reuses
one small owned texture and releases it on destruction; only the active world
publishes shader globals. Existing texture/block data assets are untouched.

Run:

```
python3 Tools/Java18Lighting/run.py --mcs mcs --mono mono
python3 Tools/LightingParity/run.py --mcs mcs --mono mono
```

The first suite compares the production C# map against 18,432 independently
evaluated Java samples, then compiles the production World_Lighting and
LightTextureCreator against Unity doubles to check updates, ownership and
remesh behavior. LightingParity checks all six modern face orientations,
independent light levels, corner occlusion, emitting blocks and the retained
Beta reference fixtures.

Reference: 1.8-era `EntityRenderer.updateLightmap`, `World.getSunBrightness`,
`WorldProvider.generateLightBrightnessTable` and `WorldProviderHell`:
https://github.com/Marcelektro/MavenMCP-1.8.9/tree/master/src/main/java/net/minecraft

This implements the clear-weather 1.8 lighting appearance within the project's
existing Beta world. It does not add weather/lightning, night vision, the End,
boss darkening or a full replacement of every special-model AO rule. Sky/fog
and biome colors retain the project's existing systems. Unity shader compilation,
actual screenshots and live multiplayer still need engine verification.

Play checks: daylight grass/stone, enclosed unlit cave, torch against a cave
wall, mixed sunlight/torches, sunset/night, each gamma endpoint, slabs/stairs,
water/lava, fences/torches, hand/items/chest/mobs, Nether travel and world reload.
