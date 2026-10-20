using System;
using System.IO;
using UnityEngine;

public static partial class LightingCheck
{
 static int RGB(Color c)=>(int)Math.Round(c.r*255)<<16|(int)Math.Round(c.g*255)<<8|(int)Math.Round(c.b*255);
 static Color32[] ReadMap(byte[] raw,int offset)
 {
  var map=new Color32[65536];
  for(int i=0;i<map.Length;i++)map[i]=new Color32(raw[offset+i*4],raw[offset+i*4+1],raw[offset+i*4+2],raw[offset+i*4+3]);
  return map;
 }
 static void CheckBiomeColors(string mapPath)
 {
  int before=checks;
  var raw=File.ReadAllBytes(mapPath);
  var grass=ReadMap(raw,0);var foliage=ReadMap(raw,65536*4);
  BiomeColorPalette.Initialize(grass,foliage);
  var palette=BiomeColorPalette.Current;
  Check(BiomeColorPalette.LookupIndex(1,1)==0,"hot wet samples top left");
  Check(BiomeColorPalette.LookupIndex(1,0)==65280,"hot dry samples bottom left");
  Check(BiomeColorPalette.LookupIndex(0,1)==65535,"cold samples bottom right");
  Check(BiomeColorPalette.LookupIndex(.5,.5)==49023,"humidity multiplied by temperature before lookup");
  Check(BiomeColorPalette.LookupIndex(-1,2)==65535&&BiomeColorPalette.LookupIndex(2,2)==0,"clamped climate stays in map bounds");
  // Independent Java evaluations of the original BlockTallGrass coordinate hash.
  foreach(var row in new[]{new[]{0,64,0,9,27},new[]{-1,65,17,5,45},new[]{1000000,127,-1000000,1000008,-999970},new[]{int.MaxValue,0,int.MinValue,-2147483647,-2147483645}})
  {
   BiomeColorPalette.TallGrassSample(row[0],row[1],row[2],out int sx,out int sz);
   Check(sx==row[3]&&sz==row[4],"Java plant colour hash preserves int and long overflow");
  }
  var topLeft=grass[255*256];
  Check(palette.Grass(1,1)==(topLeft.r<<16|topLeft.g<<8|topLeft.b),"Unity pixel rows flipped to PNG coordinates");
  grass[255*256]=new Color32(255,0,255,0);
  Check(palette.Grass(1,1)!=(255<<16|255),"palette snapshots pixels instead of retaining writable source buffer");
  grass=ReadMap(raw,0);
  var configured=new Color(.2f,.1f,.3f,1);
  foreach(long seed in new[]{0L,12345L,-1446162294L,long.MinValue,long.MaxValue})
  foreach(var origin in new[]{new Vector3Int(0,0,0),new Vector3Int(-32,0,16),new Vector3Int(64,0,-48)})
  {
   var chunk=new ChunkData{worldPos=origin,worldRef=new World{betaWorldSeed=seed}};
   var climate=new BetaClimate(seed);
   for(int x=0;x<16;x++)for(int z=0;z<16;z++)
   {
    var pos=new Vector3Int(x,64,z);
    climate.Sample(-origin.x-x-1,origin.z+z,out double t,out double h);
    int index=BiomeColorPalette.LookupIndex(t,h),source=(255-index/256)*256+index%256;
    var g=grass[source];var f=foliage[source];
    Check(RGB(BiomeBlockColors.ForFace(BlockType.Grass,Direction.up,chunk,pos,configured))==(g.r<<16|g.g<<8|g.b),"grass world climate and reflected X match PNG");
    Check(RGB(BiomeBlockColors.ForFace(BlockType.Leaves,Direction.left,chunk,pos,configured))==(f.r<<16|f.g<<8|f.b),"oak climate matches PNG on every column");
   }
   var cache=chunk.biomeColors;
   BiomeBlockColors.ForFace(BlockType.Leaves,Direction.up,chunk,new Vector3Int(),configured);
   Check(ReferenceEquals(cache,chunk.biomeColors),"lighting rebuild reuses chunk colour cache");
   var p=new Vector3Int(7,65,11);var world=chunk.GetGlobalBlockCoords(p);
   BiomeColorPalette.TallGrassSample(-world.x-1,world.y,world.z,out int sx,out int sz);
   climate.Sample(sx,sz,out double gt,out double gh);
   foreach(var type in new[]{BlockType.TallGrass,BlockType.Fern})
    Check(RGB(BiomeBlockColors.ForFace(type,Direction.forwards,chunk,p,configured))==palette.Grass(gt,gh),"plant uses perturbed Beta climate coordinates");
   Check(RGB(BiomeBlockColors.ForFace(BlockType.SpruceLeaves,Direction.up,chunk,p,configured))==0x619961,"pine fixed Beta colour");
   Check(RGB(BiomeBlockColors.ForFace(BlockType.BirchLeaves,Direction.up,chunk,p,configured))==0x80A755,"birch fixed Beta colour");
  }
  var storage=new ChunkData{chunkSize=16,worldRef=new World{betaWorldSeed=17}};var local=new Vector3Int(3,64,5);
  var definition=BlockDataManager.blockTypeDataDictionary[(int)BlockType.Grass];var oldTint=definition.albedoTint;
  definition.albedoTint=configured;definition.textureData.parts[BlockTexturePart.GrassSideOverlay]=17;
  var block=new Block{type=BlockType.Grass,chunkData=storage,localChunkPosition=local,sky=15};storage.blocks[local]=block;
  var top=new MeshData(true);BlockHelper.GetFaceDataIn(Direction.up,local,top,block,definition,storage);
  var side=new MeshData(true);BlockHelper.GetFaceDataIn(Direction.left,local,side,block,definition,storage);
  var bottom=new MeshData(true);BlockHelper.GetFaceDataIn(Direction.down,local,bottom,block,definition,storage);
  Check(top.colors.Count==4&&side.colors.Count==8&&bottom.colors.Count==4,"grass top, dirt base and overlay have aligned colour streams");
  for(int i=0;i<4;i++)
  {
   Check(RGB(side.colors[i])==0xFFFFFF&&RGB(bottom.colors[i])==0xFFFFFF,"dirt side and bottom never receive biome green");
   Check(RGB(side.colors[i+4])==RGB(top.colors[i]),"side overlay matches top without multiplying configured green");
  }
  var savedCache=storage.biomeColors;storage.worldRef.betaWorldSeed=18;
  BiomeBlockColors.ForFace(BlockType.Leaves,Direction.up,storage,local,configured);
  Check(!ReferenceEquals(savedCache,storage.biomeColors),"changing seed invalidates cached climate");
  savedCache=storage.biomeColors;BiomeColorPalette.Initialize(grass,foliage);
  BiomeBlockColors.ForFace(BlockType.Leaves,Direction.up,storage,local,configured);
  Check(!ReferenceEquals(savedCache,storage.biomeColors),"reloading colour maps invalidates cached colours");
  Check(RGB(BiomeBlockColors.ForFace(BlockType.DryTallGrass,Direction.forwards,storage,local,configured))==0xFFFFFF,"dry grass keeps baked colour");
  Check(RGB(BiomeBlockColors.ForFace(BlockType.Stone,Direction.up,storage,local,configured))==RGB(configured),"other block tint remains user controlled");
  Check(RGB(BiomeBlockColors.ForFace(BlockType.Leaves,Direction.up,null,local,configured))==0x48B518,"inventory oak has stable Beta render colour");
  Check(RGB(BiomeBlockColors.ForFace(BlockType.Grass,Direction.up,null,local,configured))==palette.Grass(.5,1),"inventory grass uses colour map default");
  definition.albedoTint=oldTint;definition.textureData.parts.Remove(BlockTexturePart.GrassSideOverlay);
  BiomeColorPalette.Initialize(null,null);
  Console.WriteLine((checks-before)+" production biome-map/mesh/cache checks passed.");
 }
}
