using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ShapeCheck
{
 static void CheckSpecialTextures()
 {
  CheckWireAndRailConnections();
  int start=checks;
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
  {
   var parts=SpecialTextureLayout.Parts(type);
   if(parts.Length==0||!SpecialBlockGeometry.Handles(type))continue;
   var used=new HashSet<int>();
   Func<BlockTexturePart,int> image=p=>Array.IndexOf(parts,p)<0?0:100+(int)p;
   var neighbors=new List<Func<Vector3Int,BlockType>>{null,
    p=>p.x==-1&&p.y==0&&p.z==0?type:BlockType.Air,
    p=>p.x==1&&p.y==0&&p.z==0?type:BlockType.Air,
    p=>p.z==-1&&p.y==0&&p.x==0?type:BlockType.Air,
    p=>p.z==1&&p.y==0&&p.x==0?type:BlockType.Air,
    p=>p.y==0&&(p.x==-1||p.z==-1)?type:BlockType.Air,
    p=>p.x==1&&p.z==0?(p.y==0?BlockType.Stone:p.y==1?BlockType.RedstoneWire:BlockType.Air):BlockType.Air};
   for(byte state=0;state<16;state++)foreach(var get in neighbors)
   {
    var mesh=new MeshData(false);
    SpecialBlockMesh.Append(type,state,new Vector3Int(),get,d=>image(BlockTexturePart.Surface)>0?image(BlockTexturePart.Surface):7,new Color(1,1,1,1),mesh,image);
    Check(mesh.vertices.Count==mesh.uv.Count&&mesh.vertices.Count==mesh.colors.Count&&mesh.vertices.Count==mesh.faceCoordinates.Count,"special image/overlay mesh channels remain aligned");
    foreach(var uv in mesh.uv)used.Add((int)uv.z);
    var fallback=new MeshData(false);
    SpecialBlockMesh.Append(type,state,new Vector3Int(),get,d=>7,new Color(1,1,1,1),fallback,p=>0);
    foreach(var uv in fallback.uv)Check(uv.z==7,"empty part slots keep legacy texture layers");
   }
   foreach(var part in parts)Check(used.Contains(image(part)),"exposed slot reaches mesh: "+type+"/"+part);
  }
  var cake=SpecialBlockGeometry.Selection(BlockType.Cake,0);
  var bitten=SpecialBlockGeometry.Selection(BlockType.Cake,3);
  Check(cake.xMin==1&&bitten.xMin==7&&bitten.xMax==15,"cake interior follows the metadata bite face");
  var fire=new MeshData(false);
  SpecialBlockMesh.Append(BlockType.Fire,0,new Vector3Int(),null,d=>1,new Color(1,1,1,1),fire,p=>p==BlockTexturePart.FireA?2:3);
  float minX=1,minZ=1,maxX=0,maxZ=0,maxY=0;
  int firstSheet=0,secondSheet=0;
  foreach(var v in fire.vertices){minX=Math.Min(minX,v.x);maxX=Math.Max(maxX,v.x);minZ=Math.Min(minZ,v.z);maxZ=Math.Max(maxZ,v.z);maxY=Math.Max(maxY,v.y);}
  foreach(var uv in fire.uv){if(uv.z==2)firstSheet++;if(uv.z==3)secondSheet++;}
  Check(fire.vertices.Count==64&&firstSheet==32&&secondSheet==32,"fire has eight double-sided leaning sheets using both animation images");
  Check(minX==0&&maxX==1&&minZ==0&&maxZ==1&&Math.Abs(maxY-1.4f)<.00001f,"fire covers the native full block footprint at Beta's 1.4-block flame height");
  Console.WriteLine((checks-start)+" special texture selection/fallback/overlay checks passed.");
 }

 static void CheckWireAndRailConnections()
 {
  var origin=new Vector3Int();
  var offsets=new[]{Vector3Int.left,Vector3Int.right,Vector3Int.back,Vector3Int.forward};
  int[] expected={0,1,1,1,0,8,9,9,0,7,6,6,0,7,6,6};
  for(int mask=0;mask<16;mask++)
  {
   int connections=mask;
   Func<Vector3Int,BlockType> get=p=>{
    for(int i=0;i<4;i++)if((connections&(1<<i))!=0&&p.x==offsets[i].x&&p.y==0&&p.z==offsets[i].z)return BlockType.Rail;
    return BlockType.Air;
   };
   for(byte old=0;old<10;old++)
   {
    byte actual=SpecialBlockGeometry.RailState(BlockType.Rail,old,origin,get);
    Check(actual==(mask==0&&(old==1||old==2||old==3)?1:expected[mask]),"rail recomputes removed corners/slopes and junctions from every previous state");
    byte powered=SpecialBlockGeometry.RailState(BlockType.PoweredRail,(byte)(old|8),origin,get);
    Check((powered&8)!=0&&(powered&7)<6,"powered rail keeps activation and never curves");
   }
  }
  // The actual corner PNG exits its right and bottom image edges (U=1,V=0).
  // Verify those edges reach the requested world directions on BOTH faces.
  for(byte shape=6;shape<=9;shape++)
  {
   var mesh=new MeshData(false);
   SpecialBlockMesh.Append(BlockType.Rail,shape,origin,null,d=>7,new Color(1,1,1,1),mesh,p=>100+(int)p);
   for(int i=0;i<8;i++)
   {
    var v=mesh.vertices[i];var uv=mesh.uv[i];
    if(uv.x==1)Check(shape==6?v.x==1:shape==7?v.z==1:shape==8?v.x==0:v.z==0,"corner right-image exit matches rail metadata");
    if(uv.y==0)Check(shape==6?v.z==1:shape==7?v.x==0:shape==8?v.z==0:v.x==1,"corner bottom-image exit matches rail metadata");
    Check(uv.z==100+(int)BlockTexturePart.RailCorner,"corner uses assigned corner image");
   }
  }
  foreach(var type in new[]{BlockType.RedstoneRepeater,BlockType.PoweredRepeater})
  for(byte state=0;state<16;state++)for(int direction=0;direction<4;direction++)
   Check(SpecialBlockMesh.WireConnects(type,state,direction)==((state&1)==(direction<2?1:0)),"repeater wire joins follow facing axis, not delay bits");
  Check(SpecialBlockMesh.WireConnects(BlockType.RedstoneTorchOff,0,0),"unlit redstone torch retains dust connection");
  for(int mask=0;mask<16;mask++)
  {
   int connections=mask;
   Func<Vector3Int,BlockType> get=p=>{
    for(int i=0;i<4;i++)if((connections&(1<<i))!=0&&p.x==offsets[i].x&&p.y==0&&p.z==offsets[i].z)return BlockType.RedstoneWire;
    return BlockType.Air;
   };
   var mesh=new MeshData(false);
   SpecialBlockMesh.Append(BlockType.RedstoneWire,0,origin,get,f=>7,new Color(1,1,1,1),mesh,p=>100+(int)p);
   bool straight=mask!=0&&((mask&3)==0||(mask&12)==0);
   for(int i=0;i<8;i++)
   {
    var v=mesh.vertices[i];var uv=mesh.uv[i];
    Check(uv.z==100+(int)(straight?BlockTexturePart.WireLine:BlockTexturePart.WireCross),"all sixteen wire masks select straight/cross image");
    if(!straight)Check(Near(uv.x,v.x)&&Near(uv.y,v.z),"cropped wire UVs match geometry on both windings");
   }
  }
  for(int side=0;side<4;side++)
  {
   var d=offsets[side];
   Func<Vector3Int,BlockType> get=p=>p.x==d.x&&p.z==d.z?(p.y==0?BlockType.Stone:p.y==1?BlockType.RedstoneWire:BlockType.Air):BlockType.Air;
   var mesh=new MeshData(false);
   SpecialBlockMesh.Append(BlockType.RedstoneWire,0,origin,get,f=>7,new Color(1,1,1,1),mesh,p=>100+(int)p);
   for(int i=16;i<mesh.vertices.Count;i++)
    Check(mesh.uv[i].z==100+(int)BlockTexturePart.WireLine||mesh.uv[i].z==100+(int)BlockTexturePart.WireLineOverlay,"all climbing directions use wire line/overlay, not legacy side image");
   Check(mesh.vertices.Count==32,"climbing wire includes double sided line and overlay");
   Func<Vector3Int,BlockType> blocked=p=>p.x==0&&p.z==0&&p.y==1?BlockType.Stone:get(p);
   mesh=new MeshData(false);
   SpecialBlockMesh.Append(BlockType.RedstoneWire,0,origin,blocked,f=>7,new Color(1,1,1,1),mesh,p=>100+(int)p);
   Check(mesh.vertices.Count==16,"ceiling blocks upward dust connection");
  }
 }
}
