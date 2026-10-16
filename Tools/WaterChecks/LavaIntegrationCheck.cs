using System;
using UnityEngine;
public static partial class IntegrationCheck
{
 private static World LavaWorld()
 {
  var w=new World();
  for(int x=-4;x<=4;x+=2)for(int z=-4;z<=4;z+=2)
  {
   var c=AddChunk(w,V(x,0,z));for(int a=0;a<2;a++)for(int b=0;b<2;b++)c.SetBlock(V(a,0,b),BlockType.Stone);
  }
  return w;
 }
 private static void CheckLavaWorld()
 {
  BlockDataManager.blockTypeDataDictionary[(int)BlockType.Lava].lightEmission=15;
  var w=LavaWorld();var c=w.worldData.chunkDataDict[V(0,0,0)];
  for(byte level=0;level<16;level++)
  {
   c.SetBlock(V(0,2,0),BlockType.Lava);c.GetBlock(V(0,2,0)).waterLevel=level;
   c.SetBlock(V(1,2,0),BlockType.Lava);c.GetBlock(V(1,2,0)).waterLevel=(byte)((level+1)&15);
   var save=new ChunkSectionSaveData(c.sections[1]);var restored=ChunkSection.Deserialize(save,c);
   WaterCheck.Assert(save.blocks[0].length==1&&save.blocks[0].waterLevel==level,"lava RLE separates unlike levels");
   WaterCheck.Assert(restored.blocks[0,0,0].type==BlockType.Lava&&restored.blocks[0,0,0].waterLevel==level&&restored.blocks[1,0,0].waterLevel==((level+1)&15),"all lava levels survive actual section load");
   WaterCheck.Assert(new Block(c.GetBlock(V(0,2,0))).waterLevel==level,"lava copies keep their flow level");
  }
  var legacy=new ChunkSectionSaveData{yOffset=0,blocks=new[]{new BlockSaveData{type=BlockType.Lava,length=8}}};
  WaterCheck.Assert(ChunkSection.Deserialize(legacy,c).blocks[0,0,0].waterLevel==0,"older lava saves remain sources");
  for(int x=0;x<2;x++)for(int z=0;z<2;z++)
  {c.SetBlock(V(x,1,z),BlockType.Lava);c.GetBlock(V(x,1,z)).waterLevel=6;c.SetBlock(V(x,2,z),BlockType.Air);}
  var mesh=new MeshData(false);BlockShapesList.FullBlock.SetFaceVertices(Direction.up,V(0,1,0),mesh);WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
  WaterCheck.Assert(Math.Abs(mesh.vertices[1].y-1-.0005f-2/9f)<1e-5,"lava mesh uses weighted flow corner heights");

  Mirror.NetworkServer.active=true;WorldServer.instance=new WorldServer();w=LavaWorld();
  w.SetLavaCell(new WaterPosition(0,2,0),8);w.TestUpdate();
  WaterCheck.Assert(WorldServer.instance.lastLevels[0]==40&&w.GetLavaCell(new WaterPosition(0,2,0))==8,"server emits encoded falling lava state");
  w.SetLavaCell(new WaterPosition(0,1,0),0);w.SetWaterCell(new WaterPosition(1,1,0),0);w.TestUpdate();
  var hardened=w.GetBlock(V(0,1,0));
  WaterCheck.Assert(hardened.type==BlockType.Obsidian&&w.GetBlock(V(1,1,0)).type==BlockType.Water,"real world adapter hardens source lava without consuming water");
  bool sent=false;for(int i=0;i<WorldServer.instance.lastPositions.Length;i++)if(WorldServer.instance.lastPositions[i].Equals(V(0,1,0)))sent=WorldServer.instance.lastLevels[i]==64;
  WaterCheck.Assert(sent,"server batch sends obsidian result instead of stale lava state");
  WaterCheck.Assert(hardened.GetBlockLight()==0&&hardened.chunkData.blockLightRemoveQueue.Count>0,"hardening removes emitted lava light");
  w.SetLavaCell(new WaterPosition(0,1,0),3);w.TestUpdate();
  WaterCheck.Assert(w.GetBlock(V(0,1,0)).type==BlockType.Cobblestone,"flowing lava becomes cobblestone through world notifications");
  w.SetLavaCell(new WaterPosition(0,1,0),8);w.TestUpdate();
  WaterCheck.Assert(w.GetBlock(V(0,1,0)).type==BlockType.Lava,"falling lava is not incorrectly hardened by adjacent water");
  foreach(var t in new[]{BlockType.Dandelion,BlockType.Torch,BlockType.RedstoneWire,BlockType.SnowLayer})
  {
   var p=V(0,2,0);WorldDataHelper.SetBlock(w,p,t);
   WaterCheck.Assert(w.GetWaterCell(new WaterPosition(0,2,0))==WaterSimulation.Air&&w.GetLavaCell(new WaterPosition(0,2,0))==WaterSimulation.Air,"fluids replace canonical nonblocking decoration "+t);
  }
  foreach(var t in new[]{BlockType.SugarCane,BlockType.Ladder,BlockType.WoodenDoor,BlockType.StandingSign,BlockType.Cobweb,BlockType.StoneSlab,BlockType.StonePressurePlate,BlockType.WoodenPressurePlate})
  {
   WorldDataHelper.SetBlock(w,V(0,2,0),t);
   WaterCheck.Assert(w.GetWaterCell(new WaterPosition(0,2,0))==WaterSimulation.Solid&&w.GetLavaCell(new WaterPosition(0,2,0))==WaterSimulation.Solid,"Beta material/exception flow blocker "+t);
  }

  Mirror.NetworkServer.active=false;var client=LavaWorld();
  client.ReceiveWaterState(V(0,1,0),46);WaterCheck.Assert(client.GetBlock(V(0,1,0)).type==BlockType.Lava&&client.GetBlock(V(0,1,0)).waterLevel==14,"client decodes falling lava");
  client.ReceiveWaterState(V(0,1,0),65);WaterCheck.Assert(client.GetBlock(V(0,1,0)).type==BlockType.Cobblestone,"client applies hardening product");
  client.ReceiveWaterState(V(0,1,0),64);WaterCheck.Assert(client.GetBlock(V(0,1,0)).type==BlockType.Obsidian,"client decodes obsidian");
  client.ReceiveWaterState(V(0,1,0),255);WaterCheck.Assert(client.GetBlock(V(0,1,0)).type==BlockType.Air,"client removes drained lava");
  client.ReceiveWaterState(V(10,1,0),35);client.ReceiveWaterState(V(11,1,0),64);var late=AddChunk(client,V(10,0,0));client.TestActivate(late);client.TestUpdate();
  WaterCheck.Assert(client.GetBlock(V(10,1,0)).type==BlockType.Lava&&client.GetBlock(V(10,1,0)).waterLevel==3&&client.GetBlock(V(11,1,0)).type==BlockType.Obsidian,"lava and hardening deltas survive chunk-load ordering");
  client.ReceiveWaterState(V(0,1,0),32);client.ReceiveWaterState(V(1,1,0),0);
  foreach(byte invalid in new byte[]{16,31,48,63,66,254})client.ReceiveWaterState(V(0,1,0),invalid);
  for(int i=0;i<100;i++)client.TestUpdate();
  WaterCheck.Assert(client.GetBlock(V(0,1,0)).type==BlockType.Lava,"client neither accepts unknown fluid codes nor independently simulates lava/reactions");
  var deferred=LavaWorld();deferred.TestRequirePopulation(true);
  var contributors=new[]{deferred.worldData.chunkDataDict[V(0,0,0)],deferred.worldData.chunkDataDict[V(2,0,0)],deferred.worldData.chunkDataDict[V(0,0,-2)],deferred.worldData.chunkDataDict[V(2,0,-2)]};
  deferred.ReceiveWaterState(V(0,1,0),38);deferred.TestActivate(contributors[0]);deferred.TestUpdate();
  WaterCheck.Assert(deferred.GetBlock(V(0,1,0)).type==BlockType.Air,"client holds fluid delta behind incomplete population");
  foreach(var owner in contributors)owner.betaPopulationDone=true;
  deferred.TestActivate(contributors[0]);deferred.TestUpdate();
  WaterCheck.Assert(deferred.GetBlock(V(0,1,0)).type==BlockType.Lava&&deferred.GetBlock(V(0,1,0)).waterLevel==6,"population-gated activation retains and later applies queued lava delta");
  Mirror.NetworkServer.active=true;
 }
}
