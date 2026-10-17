using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ShapeCheck
{
 static void CheckSpecialTextures()
 {
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
}
