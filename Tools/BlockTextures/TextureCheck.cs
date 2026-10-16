using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
    public class Object { public string name; public bool destroyed; public static void Destroy(Object o) { if(o!=null)o.destroyed=true; } }
    public class MonoBehaviour : Object { }
    public class ScriptableObject : Object { }
    public class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; }
    public class DefaultExecutionOrderAttribute : Attribute { public DefaultExecutionOrderAttribute(int i){} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
    public class RangeAttribute : Attribute { public RangeAttribute(int a,int b){} }
    public static class Application { public static bool isPlaying=true; }
    public static class SystemInfo { public static bool supports2DArrayTextures=true; }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b); public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v)); }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
    public struct Color { public static Color white=>new Color(); }
    public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
    public struct Rect { public Rect(int a,int b,int c,int d){} }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum RenderTextureFormat { ARGB32 }
    public enum RenderTextureReadWrite { sRGB }
    public enum ColorSpace { Linear, Gamma }
    public static class QualitySettings { public static ColorSpace activeColorSpace=ColorSpace.Linear; }
    public static class GL { public static bool sRGBWrite; }
    public class Texture2D : Object
    {
        public int width,height; public bool isReadable=true; public FilterMode filterMode=FilterMode.Bilinear;
        public Color32[] pixels;
        public Texture2D(int w,int h,TextureFormat f,bool mip,bool linear){width=w;height=h;pixels=new Color32[w*h];}
        public Color32[] GetPixels32(){if(!isReadable)throw new Exception("Read/Write disabled");return pixels;}
        public void ReadPixels(Rect r,int x,int y,bool mip)
        {
            var src=RenderTexture.active.source;
            for(int v=0;v<height;v++)for(int u=0;u<width;u++)pixels[v*width+u]=src.pixels[(v*src.height/height)*src.width+(u*src.width/width)];
        }
    }
    public class Texture2DArray : Object
    {
        public int width,height,depth; public FilterMode filterMode; public TextureWrapMode wrapMode; public Color32[][] layers; public bool applied;
        public Texture2DArray(int w,int h,int d,TextureFormat f,bool mip,bool linear){width=w;height=h;depth=d;layers=new Color32[d][];}
        public void SetPixels32(Color32[] p,int layer){if(p.Length!=width*height)throw new Exception("size");layers[layer]=p;}
        public void Apply(bool mip,bool discard){applied=true;}
    }
    public class RenderTexture : Object
    {
        public static RenderTexture active; public Texture2D source; public static int allocated;
        public static RenderTexture GetTemporary(int w,int h,int depth,RenderTextureFormat f,RenderTextureReadWrite rw){allocated++;return new RenderTexture();}
        public static void ReleaseTemporary(RenderTexture rt){allocated--;}
    }
    public static class Graphics
    {
        public static int blits;
        public static void Blit(Texture2D src,RenderTexture rt){if(src.filterMode!=FilterMode.Point||!GL.sRGBWrite)throw new Exception("readback state");blits++;rt.source=src;}
    }
    public static class Shader { public static Texture2DArray bound; public static void SetGlobalTexture(string n,Texture2DArray a){if(n!="_BlockFaceTextures")throw new Exception("property");bound=a;} }
    public static class Debug { public static int errors,warnings;public static void LogWarning(string s,Object o){warnings++;}public static void LogError(string s,Object o){errors++;} }
}
public enum Direction { up,down,left,right,forwards,backwards }
public static class TextureCheck
{
    static int checks;
    static void Check(bool v,string label){checks++;if(!v)throw new Exception(label);}
    static void Invoke(object target,string method){target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}
    static Texture2D Image(int size,byte r,bool readable=true){var t=new Texture2D(size,size,TextureFormat.RGBA32,false,false);t.isReadable=readable;for(int i=0;i<t.pixels.Length;i++)t.pixels[i]=new Color32(r,13,21,127);return t;}
    public static void Main()
    {
        foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
            Check(BlockDataManager.blockTypeDataDictionary[(int)type]!=null,"static lookup safe before manager OnEnable");
        var top=Image(32,10);var bottom=Image(16,20,false);var side=Image(8,30);
        var data=new BlockTypeData{blockType=BlockType.Grass,textureData=new TextureData{top=top,bottom=bottom,side=side}};
        var uniform=new BlockTypeData{blockType=BlockType.Dirt,textureData=new TextureData{top=side,bottom=side,side=side}};
        var missing=new BlockTypeData{blockType=BlockType.Air};
        var asset=new BlockDataSO{faceTextureResolution=32,textureDataList=new List<BlockTypeData>{data,uniform,missing}};
        var manager=new BlockDataManager{textureData=asset};
        var previous=new RenderTexture();RenderTexture.active=previous;GL.sRGBWrite=false;
        Invoke(manager,"OnEnable");
        foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
            Check(BlockDataManager.blockTypeDataDictionary[(int)type]!=null&&BlockDataManager.blockTypeDataDictionary[(int)type].textureData!=null,"sparse asset has safe complete runtime lookup");
        foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
        {
            var probe=new LoadingProbe{type=type};probe.Loaded();
            Check(probe.light==probe.BlockData.lightEmission&&probe.chunkData.blockLightUpdateQueue.Count==(probe.BlockData.lightEmission>0?1:0),"production Block.Loaded handles every saved type with sparse asset");
        }
        Check(asset.textureDataList.Count==3&&ReferenceEquals(BlockDataManager.blockTypeDataDictionary[(int)BlockType.Grass],data),"fallback never replaces or appends user asset definitions");
        Check(!BlockDataManager.blockTypeDataDictionary[0].generateCollider&&BlockDataManager.blockTypeDataDictionary[0].opacity==0,"missing Nothing definition safe for unloaded cells");
        Check(BlockDataManager.blockTypeDataDictionary[(int)BlockType.Torch].lightEmission==14,"missing emitter has default emission");
        var array=Shader.bound;
        Check(array.depth==4&&array.applied,"unique images + missing layer");
        Check(array.filterMode==FilterMode.Point&&array.wrapMode==TextureWrapMode.Clamp,"sampling");
        Check(array.layers[1][0].r==10&&array.layers[2][0].r==20&&array.layers[3][0].r==30,"image upload");
        Check(array.layers[2][0].a==127,"alpha preserved");
        Check(array.layers[0][0].r==255&&array.layers[0][4].r==0,"missing checker");
        Check(RenderTexture.active==previous&&!GL.sRGBWrite&&RenderTexture.allocated==0,"GPU state restored");
        Check(bottom.filterMode==FilterMode.Bilinear&&side.filterMode==FilterMode.Bilinear,"source filters restored");
        Check(Graphics.blits==2,"mixed size/unreadable readback");
        foreach(Direction d in Enum.GetValues(typeof(Direction)))
        {
            var uv=BlockHelper.FaceUVs(d,BlockType.Grass);
            int expected=d==Direction.up?1:d==Direction.down?2:3;
            Check(uv.Length==4,"quad size");
            Check(uv[0].x==0&&uv[0].y==0&&uv[1].x==0&&uv[1].y==1&&uv[2].x==1&&uv[2].y==1&&uv[3].x==1&&uv[3].y==0,"full face orientation");
            foreach(var v in uv)Check(v.z==expected,"face layer");
            foreach(var v in BlockHelper.FaceUVs(d,BlockType.Dirt))Check(v.z==3,"shared texture dedup");
            foreach(var v in BlockHelper.FaceUVs(d,BlockType.Air))Check(v.z==0,"null faces");
        }
        Invoke(manager,"OnDisable");Check(Shader.bound==null&&array.destroyed,"release global and GPU resource");
        asset.faceTextureResolution=1;Invoke(manager,"OnEnable");Check(Shader.bound.width==16,"minimum resolution");Invoke(manager,"OnDisable");
        SystemInfo.supports2DArrayTextures=false;Invoke(manager,"OnEnable");Check(Shader.bound==null&&Debug.errors==1,"unsupported GPU preserves CPU map");Check(BlockDataManager.blockTypeDataDictionary[3]==data,"CPU map available");Invoke(manager,"OnDisable");
        SystemInfo.supports2DArrayTextures=true;
        data.textureData.front=top;data.textureData.back=bottom;data.textureData.left=side;data.textureData.right=top;
        Invoke(manager,"OnEnable");
        Check(data.textureData.FaceIndex(Direction.backwards)==1&&data.textureData.FaceIndex(Direction.forwards)==2&&data.textureData.FaceIndex(Direction.left)==3&&data.textureData.FaceIndex(Direction.right)==1,"optional directional faces register and reuse existing image layers");
        Check(Shader.bound.depth==4&&uniform.textureData.FaceIndex(Direction.backwards)==3,"overrides preserve deduplication and blank fallbacks");Invoke(manager,"OnDisable");
        manager.textureData=new BlockDataSO{textureDataList=new List<BlockTypeData>{null,data,uniform,missing}};
        Invoke(manager,"OnEnable");Check(BlockDataManager.blockTypeDataDictionary.Length>=133&&ReferenceEquals(BlockDataManager.blockTypeDataDictionary[3],data),"null catalog entries skipped while texture assignments survive");Invoke(manager,"OnDisable");
        manager.textureData=null;Invoke(manager,"OnEnable");Check(BlockDataManager.blockTypeDataDictionary.Length>=133&&Shader.bound.depth==1,"missing asset uses CPU definitions and checker texture");Invoke(manager,"OnDisable");
        Console.WriteLine(checks+" block texture data/UV/startup checks passed (Unity GPU APIs mocked).");
    }
}
