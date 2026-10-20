using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
    public partial class Object { public string name; public bool destroyed; public static void Destroy(Object o) { if(o!=null)o.destroyed=true; } }
    public class MonoBehaviour : Object { }
    public class ScriptableObject : Object { }
    public class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; }
    public class DefaultExecutionOrderAttribute : Attribute { public DefaultExecutionOrderAttribute(int i){} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
    public class RangeAttribute : Attribute { public RangeAttribute(int a,int b){} }
    public class HideInInspector:Attribute {}
    public static partial class Application { public static bool isPlaying=true; }
    public static class SystemInfo { public static bool supports2DArrayTextures=true; }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b); public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v)); }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1,1); }
    public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
    public struct Rect { public Rect(int a,int b,int c,int d){} }
    public enum TextureFormat { RGBA32, RGBAFloat }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum RenderTextureFormat { ARGB32 }
    public enum RenderTextureReadWrite { sRGB }
    public enum ColorSpace { Linear, Gamma }
    public static class QualitySettings { public static ColorSpace activeColorSpace=ColorSpace.Linear; }
    public static class GL { public static bool sRGBWrite; }
    public partial class Texture2D : Object
    {
        public int width,height; public bool isReadable=true; public FilterMode filterMode=FilterMode.Bilinear;
        public Color32[] pixels;
        public Color[] colors;public TextureWrapMode wrapMode;public bool applied,linear;public TextureFormat format;
        public Texture2D(int w,int h,TextureFormat f,bool mip,bool linear){width=w;height=h;pixels=new Color32[w*h];this.linear=linear;format=f;}
        public void SetPixels(Color[] value){if(value.Length!=width*height)throw new Exception("size");colors=value;}
        public void Apply(bool mip,bool discard){applied=true;}
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
    public static class Shader
    {
        public static Texture2DArray bound;public static Texture2D animations;public static float animationWidth;
        public static void SetGlobalTexture(string n,Object a){if(n=="_BlockFaceTextures")bound=(Texture2DArray)a;else if(n=="_BlockFaceAnimations")animations=(Texture2D)a;else throw new Exception("property");}
        public static void SetGlobalFloat(string n,float value){if(n!="_BlockFaceAnimationWidth")throw new Exception("property");animationWidth=value;}
    }
    public static partial class Debug { public static int errors,warnings;public static void LogWarning(string s,Object o){warnings++;}public static void LogError(string s,Object o){errors++;} }
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
        var manager=new BlockDataManager{textureData=asset,grassColorMap=Image(256,111),foliageColorMap=Image(256,123)};
        var previous=new RenderTexture();RenderTexture.active=previous;GL.sRGBWrite=false;
        Invoke(manager,"OnEnable");
        Check(BiomeColorPalette.Current.Grass(1,1)==(111<<16|13<<8|21)&&BiomeColorPalette.Current.Foliage(1,1)==(123<<16|13<<8|21),"manager initializes assigned biome maps before meshing");
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
        Check(Graphics.blits==1,"only unreadable images need GPU readback; readable faces resize on CPU");
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
        CheckSpecialParts();
        CheckFluidSheetGrids();
        CheckAnimations();
        Console.WriteLine(checks+" block texture data/UV/startup checks passed (Unity GPU APIs mocked).");
    }
    static void CheckSpecialParts()
    {
        var side=Image(16,91);
        var texture=new TextureData{side=side,top=side,bottom=side};
        foreach(BlockTexturePart part in Enum.GetValues(typeof(BlockTexturePart)))
            texture.specialTextures.Add(new SpecialBlockTexture{part=part,image=side});
        var block=new BlockTypeData{blockType=BlockType.Bed,textureData=texture};
        var manager=new BlockDataManager{textureData=new BlockDataSO{textureDataList=new List<BlockTypeData>{block}}};
        Invoke(manager,"OnEnable");
        foreach(BlockTexturePart part in Enum.GetValues(typeof(BlockTexturePart)))
            Check(texture.PartIndex(part)==1,"every named model image registers and deduplicates shared references");
        Check(Shader.bound.depth==2&&texture.specialTextures.Count==Enum.GetValues(typeof(BlockTexturePart)).Length,"part registration neither copies nor replaces user assignments");
        texture.specialTextures.Clear();Invoke(manager,"OnEnable");
        Check(texture.PartIndex(BlockTexturePart.BedHeadTop,7)==7,"rebuild clears stale indices after an override is removed");
        Check(texture.FaceIndex(Direction.backwards)==1,"legacy side fallback survives removal of part overrides");
        Invoke(manager,"OnDisable");
        texture.specialTextures.Add(new SpecialBlockTexture{part=BlockTexturePart.FluidStill,image=Image(16,92)});
        texture.specialTextures.Add(new SpecialBlockTexture{part=BlockTexturePart.FluidFlow,image=Image(16,93)});
        Invoke(manager,"OnEnable");
        Check(texture.FaceIndex(BlockType.Water,Direction.up)==2&&texture.FaceIndex(BlockType.Lava,Direction.right)==3,"water and lava select still versus flowing images");
        Check(texture.FaceIndex(BlockType.Stone,Direction.up)==1,"fluid overrides cannot change ordinary block faces");
        Invoke(manager,"OnDisable");
        texture.specialTextures.Clear();texture.top=texture.bottom=texture.side=null;
        var sheet=Sheet(16,1,3,31);texture.animateFaces=true;
        texture.specialTextures.Add(new SpecialBlockTexture{part=BlockTexturePart.FireA,image=sheet});
        texture.specialTextures.Add(new SpecialBlockTexture{part=BlockTexturePart.FireB,image=sheet});
        Invoke(manager,"OnEnable");
        Check(Shader.bound.depth==4&&texture.PartIndex(BlockTexturePart.FireA)==texture.PartIndex(BlockTexturePart.FireB),"animated special parts share frame ranges without duplicate allocations");
        Check(Shader.animations.colors[1].g==3&&Shader.animations.colors[1].b==20,"special part sheets use the existing animation timing table");
        Invoke(manager,"OnDisable");
        Check(!sheet.destroyed&&!side.destroyed,"rebuilding and disposing runtime part images preserves source textures");
    }
    static void CheckFluidSheetGrids()
    {
        var still=Sheet(16,1,20,40);var flow=Sheet(32,1,32,90);
        var stillPart=new SpecialBlockTexture{part=BlockTexturePart.FluidStill,image=still};
        var flowPart=new SpecialBlockTexture{part=BlockTexturePart.FluidFlow,image=flow};
        var lava=new BlockTypeData{blockType=BlockType.Lava,textureData=new TextureData{
            animateFaces=true,animationRows=20,animationFrameCount=20,specialTextures=new List<SpecialBlockTexture>{stillPart,flowPart}}};
        var manager=new BlockDataManager{textureData=new BlockDataSO{textureDataList=new List<BlockTypeData>{lava}}};
        Invoke(manager,"OnEnable");
        Check(Shader.bound.depth==53&&lava.textureData.PartIndex(BlockTexturePart.FluidStill)==1&&lava.textureData.PartIndex(BlockTexturePart.FluidFlow)==21,"20-frame still and 32-frame flow detect independent grids despite old block-wide rows");
        Check(Shader.animations.colors[1].g==20&&Shader.animations.colors[21].g==32,"each fluid sheet plays all its frames");
        for(int i=0;i<20;i++)Check(Shader.bound.layers[1+i][0].r==40+i,"still lava extracts each 16x16 frame without stretching");
        for(int i=0;i<32;i++)Check(Shader.bound.layers[21+i][0].r==90+i,"flow lava extracts each 32x32 frame without truncation");
        Check(Shader.animations.colors[1].a==1&&Shader.animations.colors[21].a==0,"automatic playback reverses still lava but loops flow");
        Invoke(manager,"OnDisable");
        still.name="lava_still";flow.name="lava_flow";Invoke(manager,"OnEnable");
        Check(Shader.animations.colors[1].b==10&&Math.Abs(Shader.animations.colors[21].b-20f/3)<.00001f,"already-assigned modern lava strips use metadata timing rather than legacy 20fps");
        Check(ModernFluidTextures.FramesPerSecond("water_still")==10&&ModernFluidTextures.FramesPerSecond("water_flow")==20,"modern water sheets use their distinct still/flow timings");
        Invoke(manager,"OnDisable");
        stillPart.playback=SheetPlayback.Loop;
        flowPart.overrideAnimation=true;flowPart.rows=16;flowPart.frameCount=8;flowPart.framesPerSecond=7.5f;
        Invoke(manager,"OnEnable");
        Check(Shader.bound.depth==29&&Shader.animations.colors[1].a==0&&Shader.animations.colors[1].g==20,"explicit still playback override preserves its frame grid");
        Check(Shader.animations.colors[21].g==8&&Shader.animations.colors[21].b==7.5f,"individual flow grid, count and speed overrides are honoured independently");
        Invoke(manager,"OnDisable");
        flowPart.animate=false;Invoke(manager,"OnEnable");
        Check(Shader.animations.colors[21].g==1&&Shader.animations.colors[21].b==0,"a part can be static while its block's other image animates");
        Invoke(manager,"OnDisable");
    }
    static Texture2D Sheet(int cell,int columns,int rows,byte first,bool readable=true)
    {
        var t=new Texture2D(cell*columns,cell*rows,TextureFormat.RGBA32,false,false){isReadable=readable};
        for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)
        {
            int frame=(rows-1-y/cell)*columns+x/cell;
            t.pixels[y*t.width+x]=new Color32((byte)(first+frame),(byte)(x%cell),(byte)(y%cell),127);
        }
        return t;
    }
    static void CheckAnimations()
    {
        var still=Sheet(16,1,4,16);var flow=Sheet(32,1,4,64,false);
        var water=new BlockTypeData{blockType=BlockType.Water,textureData=new TextureData{top=still,bottom=still,side=flow,animateFaces=true}};
        var lava=new BlockTypeData{blockType=BlockType.Lava,textureData=new TextureData{top=still,bottom=still,side=still,animateFaces=true,animationFramesPerSecond=10}};
        var stone=new BlockTypeData{blockType=BlockType.Stone,textureData=new TextureData{top=still,bottom=still,side=still}};
        var manager=new BlockDataManager{textureData=new BlockDataSO{faceTextureResolution=16,textureDataList=new List<BlockTypeData>{water,lava,stone}}};
        Invoke(manager,"OnEnable");var array=Shader.bound;var timing=Shader.animations;
        Check(array.depth==18&&water.textureData.topIndex==1&&water.textureData.bottomIndex==1&&water.textureData.sideIndex==5&&lava.textureData.topIndex==9&&lava.textureData.sideIndex==13&&stone.textureData.topIndex==17,"sheet deduplication includes timing, playback direction and static/animated usage");
        for(int i=0;i<4;i++)
        {
            Check(array.layers[1+i][0].r==16+i&&array.layers[9+i][0].r==16+i,"vertical strips extract top frame first without squeezing the sheet");
            Check(array.layers[5+i][0].r==64+i&&array.layers[5+i][0].a==127,"unreadable/rescaled flowing sheets retain frame pixels and alpha");
            Check(timing.colors[1+i].r==1&&timing.colors[1+i].g==4&&timing.colors[1+i].b==20,"water timing table points to contiguous four-frame range");
            Check(timing.colors[9+i].r==9&&timing.colors[9+i].g==4&&timing.colors[9+i].b==10,"lava playback speed independent of shared source image");
            Check(timing.colors[9+i].a==1&&timing.colors[13+i].a==0,"still lava reverses smoothly while flowing lava loops forward");
        }
        Check(timing.format==TextureFormat.RGBAFloat&&timing.linear&&timing.filterMode==FilterMode.Point&&timing.wrapMode==TextureWrapMode.Clamp,"timing metadata keeps exact values without filtering or gamma transforms");
        Check(timing.colors[17].r==17&&timing.colors[17].g==1&&timing.colors[17].b==0,"static faces are explicitly nonanimated");
        Check(Shader.animationWidth==array.depth&&timing.applied,"timing table bound for every array layer including checker");
        Invoke(manager,"OnEnable");Check(array.destroyed&&timing.destroyed,"reinitialization releases both old GPU allocations");
        array=Shader.bound;timing=Shader.animations;Invoke(manager,"OnDisable");
        Check(array.destroyed&&timing.destroyed&&Shader.bound==null&&Shader.animations==null&&Shader.animationWidth==0,"disable clears bindings and releases texture and timing resources");
        var grid=Sheet(16,2,2,100);
        water.textureData=new TextureData{top=grid,bottom=grid,side=grid,animateFaces=true,animationColumns=2,animationRows=2,animationFrameCount=3,animationFramesPerSecond=7.5f};
        manager.textureData.textureDataList=new List<BlockTypeData>{water};Invoke(manager,"OnEnable");
        Check(Shader.bound.depth==4&&Shader.animations.colors[1].g==3&&Shader.animations.colors[1].b==7.5f,"grid count may exclude trailing unused cell and retain fractional FPS");
        for(int i=0;i<3;i++)Check(Shader.bound.layers[1+i][0].r==100+i,"grid order is left-to-right then top-to-bottom");
        Invoke(manager,"OnDisable");
        water.textureData.animationColumns=3;int warnings=Debug.warnings;Invoke(manager,"OnEnable");
        Check(Debug.warnings>warnings&&Shader.bound.depth==2&&Shader.animations.colors[1].g==1,"invalid grids warn and fall back safely to static");Invoke(manager,"OnDisable");
        water.textureData=new TextureData{top=Sheet(16,4,1,120),animateFaces=true,animationColumns=4,animationFramesPerSecond=0};Invoke(manager,"OnEnable");
        Check(Shader.bound.depth==5&&Shader.bound.layers[4][0].r==123&&Shader.animations.colors[1].b==0,"horizontal strips and paused playback supported");Invoke(manager,"OnDisable");
        Check(RenderTexture.allocated==0,"all animated image readbacks release temporary render textures");
    }
}
