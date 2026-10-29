using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public class Object {public string name;public bool destroyed;public static void Destroy(Object o){o.destroyed=true;}}
 public class Texture:Object {}
 public class Texture2D:Texture {public static Texture2D whiteTexture=new Texture2D();}
 public class Material:Object {public Texture mainTexture;public Material(Material source){mainTexture=source==null?null:source.mainTexture;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}}
 public class MaterialPropertyBlock {public Vector4 light;public void SetVector(string key,Vector4 value){light=value;}}
 public class MeshRenderer:Object {public Material sharedMaterial;public Vector4 light;public void GetPropertyBlock(MaterialPropertyBlock b){b.light=light;}public void SetPropertyBlock(MaterialPropertyBlock b){light=b.light;}}
 public class Transform:Object,IEnumerable
 {
  public List<Transform> children=new List<Transform>();public List<MeshRenderer> renderers=new List<MeshRenderer>();
  public IEnumerator GetEnumerator()=>children.GetEnumerator();
  public T[] GetComponentsInChildren<T>(bool inactive){var result=new List<T>();foreach(var r in renderers)result.Add((T)(object)r);foreach(var c in children)result.AddRange(c.GetComponentsInChildren<T>(inactive));return result.ToArray();}
 }
 public static class Resources {public static Dictionary<string,Object> assets=new Dictionary<string,Object>();public static T Load<T>(string key)where T:Object=>assets.ContainsKey(key)?(T)assets[key]:null;}
 public static class Debug {public static void LogWarning(string text){}}
}
public class PlayerHudAssets:UnityEngine.Object {public Texture2D inventoryPlayerSkin;}
public class PlayerObjects {public Transform body,head,leftArm,rightArm,leftLeg,rightLeg;}
public partial class Player:UnityEngine.Object
{
 public PlayerObjects objects=new PlayerObjects();
 public void StartForCheck()=>InitializeBodySkin();public void LightForCheck()=>UpdateBodySkinLight(9,3);public void EndForCheck()=>DisposeBodySkin();
}
class Check
{
 static void Assert(bool ok,string label){if(!ok)throw new Exception(label);}
 static Transform Part(string name)=>new Transform{name=name,renderers=new List<MeshRenderer>{new MeshRenderer()}};
 static void Main()
 {
  var skin=new Texture2D();var template=new Material(null);
  Resources.assets["PlayerHudAssets"]=new PlayerHudAssets{inventoryPlayerSkin=skin};Resources.assets["PlayerBodyMaterial"]=template;
  var p=new Player();p.objects.body=new Transform();
  var parts=new[]{p.objects.head=Part("Head"),p.objects.leftArm=Part("Left arm"),p.objects.rightArm=Part("Right arm"),p.objects.leftLeg=Part("Left leg"),p.objects.rightLeg=Part("Right leg"),Part("Torso")};
  p.objects.body.children.AddRange(parts);var camera=Part("Camera");p.objects.body.children.Add(camera);
  p.StartForCheck();var owned=parts[0].renderers[0].sharedMaterial;
  foreach(var part in parts)Assert(part.renderers[0].sharedMaterial==owned&&owned.mainTexture==skin,"all six parts use inventory skin");
  Assert(camera.renderers[0].sharedMaterial==null,"held-item camera renderers untouched");
  Assert(template.mainTexture==null&&owned!=template,"shared material asset is not mutated");
  p.LightForCheck();foreach(var part in parts)Assert(part.renderers[0].light.x==9&&part.renderers[0].light.y==3&&part.renderers[0].light.z==1,"body retains world lighting");
  p.EndForCheck();Assert(owned.destroyed&&!skin.destroyed&&!template.destroyed,"only owned material destroyed");
  Resources.assets.Remove("PlayerHudAssets");Resources.assets["InventoryPlayerMaterial"]=new Material(null){mainTexture=skin};
  p.StartForCheck();Assert(parts[0].renderers[0].sharedMaterial.mainTexture==skin,"inventory material fallback matches preview");p.EndForCheck();
  Console.WriteLine("PASS: shared body skin, six body parts, renderer isolation, lighting, fallback, and cleanup.");
 }
}
