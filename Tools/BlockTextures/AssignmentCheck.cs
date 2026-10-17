using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object {}}
namespace UnityEngine
{
 public partial class Object {public static void DestroyImmediate(Object value){Destroy(value);}}
 public static partial class Application {public static string dataPath;}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public class Material:Object {public Texture2D mainTexture;}
 public static class Resources {public static T Load<T>(string name)where T:class=>null;}
 public static partial class Debug
 {
  public static string last;
  public static void Log(string value){last=value;}
  public static void LogWarning(string value){warnings++;}
  public static void LogError(string value){errors++;last=value;}
  public static void LogException(Exception value){throw value;}
 }
 public partial class Texture2D
 {
  public void SetPixels32(Color32[] value){pixels=value;}
  public byte[] EncodeToPNG()
  {
   var result=new byte[8+pixels.Length*4];Array.Copy(BitConverter.GetBytes(width),0,result,0,4);Array.Copy(BitConverter.GetBytes(height),0,result,4,4);
   for(int i=0;i<pixels.Length;i++){result[8+i*4]=pixels[i].r;result[9+i*4]=pixels[i].g;result[10+i*4]=pixels[i].b;result[11+i*4]=pixels[i].a;}return result;
  }
 }
}
namespace UnityEditor
{
 public class InitializeOnLoadAttribute:Attribute {}
 public class MenuItem:Attribute {public MenuItem(string name){}}
 public static class EditorApplication {public static Action delayCall;public static bool isPlayingOrWillChangePlaymode;}
 public class AssetPostprocessor {public string assetPath;public AssetImporter assetImporter;}
 public enum TextureImporterNPOTScale {ToNearest,None}
 public enum TextureImporterCompression {Compressed,Uncompressed}
 public class AssetImporter
 {
  public static Dictionary<string,TextureImporter> all=new Dictionary<string,TextureImporter>();
  public static AssetImporter GetAtPath(string path){TextureImporter result;return all.TryGetValue(path,out result)?result:null;}
 }
 public class TextureImporter:AssetImporter
 {
  public TextureImporterNPOTScale npotScale=TextureImporterNPOTScale.None;
  public TextureImporterCompression textureCompression=TextureImporterCompression.Uncompressed;
  public FilterMode filterMode=FilterMode.Point;public bool mipmapEnabled;
  public int maxTextureSize=2048;public Texture2D image;
  public void SaveAndReimport(){}
  public void GetSourceTextureWidthAndHeight(out int w,out int h){w=image.width;h=image.height;}
 }
 public enum ImportAssetOptions {ForceSynchronousImport}
 public static class AssetDatabase
 {
  public static bool rootExists=true;public static int imports;
  public static Dictionary<string,UnityEngine.Object> assets=new Dictionary<string,UnityEngine.Object>();
  public static string[] FindAssets(string filter)=>assets.Where(x=>filter=="t:BlockDataSO"?x.Value is BlockDataSO:x.Value is Texture2D).Select(x=>x.Key).ToArray();
  public static string[] FindAssets(string filter,string[] roots)=>FindAssets(filter).Where(p=>roots.Any(r=>p.StartsWith(r+"/"))).ToArray();
  public static string GUIDToAssetPath(string guid)=>guid;
  public static string AssetPathToGUID(string path)=>((uint)path.GetHashCode()).ToString("x8");
  public static T LoadAssetAtPath<T>(string path)where T:class {UnityEngine.Object result;return assets.TryGetValue(path,out result)?result as T:null;}
  public static string GetAssetPath(UnityEngine.Object value)=>assets.First(x=>ReferenceEquals(x.Value,value)).Key;
  public static void SaveAssetIfDirty(UnityEngine.Object value){}
  public static bool IsValidFolder(string path)=>path==MinecraftTextureAutoAssign.Root?rootExists:Directory.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath),path));
  public static void Refresh(){}
  public static void ImportAsset(string path,ImportAssetOptions options)
  {
   imports++;var raw=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath),path));
   var image=new Texture2D(BitConverter.ToInt32(raw,0),BitConverter.ToInt32(raw,4),TextureFormat.RGBA32,false,false);
   for(int i=0;i<image.pixels.Length;i++)image.pixels[i]=new Color32(raw[8+i*4],raw[9+i*4],raw[10+i*4],raw[11+i*4]);
   assets[path]=image;AssetImporter.all[path]=new TextureImporter{image=image};
  }
 }
 public static class Undo
 {
  public static int records;public static void IncrementCurrentGroup(){}public static int GetCurrentGroup()=>1;
  public static void SetCurrentGroupName(string name){}public static void RecordObject(UnityEngine.Object data,string name){records++;}
  public static void CollapseUndoOperations(int group){}public static void RevertAllDownToGroup(int group){}public static void FlushUndoRecordObjects(){}
 }
 public static class EditorUtility
 {
  public static void DisplayProgressBar(string title,string detail,float progress){}public static void ClearProgressBar(){}
  public static void SetDirty(UnityEngine.Object value){}public static void RevealInFinder(string path){}
 }
}
public static class AssignmentCheck
{
 static int checks;
 static void Check(bool value,string detail){checks++;if(!value)throw new Exception(detail);}
 static Texture2D Image(string path,int width=16,int height=16,byte color=1)
 {
  var image=new Texture2D(width,height,TextureFormat.RGBA32,false,false);
  for(int i=0;i<image.pixels.Length;i++)image.pixels[i]=new Color32(color,0,0,255);
  UnityEditor.AssetDatabase.assets[path]=image;UnityEditor.AssetImporter.all[path]=new UnityEditor.TextureImporter{image=image};return image;
 }
 public static void Main()
 {
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
  {
   var names=MinecraftTextureNames.Block(type);
   Check(names!=null,"every block has an explicit texture mapping or intentional empty mapping");
   if(type==BlockType.Nothing||type==BlockType.Air||type==BlockType.MovingPiston)continue;
   foreach(var face in new[]{"top","bottom","side","front","back","left","right"})Check(!string.IsNullOrEmpty(names.Get(face)),type+"."+face+" has candidates");
   foreach(var part in SpecialTextureLayout.Parts(type))Check(!string.IsNullOrEmpty(MinecraftTextureNames.Part(type,part)),type+"."+part+" has candidates");
  }
  foreach(ItemType type in Enum.GetValues(typeof(ItemType)))if(MinecraftTextureNames.DedicatedItemIcon(type))Check(!string.IsNullOrEmpty(MinecraftTextureNames.Item(type)),"every standalone/native item has icon candidates");
  var lookup=new MinecraftTextureLookup("Assets/Textures/Minecraft",new[]{
   "Assets/Textures/Minecraft/gui/stone.png","Assets/Textures/Minecraft/block/stone.png","Assets/Textures/Minecraft/items/diamond.png","Assets/Textures/Elsewhere/block/dirt.png"});
  bool ambiguous;
  Check(lookup.Find(MinecraftTextureKind.Block,"stone",out ambiguous).Contains("/block/"),"GUI names cannot overwrite block textures");
  Check(lookup.Find(MinecraftTextureKind.Item,"diamond",out ambiguous).Contains("/items/"),"legacy plural item directory works");
  Check(lookup.Find(MinecraftTextureKind.Block,"dirt",out ambiguous)==null,"lookup stays inside selected folder");
  lookup=new MinecraftTextureLookup("Assets/Textures/Minecraft",new[]{"Assets/Textures/Minecraft/a/blocks/stone.png","Assets/Textures/Minecraft/b/block/stone.png"});
  Check(lookup.Find(MinecraftTextureKind.Block,"stone",out ambiguous)==null&&ambiguous,"duplicate pack names are reported rather than chosen arbitrarily");
  Check(!MinecraftTextureNames.DedicatedItemIcon(ItemType.Dirt)&&!MinecraftTextureNames.FlatBlockIcon(ItemType.Dirt),"ordinary block icons keep their 3D mesh");
  string temp=Path.Combine(Path.GetTempPath(),"minecraft-assign-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(temp,"Assets"));
  Application.dataPath=Path.Combine(temp,"Assets");
  try
  {
   string root=MinecraftTextureAutoAssign.Root;
   var dirt=Image(root+"/block/dirt.png",color:2);var guiDirt=Image(root+"/gui/dirt.png",color:3);
   var diamond=Image(root+"/items/diamond.png",color:4);var torch=Image(root+"/blocks/torch.png",color:5);
   var original=new Texture2D(16,16,TextureFormat.RGBA32,false,false);
   var clock=Image(root+"/item/clock.png",16,512,7);
   for(int i=0;i<16*16;i++)clock.pixels[clock.pixels.Length-1-i]=new Color32(8,0,0,255);
   var icons=Image(root+"/gui/icons.png",256,256);
   var steve=Image(root+"/entity/steve.png",64,64,10);
   for(int i=0;i<steve.pixels.Length/2;i++)steve.pixels[i]=new Color32(11,0,0,255);
   for(int i=0;i<10;i++)Image(root+"/block/destroy_stage_"+i+".png",color:(byte)(20+i));
   var untouched=new BlockTypeData{blockType=BlockType.Grass,textureData=new TextureData{top=original},opacity=7,lightEmission=9};
   var blocks=new BlockDataSO{textureDataList=new List<BlockTypeData>{new BlockTypeData{blockType=BlockType.Dirt},untouched,new BlockTypeData{blockType=BlockType.Torch}}};
   var items=new ItemDataSO{items=new List<ItemDefinition>{new ItemDefinition{id=ItemType.Dirt},new ItemDefinition{id=ItemType.Diamond},new ItemDefinition{id=ItemType.Coal,icon=original},new ItemDefinition{id=ItemType.Clock}}};
   var hud=new PlayerHudAssets();var material=new Material();
   UnityEditor.AssetDatabase.assets["Assets/Block Data.asset"]=blocks;
   UnityEditor.AssetDatabase.assets["Assets/Resources/Item Data.asset"]=items;
   UnityEditor.AssetDatabase.assets["Assets/Resources/PlayerHudAssets.asset"]=hud;
   UnityEditor.AssetDatabase.assets["Assets/Resources/InventoryPlayerMaterial.mat"]=material;
   MinecraftTextureAutoAssign.Assign();
   Check(ReferenceEquals(blocks.textureDataList[0].textureData.side,dirt)&&!ReferenceEquals(blocks.textureDataList[0].textureData.side,guiDirt),"actual tool assigns block image, not identically named GUI image");
   Check(ReferenceEquals(untouched.textureData.top,original)&&untouched.opacity==7&&untouched.lightEmission==9&&blocks.textureDataList.Count==3,"missing matches keep original references and gameplay properties");
   Check(ReferenceEquals(items.items[1].icon,diamond)&&ReferenceEquals(items.items[2].icon,original)&&items.items[0].icon==null,"item matching preserves missing icons and mesh block icons");
   Check(ReferenceEquals(blocks.textureDataList[2].textureData.specialTextures[0].image,torch),"special model slot receives matching image");
   Check(items.items[3].icon.width==16&&items.items[3].icon.height==16&&items.items[3].icon.pixels.All(p=>p.r==8),"animated item sheet becomes its top frame rather than a squeezed strip");
   Check(ReferenceEquals(hud.icons,icons)&&hud.blockBreak.width==160&&hud.blockBreak.height==16,"HUD icons and all ten breaking images are assigned");
   for(int i=0;i<10;i++)Check(hud.blockBreak.pixels[i*16].r==20+i,"breaking sheet preserves stage order");
   Check(material.mainTexture.width==64&&material.mainTexture.height==32&&material.mainTexture.pixels.All(p=>p.r==10),"modern square Steve skin converts to existing classic UV layout");
   Check(steve.height==64&&clock.height==512&&!steve.destroyed&&!clock.destroyed,"source textures are never cropped or destroyed in place");
   int imported=UnityEditor.AssetDatabase.imports;MinecraftTextureAutoAssign.Assign();
   Check(imported==UnityEditor.AssetDatabase.imports,"repeat assignment reuses stable generated images");
   int records=UnityEditor.Undo.records;UnityEditor.AssetDatabase.rootExists=false;MinecraftTextureAutoAssign.Assign();
   Check(records==UnityEditor.Undo.records,"missing local folder cannot mutate either catalogue");
   Check(RenderTexture.allocated==0,"texture extraction releases temporary render targets");
   Console.WriteLine(checks+" texture-name coverage, directory isolation, assignment and preservation checks passed (Unity editor mocked).");
  }
  finally{Directory.Delete(temp,true);}
 }
}
