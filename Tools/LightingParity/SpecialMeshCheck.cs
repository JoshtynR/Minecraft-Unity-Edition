using System;
using System.Linq;
using UnityEngine;
public static partial class LightingCheck
{
 static void CheckSpecialMeshes()
 {
  {
   var chunk=new ChunkData();var p=new Vector3Int(0,10,0);
   for(int x=-1;x<=2;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)Add(chunk,p+new Vector3Int(x,y,z),BlockType.Air).sky=15;
   var lava=Add(chunk,p,BlockType.Lava);lava.waterLevel=6;lava.BlockData.lightEmission=15;
   BlockDataManager.blockTypeDataDictionary[(int)BlockType.Lava].isTransparent=true;
   var data=BlockHelper.GetMeshData(chunk,p,new MeshData(true),BlockType.Lava);
   Check(data.vertices.Count==24&&data.transparentMesh.vertices.Count==0&&data.waterMesh.vertices.Count==0,"lava is opaque fluid geometry even with transparent catalog flag");
   Check(data.vertices.Exists(v=>v.y>p.y+.01f&&v.y<p.y+.9f),"lava mesh is deformed before lighting");
   var other=Add(chunk,p+Vector3Int.right,BlockType.Lava);other.waterLevel=4;
   data=BlockHelper.GetMeshData(chunk,p,new MeshData(true),BlockType.Lava);
   Check(data.vertices.Count==20,"shared lava face is culled even with nonopaque material");
  }
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))if(SpecialBlockGeometry.Handles(type))
  for(byte s=0;s<16;s++)
  {
   var chunk=new ChunkData();var p=new Vector3Int(2,10,3);
   for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)Add(chunk,p+new Vector3Int(x,y,z),BlockType.Air).sky=15;
   var b=Add(chunk,p,type);b.sky=SpecialBlockGeometry.NonOccluding(type)?15:0;
   b.blockShape.orientation=SpecialBlockGeometry.Shape(type,s);b.blockShape.shape=b.blockShape.orientation.GetShape();
   BlockDataManager.blockTypeDataDictionary[(int)type].isTransparent=false;
   var main=BlockHelper.GetMeshData(chunk,p,new MeshData(true),type);
   var mesh=SpecialBlockGeometry.NonOccluding(type)?main.transparentMesh:main;
   int count=mesh.vertices.Count;
   Check(count==mesh.colors.Count&&count==mesh.uv.Count&&count==mesh.skyLight.Count&&count==mesh.blockLight.Count&&count==mesh.sides.Count&&count==mesh.smoothLightCorners.Count&&count==mesh.faceCoordinates.Count,"all special world render channels align: "+type);
   if(type==BlockType.Furnace)Check(mesh.sides.All(v=>v.x>=.5f),"opaque directional blocks sample outside their dark cell");
   var icon=new MeshData(true);
   foreach(Direction dir in Enum.GetValues(typeof(Direction)))BlockHelper.GetFaceDataIn(dir,new Vector3Int(),icon,b,b.BlockData,null);
   Check(icon.vertices.Count==count&&icon.uv.Count==count&&icon.colors.Count==count,"special inventory emits one complete model: "+type);
   main.PrepareForRendering();Check(main.vertices.Count==main.lightingUVs.Count,"special submesh merge retains lighting streams");
  }
  foreach(var type in new[]{BlockType.Torch,BlockType.Fence,BlockType.RedstoneWire,BlockType.Ladder,BlockType.WoodenDoor})
  {
   var chunk=new ChunkData();var p=new Vector3Int(0,10,0);var neighbor=p+Vector3Int.up;
   Add(chunk,p,BlockType.Stone);var b=Add(chunk,neighbor,type);b.blockShape.orientation=SpecialBlockGeometry.Shape(type,SpecialBlockGeometry.DefaultState(type));b.blockShape.shape=b.blockShape.orientation.GetShape();
   Check(BlockHelper.GetMeshData(chunk,p,new MeshData(true),BlockType.Stone).vertices.Count==24,"special non-cubes never hide solid neighbor face even if asset is opaque");
  }
 }
}
