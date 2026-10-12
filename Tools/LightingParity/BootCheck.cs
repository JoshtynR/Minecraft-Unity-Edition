using System;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
 public class Object { public bool destroyed; public static void Destroy(Object value){value.destroyed=true;} }
 public class MonoBehaviour:Object {}
 public class Texture:Object {}
 public class Texture2D:Texture {public TextureWrapMode wrapMode;public FilterMode filterMode;}
 public enum TextureWrapMode {Repeat,Clamp}
 public enum FilterMode {Point,Bilinear}
 public class Material:Object
 {
  public bool supportsLut;public Texture texture;
  public bool HasProperty(int id)=>supportsLut;
  public Texture GetTexture(int id)=>texture;
  public void SetTexture(int id,Texture value){texture=value;}
 }
 public static class Shader {public static int PropertyToID(string value)=>1;}
 public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int value){}}
}
public static class LightTextureCreator
{
 public static int creates,builds;
 public static void CreateLightTexture(){creates++;}
 public static Texture2D BuildLightTex2D(){builds++;return new Texture2D();}
}
public static class BootCheck
{
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Call(LightTexBoot component,string method){typeof(LightTexBoot).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);}
 public static void Main()
 {
  var missing=new LightTexBoot();Call(missing,"Awake");Call(missing,"OnDestroy");
  var modern=new LightTexBoot{Main_Mat=new Material()};Call(modern,"Awake");Call(modern,"OnDestroy");
  Check(LightTextureCreator.creates==0&&LightTextureCreator.builds==0,"modern shader and missing material skip LUT allocation");
  var previous=new Texture2D();var material=new Material{supportsLut=true,texture=previous};var legacy=new LightTexBoot{Main_Mat=material};Call(legacy,"Awake");
  var owned=material.texture as Texture2D;
  Check(owned!=null&&owned!=previous,"legacy LUT binding");
  Check(owned.wrapMode==TextureWrapMode.Clamp&&owned.filterMode==FilterMode.Bilinear,"legacy sampling settings");
  Call(legacy,"OnDestroy");
  Check(owned.destroyed&&material.texture==previous&&!previous.destroyed,"restore prior texture and destroy only owned LUT");
  Call(legacy,"OnDestroy");Check(material.texture==previous,"cleanup remains idempotent");
  var changed=new LightTexBoot{Main_Mat=material};Call(changed,"Awake");owned=(Texture2D)material.texture;
  var replacement=new Texture2D();material.texture=replacement;Call(changed,"OnDestroy");
  Check(material.texture==replacement&&owned.destroyed&&!replacement.destroyed,"cleanup preserves external replacement");
  Console.WriteLine(checks+" LightTexBoot compatibility/ownership checks passed (Unity GPU mocked).");
 }
}
