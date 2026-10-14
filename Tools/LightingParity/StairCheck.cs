using System;
using UnityEngine;
public static partial class LightingCheck
{
 static void CheckStairMeshes()
 {
  foreach(var type in new[]{BlockType.WoodenStairs,BlockType.CobblestoneStairs})
  for(byte facing=0;facing<4;facing++)
  {
   var chunk=new ChunkData();var p=new Vector3Int(2,10,3);
   var block=new Block{type=type,sky=0,light=0,chunkData=chunk,localChunkPosition=p};
   block.blockShape.orientation=StairGeometry.Shape(facing);chunk.blocks[p]=block;
   chunk.blocks[p+Vector3Int.up]=new Block{type=BlockType.Air,sky=15,chunkData=chunk};
   BlockDataManager.blockTypeDataDictionary[(int)type].isTransparent=false;
   var data=BlockHelper.GetMeshData(chunk,p,new MeshData(true),type);
   Check(data.vertices.Count==44&&data.triangles.Count==66,"stair exterior is eleven quads without internal touching faces");
   Check(data.uv.Count==44&&data.sides.Count==44&&data.colors.Count==44&&data.skyLight.Count==44,"stair vertex streams match");
   var high=StairGeometry.CollisionPart(facing,1);
   foreach(var v in data.vertices)if(v.y>p.y+.9f)
    Check(v.x>=p.x+high.xMin/16f-1e-3&&v.x<=p.x+high.xMax/16f+1e-3&&v.z>=p.z+high.zMin/16f-1e-3&&v.z<=p.z+high.zMax/16f+1e-3,"upper stair mesh occupies correct oriented half");
   bool litTread=false;
   for(int i=0;i<data.vertices.Count;i++)if(Math.Abs(data.vertices[i].y-p.y-.5005f)<1e-4&&data.sides[i].y>.99f)litTread=true;
   Check(litTread,"stair inset tread borrows surrounding light instead of being black");
   var icon=new MeshData(true);
   foreach(Direction dir in Enum.GetValues(typeof(Direction)))BlockHelper.GetFaceDataIn(dir,new Vector3Int(),icon,block,BlockDataManager.blockTypeDataDictionary[(int)type],null);
   Check(icon.vertices.Count==44&&icon.uv.Count==44&&icon.colors.Count==44,"inventory uses same oriented stair model");
   // Stair bottoms cover the whole footprint; tops and side notches do not.
   Check(StairGeometry.CoversFace(facing,BlockShapesList.FullBlock,Direction.down),"stair bottom covers full face");
   Check(!StairGeometry.CoversFace(facing,BlockShapesList.FullBlock,Direction.up),"stair top notch never occludes full cube bottom");
   var riser=facing==0?Direction.left:facing==1?Direction.right:facing==2?Direction.backwards:Direction.forwards;
   var uvFace=StairGeometry.RenderFace(facing,1,riser).FaceUVTransform(riser);
   Check(uvFace.y==.5f&&uvFace.w==.5f,"upper stair sides crop upper half of artwork");
  }
  foreach(var slab in new[]{BlockType.WoodenSlab,BlockType.StoneSlab,BlockType.SandstoneSlab,BlockType.CobblestoneSlab})
  {
   var chunk=new ChunkData();var p=new Vector3Int(0,10,0);var block=new Block{type=slab,chunkData=chunk,localChunkPosition=p};block.blockShape.shape=BlockShapesList.Slab;chunk.blocks[p]=block;
   chunk.blocks[p+Vector3Int.up]=new Block{type=BlockType.Air,sky=15,chunkData=chunk};
   var data=new MeshData(true);BlockHelper.GetFaceDataIn(Direction.up,p,data,block,BlockDataManager.blockTypeDataDictionary[(int)slab],chunk);
   Check(data.sides[0].y>.99f,"every slab material borrows light above/sides for inset top");
   Check(BlockShapesList.Slab.FaceUVTransform(Direction.backwards).y==.5f,"slab side texture remains cropped to half height");
  }
 }
}
