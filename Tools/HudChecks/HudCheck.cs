using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace UnityEngine
{
 public class Object {public bool destroyed;public static void Destroy(Object o){o.destroyed=true;var go=o as GameObject;if(go!=null&&go.transform.parent!=null)go.transform.parent.children.Remove(go.transform);} }
 public class ScriptableObject:Object {}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component{return gameObject.GetComponent<T>();}}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public class SerializeField:Attribute {}
 public class Tooltip:Attribute {public Tooltip(string value){}}
 public class Material:Object {public Material(){}public Material(Material source){}public void DisableKeyword(string n){}public void EnableKeyword(string n){}public void SetColor(string n,Color c){}public void SetFloat(string n,float f){}public float GetFloat(string n)=>1;}
 public class AudioClip:Object {}
 public class Font:Object {}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;this.width=w;this.height=h;} }
 public struct Color {public static Color white=>new Color();public static Color black=>new Color();public static implicit operator Color(Color32 c)=>new Color();}
 public struct Color32 {public Color32(byte r,byte g,byte b,byte a){} }
 public class Transform:Component
 {
  public Transform parent;public List<Transform> children=new List<Transform>();
  public void SetParent(Transform p,bool keep){parent=p;p.children.Add(this);}
  public Transform Find(string name){return children.Find(t=>t.gameObject.name==name);}
 }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
 public class GameObject:Object
 {
  public string name;public int layer;public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
  public RectTransform transform;public List<Component> components=new List<Component>();
  public GameObject(string name,params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};components.Add(transform);}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>() where T:Component{return (T)components.Find(c=>c is T);}
  public void SetActive(bool value){activeSelf=value;}
 }
 public class Texture2D:Object {public int width=256,height=256;}
 public static class Resources {public static Object asset;public static T Load<T>(string name) where T:Object{return (T)asset;} }
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int FloorToInt(float n)=>(int)Math.Floor(n);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int n,int min,int max)=>Math.Max(min,Math.Min(n,max));public static int CeilToInt(float n)=>(int)Math.Ceiling(n);}
 public static class Time {public static float unscaledTime;}
 public static class Debug {public static void LogWarning(string message,Object o){}public static void LogError(string message,Object o){} }
}
namespace UnityEngine.UI
{
 public class RawImage:Component {public Texture2D texture;public bool raycastTarget,enabled=true;public Rect uvRect;public RectTransform rectTransform=>(RectTransform)transform;}
 public class Shadow:Component {public Color effectColor;public Vector2 effectDistance;}
}
namespace TMPro
{
 public class TMP_FontAsset:UnityEngine.Object {public Material material=new Material();public FaceInfo faceInfo=new FaceInfo();public class FaceInfo {public float pointSize=8;}}
 public enum TextAlignmentOptions {Center}
 public class TextMeshProUGUI:Component {public TMP_FontAsset font;public Material fontSharedMaterial;public float fontSize;public TextAlignmentOptions alignment;public Color color;public bool raycastTarget,extraPadding;public string text;}
}
public class Player {public int health=20,hunger=20,experienceLevel,airSupply=300;public float experienceProgress,saturation=5;}
public class GameManager {public static GameManager Instance;public Player localPlayer;}
public static class HudCheck
{
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Call(object target,string name){target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);}
 static GameObject Child(GameObject parent,string name){return parent.transform.Find(name).gameObject;}
 static T Get<T>(GameObject go) where T:Component {return (T)go.components.Find(c=>c is T);}
 public static void Main()
 {
  for(int points=-2;points<=22;points++)
  {int sum=0,half=0;for(int i=0;i<10;i++){int iconValue=SurvivalHudValues.IconFill(points,i);sum+=iconValue;if(iconValue==1)half++;}Check(sum==SurvivalHudValues.ClampPoints(points),"ten icons preserve points");Check(half==SurvivalHudValues.ClampPoints(points)%2,"odd points give exactly one half icon");}
  Check(SurvivalHudValues.ExperiencePixels(float.NaN)==0&&SurvivalHudValues.ExperiencePixels(-1)==0,"invalid/negative XP empty");
  Check(SurvivalHudValues.ExperiencePixels(0.5f)==91&&SurvivalHudValues.ExperiencePixels(1)==182&&SurvivalHudValues.ExperiencePixels(float.PositiveInfinity)==182,"XP pixel clipping");
  var ui=new GameObject("GameUI");var hotbar=new GameObject("Hotbar");hotbar.transform.SetParent(ui.transform,false);hotbar.transform.anchoredPosition=new Vector2(3,4);
  Resources.asset=new PlayerHudAssets{icons=new Texture2D(),levelFont=new TMPro.TMP_FontAsset()};
  PlayerSurvivalHUD.EnsureOn(ui.transform);var hud=ui.GetComponent<PlayerSurvivalHUD>();
  Check(hud!=null&&hud.enabled,"inventory startup repairs a scene with no survival HUD component");
  hud.enabled=false;PlayerSurvivalHUD.EnsureOn(ui.transform);
  Check(hud.enabled&&ui.components.FindAll(c=>c is PlayerSurvivalHUD).Count==1,"HUD attachment is idempotent and restores a disabled component");
  Call(hud,"Start");var root=Child(ui,"SurvivalHUD");
  Check(root.transform.pivot.x==0.5f,"HUD centered relative to hotbar");
  Check(!root.activeSelf,"HUD hidden before local player exists");
  GameManager.Instance=new GameManager{localPlayer=new Player{health=19,hunger=5,experienceLevel=7,experienceProgress=0.5f}};Call(hud,"LateUpdate");
  Check(root.activeSelf&&root.transform.anchoredPosition.x==3&&root.transform.anchoredPosition.y==4,"HUD follows hotbar");
  Check(Get<RawImage>(Child(root,"Heart9")).uvRect.x==61/256f,"nineteen health gives final half heart");
  Check(Get<RawImage>(Child(root,"Food2")).uvRect.x==61/256f&&!Get<RawImage>(Child(root,"Food3")).enabled,"five hunger shows half third food icon");
  var fill=Get<RawImage>(Child(root,"XPFill"));Check(fill.rectTransform.sizeDelta.x==91&&fill.uvRect.width==91/256f,"half XP crops fill and UV equally");
  Check(Get<TMPro.TextMeshProUGUI>(Child(root,"XPLevel")).text=="7","level displayed");
  var textMaterial=Get<TMPro.TextMeshProUGUI>(Child(root,"XPLevel")).fontSharedMaterial;
  Check(textMaterial!=null&&!ReferenceEquals(textMaterial,((PlayerHudAssets)Resources.asset).levelFont.material),"XP owns its TMP shadow material without modifying the shared font");
  foreach(var child in root.transform.children) foreach(var c in child.gameObject.components) if(c is RawImage)Check(!((RawImage)c).raycastTarget,"HUD images do not block input");
  var oldCount=root.transform.children.Count;Call(hud,"LateUpdate");Check(root.transform.children.Count==oldCount,"unchanged refresh does not create UI objects");
  Time.unscaledTime=.05f;GameManager.Instance.localPlayer.health=17;Call(hud,"LateUpdate");
  Time.unscaledTime=.2f;Call(hud,"LateUpdate");
  Check(Get<RawImage>(Child(root,"DamageHeart9")).enabled&&Get<RawImage>(Child(root,"DamageHeart9")).uvRect.x==79/256f,"damage flash retains the previous half heart in white");
  Check(Get<RawImage>(Child(root,"HeartBackground0")).uvRect.x==25/256f,"damage flash uses the highlighted heart container");
  Time.unscaledTime=1.3f;Call(hud,"LateUpdate");Check(!Get<RawImage>(Child(root,"DamageHeart9")).enabled,"damage flash ends on the presentation clock");
  GameManager.Instance.localPlayer.health=4;GameManager.Instance.localPlayer.hunger=0;GameManager.Instance.localPlayer.saturation=0;Time.unscaledTime=1.4f;Call(hud,"LateUpdate");
  bool heartMoved=false,foodMoved=false;
  for(int i=0;i<10;i++){heartMoved|=Child(root,"Heart"+i).transform.anchoredPosition.y!=30;foodMoved|=Child(root,"FoodBackground"+i).transform.anchoredPosition.y!=30;}
  Check(heartMoved&&foodMoved,"critical health and exhausted hunger animate complete icon stacks");
  GameManager.Instance.localPlayer.health=19;GameManager.Instance.localPlayer.hunger=5;GameManager.Instance.localPlayer.saturation=5;Time.unscaledTime=1.45f;Call(hud,"LateUpdate");
  Check(Child(root,"Heart0").transform.anchoredPosition.y==30&&Child(root,"FoodBackground0").transform.anchoredPosition.y==30,"recovery clears jitter offsets");
  var anim=new SurvivalHudAnimation();anim.Reset(20,0);anim.Observe(16,1);anim.Observe(12,5);Check(anim.PreviousHealth==20,"repeated hits retain the original previous health during the damage window");
  anim.Observe(12,26);Check(anim.PreviousHealth==12&&!anim.Flash(26),"old health settles after the one-second delay");
  anim.Reset(0,30);anim.Observe(20,31);Check(!anim.Flash(31)&&anim.PreviousHealth==20,"respawn cannot carry a death flash into the new life");
  GameManager.Instance.localPlayer.airSupply=150;Call(hud,"LateUpdate");
  Check(Child(root,"Air0").activeSelf&&Child(root,"Air4").activeSelf&&!Child(root,"Air5").activeSelf,"half air supply displays five bubbles");
  GameManager.Instance.localPlayer.airSupply=300;Call(hud,"LateUpdate");Check(!Child(root,"Air0").activeSelf,"air HUD hides when breath refills");
  GameManager.Instance.localPlayer=new Player();Call(hud,"LateUpdate");Check(Get<RawImage>(Child(root,"Heart9")).uvRect.x==52/256f&&!fill.enabled,"new player resets health and XP display");
  var air=(RawImage[])typeof(PlayerSurvivalHUD).GetField("bubbles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
  air[0]=null;Call(hud,"LateUpdate");var repaired=Child(ui,"SurvivalHUD");
  Check(root.destroyed&&repaired!=root&&repaired.activeSelf,"script reload with retained root and missing runtime references rebuilds HUD");
  Check(ui.transform.children.FindAll(t=>t.gameObject.name=="SurvivalHUD").Count==1,"repair does not leave duplicate visible HUD roots");
  root=repaired;
  hotbar.SetActive(false);Call(hud,"LateUpdate");Check(!root.activeSelf,"hidden hotbar hides HUD");
  Call(hud,"OnDestroy");Check(root.destroyed&&textMaterial.destroyed&&!((PlayerHudAssets)Resources.asset).levelFont.material.destroyed,"runtime UI owner releases its text material and preserves the source font");
  var lateUI=new GameObject("GameUI");var lateBar=new GameObject("Hotbar");lateBar.transform.SetParent(lateUI.transform,false);
  var loadedAssets=Resources.asset;Resources.asset=null;PlayerSurvivalHUD.EnsureOn(lateUI.transform);
  var lateHud=lateUI.GetComponent<PlayerSurvivalHUD>();Call(lateHud,"Start");
  Check(lateUI.transform.Find("SurvivalHUD")==null,"missing resources defer HUD creation safely");
  Resources.asset=loadedAssets;Call(lateHud,"LateUpdate");
  Check(Child(lateUI,"SurvivalHUD").activeSelf,"late-loaded HUD assets recover without a scene reload");
  Console.WriteLine(checks+" production HUD state/crop/layout/lifecycle checks passed (Unity UI mocked).");
 }
}
