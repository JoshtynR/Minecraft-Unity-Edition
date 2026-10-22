using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class TooltipAttribute:Attribute{public TooltipAttribute(string text){}}
    public class RangeAttribute:Attribute{public RangeAttribute(float low,float high){}}
    public class Object {public bool destroyed;public static void Destroy(Object obj){obj.destroyed=true;}public static void DestroyImmediate(Object obj){obj.destroyed=true;}}
    public static class Application{public static bool isPlaying=true;}
    public class MonoBehaviour:Object{}
    public class Texture:Object{public string name;}
    public enum TextureFormat{RGBA32}
    public enum FilterMode{Point,Bilinear}
    public enum TextureWrapMode{Clamp}
    public class Texture2D:Texture
    {
        public static int created;public FilterMode filterMode;public TextureWrapMode wrapMode;public Color[] pixels;
        public Texture2D(int w,int h,TextureFormat f,bool mip,bool linear){created++;}
        public void SetPixels(Color[] colors){pixels=(Color[])colors.Clone();}
        public void SetPixels32(Color32[] colors){}
        public void Apply(bool mip,bool readable){}
    }
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Color32{public static explicit operator Color32(Color c)=>new Color32();}
    public struct Vector4{public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public static class Mathf
    {
        public static float Clamp01(float f)=>Math.Max(0,Math.Min(1,f));
        public static int Clamp(int f,int a,int b)=>Math.Max(a,Math.Min(b,f));
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float LerpUnclamped(float a,float b,float t)=>a+(b-a)*t;
    }
    public static class Time{public static float deltaTime=.05f;}
    public static class Shader
    {
        public static int uploads;public static Texture texture;
        public static void SetGlobalFloat(string key,float value){}
        public static void SetGlobalVectorArray(string key,Vector4[] values){uploads++;}
        public static void SetGlobalTexture(string key,Texture value){texture=value;}
    }
}
public enum WorldDimension{Overworld,Nether}
public class WorldData{public Dictionary<int,object> chunkDict=new Dictionary<int,object>{{1,new object()},{2,new object()}};}
public partial class World:MonoBehaviour
{
    public static World Instance;public WorldDimension dimension;public float gamma;public double CurrentWorldTicks=6000;
    public bool IsWorldCreated;public WorldData worldData=new WorldData();public int queued;
    public void AddChunkToUpdate(object chunk){queued++;}
    public void Frame()=>UpdateBetaLightingSettings();public void DisposeForCheck()=>ReleaseLightmap();
}
class RuntimeCheck
{
    static void Require(bool ok,string label){if(!ok)throw new Exception(label);}
    static void Main()
    {
        var world=new World();World.Instance=world;world.Frame();world.IsWorldCreated=true;
        var texture=(Texture2D)Shader.texture;
        Require(world.java18Lighting&&texture!=null,"1.8 lighting defaults on and uploads a map");
        Require(texture.filterMode==FilterMode.Bilinear&&texture.wrapMode==TextureWrapMode.Clamp,"lightmap sampling configuration");
        var day=texture.pixels[15*16].r;
        world.CurrentWorldTicks=18000;world.betaSkylightSubtracted=11;world.Frame();
        Require(texture.pixels[15*16].r<day&&texture.pixels[15*16].b>texture.pixels[15*16].r,"clock updates cool night skylight");
        Require(world.queued==0,"day/night changes do not remesh chunks");
        var dark=texture.pixels[0].r;world.gamma=1;world.Frame();
        Require(texture.pixels[0].r>dark&&world.queued==0,"gamma changes update map without remeshing");
        for(int i=0;i<30;i++)world.Frame();Require(Texture2D.created==1,"lightmap texture is reused");
        var randomField=typeof(World).GetField("lightmapRandom",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        randomField.SetValue(world,null);world.Frame();
        Require(randomField.GetValue(world)!=null,"lighting recreates missing visual RNG after script reload");
        world.java18Lighting=false;world.Frame();Require(world.queued==2,"style switch remeshes exactly once");
        world.Frame();Require(world.queued==2,"unchanged style does not keep remeshing");
        world.betaSkylightSubtracted=10;world.Frame();Require(world.queued==4,"legacy baked light still remeshes when needed");
        int uploads=Shader.uploads;new World().Frame();Require(Shader.uploads==uploads,"inactive dimension cannot overwrite active lightmap");
        world.DisposeForCheck();Require(texture.destroyed,"world teardown releases owned lightmap");
        world.worldData=null;world.java18Lighting=true;world.Frame();
        Require(randomField.GetValue(world)!=null,"style change tolerates missing world data during lifecycle transitions");
        world.DisposeForCheck();
        Console.WriteLine("Production world/lightmap lifecycle, default mode, clock/gamma updates, remesh avoidance and dimension ownership checks passed (Unity GPU mocked).");
    }
}
