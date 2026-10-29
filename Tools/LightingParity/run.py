#!/usr/bin/env python3
import argparse,subprocess,tempfile
from PIL import Image
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');ap.add_argument('--stair-check',type=Path);a=ap.parse_args()
 with tempfile.TemporaryDirectory() as t:
  t=Path(t);helper=(ROOT/'Assets/_Scripts/World/Rendering/BlockHelper.cs').read_text()
  pipeline=helper[helper.index('    public static MeshData GetMeshData'):helper.index('    public static void GetFaceLightingAndAO')]
  method=pipeline+helper[helper.index('    public static void GetFaceLightingAndAO'):helper.index('    public static int GetLightIndex')]
  fluid=(ROOT/'Assets/_Scripts/World/World_Water.cs').read_text()
  materials=fluid[fluid.index('    public static bool FluidHasSolidMaterial'):fluid.index('    public void SetLavaCell')]
  (t/'BlockHelper.cs').write_text('using System;using UnityEngine;public static class BlockHelper{public static readonly Direction[] directions=(Direction[])Enum.GetValues(typeof(Direction));'+method+'}public partial class World{'+materials+'}')
  section=(ROOT/'Assets/_Scripts/World/ChunkSection.cs').read_text();axes=section[section.index('    public static void ChooseFaceAxes'):]
  (t/'ChunkSection.cs').write_text('using UnityEngine;public class ChunkSection{'+axes)
  sources=['Assets/_Scripts/World/VoxelLightingMath.cs','Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/StairGeometry.cs','Assets/_Scripts/Block/SpecialBlockGeometry.cs','Assets/_Scripts/World/Rendering/SpecialBlockMesh.cs','Assets/_Scripts/SpecialBlockTextures.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs','Assets/_Scripts/Block/BlockShape.cs','Assets/_Scripts/Block/BlockShapes.cs','Assets/_Scripts/World/Rendering/PlantMesh.cs','Tools/LightingParity/PlantCheck.cs','Tools/LightingParity/StairCheck.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Assets/_Scripts/World/Lighting.cs','Assets/_Scripts/World/WaterSurface.cs','Assets/_Scripts/World/WaterSimulation.cs','Tools/LightingParity/LightingCheck.cs']
  sources.append('Assets/_Scripts/WheatCropRules.cs')
  sources.append('Assets/_Scripts/World/World_RenderInvalidation.cs')
  binary=t/'lighting.exe'
  sources.append('Tools/LightingParity/SpecialMeshCheck.cs')
  sources.append('Tools/LightingParity/FaceLightingCheck.cs')
  sources.append('Assets/_Scripts/World/FluidTickScheduler.cs')
  sources.extend(['Assets/_Scripts/World/Rendering/BiomeColorPalette.cs','Assets/_Scripts/World/Rendering/BiomeBlockColors.cs','Assets/_Scripts/WorldGeneration/Beta/BetaClimate.cs','Assets/_Scripts/WorldGeneration/Beta/JavaRandom.cs','Tools/LightingParity/BiomeColorCheck.cs'])
  sources.append('Assets/_Scripts/WorldGeneration/Beta/BetaMathHelper.cs')
  if a.stair_check:
   sources.remove('Tools/LightingParity/StairCheck.cs');sources.append(str(a.stair_check.resolve()))
  subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(ROOT/s) for s in sources],str(t/'BlockHelper.cs'),str(t/'ChunkSection.cs')],check=True)
  maps=t/'colormaps.rgba'
  maps.write_bytes(b''.join(Image.open(ROOT/('Assets/Textures/Minecraft/'+name+'.png')).convert('RGBA').transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes() for name in ['grass','foliage']))
  subprocess.run([a.mono,str(binary),str(maps)],check=True)
if __name__=='__main__':main()
