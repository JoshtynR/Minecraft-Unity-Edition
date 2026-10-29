using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

// Attribute order must match Unity's canonical vertex attribute order.
[StructLayout(LayoutKind.Sequential, Pack=1)]
public struct PackedChunkVertex
{
    public Vector3 position;
    public sbyte nx,ny,nz,nw;
    public byte r,g,b,a;
    public Vector3 texture;
    public ushort sky,light;
    public ushort shade,flat,mode,pad;
    public ushort c0,c1,c2,c3;
    public ushort u,v;
    public const int Stride=56;
    private static readonly VertexAttributeDescriptor[] Layout={
        new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
        new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.SNorm8,4),
        new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float16,2),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord2,VertexAttributeFormat.Float16,4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord3,VertexAttributeFormat.Float16,4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord4,VertexAttributeFormat.Float16,2)};
    [StructLayout(LayoutKind.Explicit)]
    private struct Bits {[FieldOffset(0)]public float value;[FieldOffset(0)]public uint bits;}
    public static ushort Half(float value)
    {
        uint bits=new Bits{value=value}.bits;
        uint sign=(bits>>16)&0x8000, mantissa=bits&0x7fffff;
        int exponent=(int)((bits>>23)&255)-112;
        if(exponent>=31)return (ushort)(sign|0x7c00|(mantissa!=0&&((bits>>23)&255)==255?0x200u:0u));
        if(exponent<=0)
        {
            if(exponent< -10)return (ushort)sign;
            mantissa=(mantissa|0x800000)>>(1-exponent);
            return (ushort)(sign|((mantissa+0xfff+((mantissa>>13)&1))>>13));
        }
        uint rounded=mantissa+0xfff+((mantissa>>13)&1);
        return (ushort)(sign+((uint)exponent<<10)+(rounded>>13));
    }
    private static byte ColorByte(float v)=>(byte)Math.Max(0,Math.Min(255,(int)Math.Round(v*255)));
    private static sbyte NormalByte(float v)=>(sbyte)Math.Max(-127,Math.Min(127,(int)Math.Round(v*127)));
    public static PackedChunkVertex Pack(MeshData data,int i)
    {
        var n=data.normals[i];var color=data.colors[i];var light=data.lightingUVs[i];
        var side=data.sides[i];var corner=data.smoothLightCorners[i];var face=data.faceCoordinates[i];
        return new PackedChunkVertex{position=data.vertices[i],texture=data.uv[i],
            nx=NormalByte(n.x),ny=NormalByte(n.y),nz=NormalByte(n.z),
            r=ColorByte(color.r),g=ColorByte(color.g),b=ColorByte(color.b),a=ColorByte(color.a),
            sky=Half(light.x),light=Half(light.y),shade=Half(side.x),flat=Half(side.y),mode=Half(side.z),
            c0=Half(corner.x),c1=Half(corner.y),c2=Half(corner.z),c3=Half(corner.w),u=Half(face.x),v=Half(face.y)};
    }
    public static void Upload(Mesh mesh,MeshData data)
    {
        if(Marshal.SizeOf(typeof(PackedChunkVertex))!=Stride)throw new InvalidOperationException("Unexpected packed chunk vertex stride");
        var buffer=new PackedChunkVertex[data.vertices.Count];
        for(int i=0;i<buffer.Length;i++)buffer[i]=Pack(data,i);
        mesh.SetVertexBufferParams(buffer.Length,Layout);
        mesh.SetVertexBufferData(buffer,0,0,buffer.Length);
    }
}
