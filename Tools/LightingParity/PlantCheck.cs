using System;
using UnityEngine;
public static partial class LightingCheck
{
 static void CheckPlants()
 {
  int count=0;
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
  {
   if(!BlockShapesExtensions.IsPlant(type))continue;
   count++;
   var chunk=new ChunkData();var pos=new Vector3Int(2,10,3);
   var block=new Block{type=type,sky=12,light=3,chunkData=chunk,localChunkPosition=pos};
   block.blockShape.shape=BlockShapesExtensions.ForBlock(type).GetShape();
   chunk.blocks[pos]=block;
   // Force bad opaque catalog settings: models must still use the cutout pass.
   BlockDataManager.blockTypeDataDictionary[(int)type].isTransparent=false;
   var main=BlockHelper.GetMeshData(chunk,pos,new MeshData(true),type);
   var mesh=main.transparentMesh;int vertices=BlockShapesExtensions.IsCrop(type)?32:16;
   Check(mesh.triangles.Count==vertices/4*6,"plant triangle streams align");
   Check(main.vertices.Count==0&&mesh.vertices.Count==vertices,"plant uses correct sheets and cutout submesh");
   Check(mesh.uv.Count==vertices&&mesh.colors.Count==vertices&&mesh.faceCoordinates.Count==vertices&&mesh.sides.Count==vertices&&mesh.skyLight.Count==vertices&&mesh.blockLight.Count==vertices&&mesh.smoothLightCorners.Count==vertices,"all plant vertex streams align");
   for(int i=0;i<vertices;i++)
   {
    Check(mesh.sides[i].z==3&&mesh.skyLight[i]==12&&mesh.blockLight[i]==3,"flat own-cell plant lighting marker");
    Check(Math.Abs(mesh.sides[i].x-Beta173Lighting.Brightness(12,3,0,0))<1e-6,"plant brightness ignores cube AO");
   }
   var stonePos=pos+Vector3Int.down;
   chunk.blocks[stonePos]=new Block{type=BlockType.Stone,chunkData=chunk,localChunkPosition=stonePos};
   Check(BlockHelper.GetMeshData(chunk,stonePos,new MeshData(true),BlockType.Stone).vertices.Count==24,"plant never occludes neighboring cube faces even if asset opaque");
   main.PrepareForRendering();
   Check(main.vertices.Count==vertices&&main.lightingUVs.Count==vertices&&main.colors.Count==vertices,"plant survives merged mesh upload with matching streams");
   // Same model emitted once when inventory calls all six directions.
   var icon=new MeshData(true);
   foreach(Direction dir in Enum.GetValues(typeof(Direction)))BlockHelper.GetFaceDataIn(dir,new Vector3Int(),icon,block,BlockDataManager.blockTypeDataDictionary[(int)type],null);
   Check(icon.vertices.Count==vertices&&icon.uv.Count==vertices&&icon.skyLight.Count==0,"inventory model emitted once without world lighting");
   // Reverse quad normals must oppose each other, with full image UVs on each.
   for(int i=0;i<vertices;i+=8)
   {
    var a=mesh.vertices[i];var b=mesh.vertices[i+3];var reverseA=mesh.vertices[i+4];var reverseB=mesh.vertices[i+7];
    Check(Math.Abs(a.x-reverseB.x)<1e-6&&Math.Abs(a.z-reverseB.z)<1e-6&&Math.Abs(b.x-reverseA.x)<1e-6&&Math.Abs(b.z-reverseA.z)<1e-6,"reverse winding covers back face");
    Check(mesh.uv[i].x==0&&mesh.uv[i+2].x==1&&mesh.uv[i+2].y==1,"full sheet image, no selection-bound UV crop");
   }
  }
  Check(count==20,"all twenty plants and crop stages classified");
  var cropData=BlockDataManager.blockTypeDataDictionary[(int)WheatCropRules.Crop];
  cropData.textureData.growthStageIndices=new[]{30,31,32,33,34,35,36,37};
  for(byte age=0;age<8;age++)
  {
   var chunk=new ChunkData();var pos=new Vector3Int(2,10,3);
   var block=new Block{type=WheatCropRules.Crop,sky=15,chunkData=chunk,localChunkPosition=pos};
   block.blockShape.orientation=SpecialBlockGeometry.Shape(block.type,age);block.blockShape.shape=block.blockShape.orientation.GetShape();chunk.blocks[pos]=block;
   var mesh=BlockHelper.GetMeshData(chunk,pos,new MeshData(true),block.type).transparentMesh;
   Check(mesh.vertices.Count==32,"every wheat age retains native four-row geometry");
   foreach(var uv in mesh.uv)Check(uv.z==30+age,"world mesh samples metadata-selected wheat stage image");
  }
  cropData.textureData.growthStageIndices=null;
  var crop=new MeshData(true);PlantMesh.Append(BlockType.WheatCrop7,new Vector3Int(),new Vector3Int(),false,17,new Color(),crop);
  Check(crop.vertices[0].x==.25f&&crop.vertices[8].x==.75f&&crop.vertices[16].z==.25f&&crop.vertices[24].z==.75f,"wheat has four parallel rows");
  Check(crop.vertices[0].y==-.0625f&&crop.vertices[1].y==.9375f,"wheat lowered one pixel into farmland");
  foreach(var uv in crop.uv)Check(uv.z==17,"assigned image layer retained on every crop sheet");
  var flower=new MeshData(true);PlantMesh.Append(BlockType.Rose,new Vector3Int(),new Vector3Int(),true,1,new Color(),flower);
  Check(Math.Abs(flower.vertices[0].x-.05f)<1e-6&&Math.Abs(flower.vertices[2].x-.95f)<1e-6,"crossed sheets span Beta .9 width");
  var grass=new MeshData(true);PlantMesh.Append(BlockType.Fern,new Vector3Int(),new Vector3Int(100,64,-200),true,1,new Color(),grass);
  var offset=PlantMesh.GrassOffset(new Vector3Int(100,64,-200));
  Check(Math.Abs(grass.vertices[0].x-.05f-offset.x)<1e-6&&Math.Abs(grass.vertices[0].y-offset.y)<1e-6,"grass model applies global position offset");
  var iconGrass=new MeshData(true);PlantMesh.Append(BlockType.Fern,new Vector3Int(),new Vector3Int(100,64,-200),false,1,new Color(),iconGrass);
  Check(Math.Abs(iconGrass.vertices[0].x-.05f)<1e-6&&iconGrass.vertices[0].y==0,"inventory grass centered without world offset");
  {var o=PlantMesh.GrassOffset(new Vector3Int(0,0,0));Check(Math.Abs(o.x-(-0.25f))<1e-6&&Math.Abs(o.y-(-0.2f))<1e-6&&Math.Abs(o.z-(-0.25f))<1e-6,"Java signed overflow grass-offset fixture");}
  {var o=PlantMesh.GrassOffset(new Vector3Int(100,64,-200));Check(Math.Abs(o.x-(0.04999999999999999f))<1e-6&&Math.Abs(o.y-(-0.09333333333333334f))<1e-6&&Math.Abs(o.z-(-0.11666666666666667f))<1e-6,"Java signed overflow grass-offset fixture");}
  {var o=PlantMesh.GrassOffset(new Vector3Int(-17,70,16));Check(Math.Abs(o.x-(0.016666666666666663f))<1e-6&&Math.Abs(o.y-(-0.12f))<1e-6&&Math.Abs(o.z-(0.016666666666666663f))<1e-6,"Java signed overflow grass-offset fixture");}
  {var o=PlantMesh.GrassOffset(new Vector3Int(1000000,32,-1000000));Check(Math.Abs(o.x-(-0.04999999999999999f))<1e-6&&Math.Abs(o.y-(-0.18666666666666668f))<1e-6&&Math.Abs(o.z-(-0.18333333333333335f))<1e-6,"Java signed overflow grass-offset fixture");}
  {var o=PlantMesh.GrassOffset(new Vector3Int(2147483647,64,-2147483648));Check(Math.Abs(o.x-(0.18333333333333335f))<1e-6&&Math.Abs(o.y-(-0.17333333333333334f))<1e-6&&Math.Abs(o.z-(-0.08333333333333334f))<1e-6,"Java signed overflow grass-offset fixture");}
 }
}
