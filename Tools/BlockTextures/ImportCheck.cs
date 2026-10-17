using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEditor
{
 public class InitializeOnLoadAttribute:Attribute {}
 public class MenuItem:Attribute {public MenuItem(string path){}}
 public static class EditorApplication {public static Action delayCall;}
 public class AssetPostprocessor {public string assetPath;public AssetImporter assetImporter;}
 public class AssetImporter
 {
  public static Dictionary<string,AssetImporter> importers=new Dictionary<string,AssetImporter>();
  public static AssetImporter GetAtPath(string path){AssetImporter value;return importers.TryGetValue(path,out value)?value:null;}
 }
 public enum TextureImporterNPOTScale {ToNearest,None}
 public enum TextureImporterCompression {Compressed,Uncompressed}
 public class TextureImporter:AssetImporter
 {
  public TextureImporterNPOTScale npotScale;
  public TextureImporterCompression textureCompression;
  public bool mipmapEnabled=true;
  public FilterMode filterMode=FilterMode.Bilinear;
  public int maxTextureSize=256,sourceWidth=16,sourceHeight=320,reimports;
  public Texture2D image;
  public void GetSourceTextureWidthAndHeight(out int width,out int height){width=sourceWidth;height=sourceHeight;}
  public void SaveAndReimport(){reimports++;image.width=sourceWidth;image.height=sourceHeight;}
 }
 public static class AssetDatabase
 {
  public static BlockDataSO data;
  public static Dictionary<UnityEngine.Object,string> paths=new Dictionary<UnityEngine.Object,string>();
  public static string[] FindAssets(string filter)=>new[]{"data"};
  public static string GUIDToAssetPath(string guid)=>"Assets/Block Data.asset";
  public static T LoadAssetAtPath<T>(string path)where T:class=>data as T;
  public static string GetAssetPath(UnityEngine.Object value)=>paths[value];
 }
}
public static class ImportCheck
{
 public static void Main()
 {
  var still=new Texture2D(16,256,TextureFormat.RGBA32,false,false);
  var untouched=new Texture2D(16,16,TextureFormat.RGBA32,false,false);
  var importer=new UnityEditor.TextureImporter{image=still};
  var other=new UnityEditor.TextureImporter{image=untouched};
  UnityEditor.AssetImporter.importers["Assets/lava_still.png"]=importer;
  UnityEditor.AssetImporter.importers["Assets/static.png"]=other;
  UnityEditor.AssetDatabase.paths[still]="Assets/lava_still.png";
  UnityEditor.AssetDatabase.paths[untouched]="Assets/static.png";
  var part=new SpecialBlockTexture{part=BlockTexturePart.FluidStill,image=still};
  var data=new BlockDataSO{textureDataList=new List<BlockTypeData>{
   new BlockTypeData{blockType=BlockType.Lava,textureData=new TextureData{animateFaces=true,top=still,bottom=still,specialTextures=new List<SpecialBlockTexture>{part}}},
   new BlockTypeData{blockType=BlockType.Stone,textureData=new TextureData{top=untouched}}}};
  UnityEditor.AssetDatabase.data=data;
  AnimatedBlockTextureImports.RepairAssignedSheets();
  if(importer.reimports!=1||still.width!=16||still.height!=320||importer.maxTextureSize<320||
     importer.npotScale!=UnityEditor.TextureImporterNPOTScale.None||importer.mipmapEnabled||
     importer.filterMode!=FilterMode.Point||importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed)
   throw new Exception("Assigned strip dimensions and lossless pixel import must be repaired once.");
  AnimatedBlockTextureImports.RepairAssignedSheets();
  if(importer.reimports!=1||other.reimports!=0||data.textureDataList.Count!=2||
     !ReferenceEquals(part.image,still)||!ReferenceEquals(data.textureDataList[0].textureData.top,still)||still.destroyed)
   throw new Exception("Repair must be idempotent and preserve assignments and unrelated static textures.");
  var processor=new AnimatedBlockTextureImports{assetPath="Assets/lava_still.png",assetImporter=importer};
  importer.npotScale=UnityEditor.TextureImporterNPOTScale.ToNearest;
  typeof(AnimatedBlockTextureImports).GetMethod("OnPreprocessTexture",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(processor,null);
  if(importer.npotScale!=UnityEditor.TextureImporterNPOTScale.None)throw new Exception("New fluid strip imports must preserve non-power-of-two dimensions.");
  Console.WriteLine("Animated strip import repair, preservation and idempotence checks passed (Unity importer mocked).");
 }
}
