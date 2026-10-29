using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public float x,y;}
 public struct Vector4 {public float x,y,z,w;}
 public struct Color {public float r,g,b,a;}
 public class Mesh {
 public int count;public Rendering.VertexAttributeDescriptor[] layout;public Array data;
 public void SetVertexBufferParams(int count,params Rendering.VertexAttributeDescriptor[] layout){this.count=count;this.layout=layout;}
 public void SetVertexBufferData<T>(T[] data,int start,int destination,int count){this.data=data;}
 }
}
namespace UnityEngine.Rendering {
 public enum VertexAttribute {Position,Normal,Color,TexCoord0,TexCoord1,TexCoord2,TexCoord3,TexCoord4}
 public enum VertexAttributeFormat {Float32,SNorm8,UNorm8,Float16}
 public struct VertexAttributeDescriptor {
 public VertexAttribute attribute;public VertexAttributeFormat format;public int dimension;
 public VertexAttributeDescriptor(VertexAttribute a,VertexAttributeFormat f,int d){attribute=a;format=f;dimension=d;}
 }
}
public class MeshData {
 public List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>(),uv=new List<Vector3>(),sides=new List<Vector3>();
 public List<Color> colors=new List<Color>();public List<Vector2> lightingUVs=new List<Vector2>(),faceCoordinates=new List<Vector2>();
 public List<Vector4> smoothLightCorners=new List<Vector4>();
}
public static class PackedCheck {
 static void Check(bool b){if(!b)throw new Exception("packed layout/value check");}
 public static void Main(){
 Check(Marshal.SizeOf(typeof(PackedChunkVertex))==56);
 Check(Marshal.OffsetOf(typeof(PackedChunkVertex),"texture").ToInt32()==20);
 Check(PackedChunkVertex.Half(0)==0&&PackedChunkVertex.Half(1)==0x3c00&&PackedChunkVertex.Half(15)==0x4b80);
 Check(PackedChunkVertex.Half(-1)==0xbc00&&PackedChunkVertex.Half(.5f)==0x3800);
 var d=new MeshData();d.vertices.Add(new Vector3(1,127,3));d.normals.Add(new Vector3(0,1,0));d.uv.Add(new Vector3(.25f,.5f,4097));
 d.sides.Add(new Vector3(1,1,4));d.colors.Add(new Color{r=1,g=.5f,b=0,a=1});d.lightingUVs.Add(new Vector2{x=15,y=7});
 d.faceCoordinates.Add(new Vector2{x=1,y=0});d.smoothLightCorners.Add(new Vector4{x=15,y=7});
 var v=PackedChunkVertex.Pack(d,0);
 Check(v.position.y==127&&v.texture.z==4097&&v.ny==127&&v.r==255&&v.g==128&&v.a==255);
 Check(v.sky==0x4b80&&v.mode==0x4400&&v.u==0x3c00);
 var mesh=new Mesh();PackedChunkVertex.Upload(mesh,d);Check(mesh.count==1&&mesh.layout.Length==8&&mesh.data.Length==1);
 Console.WriteLine("Packed 56-byte stride, offsets, half conversion, tint/normal quantization, layer precision and upload checks passed (Unity GPU API mocked).");
 }
}
