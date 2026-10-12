using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace UnityEngine
{
 public class Object {public bool destroyed;public static void Destroy(Object o){o.destroyed=true;} }
 public class ScriptableObject:Object {}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component {}
 public class SerializeField:Attribute {}
 public class Tooltip:Attribute {public Tooltip(string value){}}
 public class Material:Object {}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;this.width=w;this.height=h;} }
 public struct Color {public static Color black=>new Color();public static implicit operator Color(Color32 c)=>new Color();}
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
  public void SetActive(bool value){activeSelf=value;}
 }
 public class Texture2D:Object {public int width=256,height=256;}
 public static class Resources {public static Object asset;public static T Load<T>(string name) where T:Object{return (T)asset;} }
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);}
 public static class Debug {public static void LogWarning(string message,Object o){}public static void LogError(string message,Object o){} }
}
namespace UnityEngine.UI
{
 public class RawImage:Component {public Texture2D texture;public bool raycastTarget,enabled=true;public Rect uvRect;public RectTransform rectTransform=>(RectTransform)transform;}
 public class Shadow:Component {public Color effectColor;public Vector2 effectDistance;}
}
namespace TMPro
{
 public class TMP_FontAsset:UnityEngine.Object {}
 public enum TextAlignmentOptions {Center}
 public class TextMeshProUGUI:Component {public TMP_FontAsset font;public float fontSize;public TextAlignmentOptions alignment;public Color color;public bool raycastTarget;public string text;}
}
public class Player {public int health=20,hunger=20,experienceLevel;public float experienceProgress;}
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
  var hud=ui.AddComponent<PlayerSurvivalHUD>();Call(hud,"Start");var root=Child(ui,"SurvivalHUD");
  Check(root.transform.pivot.x==0.5f,"HUD centered relative to hotbar");
  Check(!root.activeSelf,"HUD hidden before local player exists");
  GameManager.Instance=new GameManager{localPlayer=new Player{health=19,hunger=5,experienceLevel=7,experienceProgress=0.5f}};Call(hud,"LateUpdate");
  Check(root.activeSelf&&root.transform.anchoredPosition.x==3&&root.transform.anchoredPosition.y==4,"HUD follows hotbar");
  Check(Get<RawImage>(Child(root,"Heart9")).uvRect.x==61/256f,"nineteen health gives final half heart");
  Check(Get<RawImage>(Child(root,"Food2")).uvRect.x==61/256f&&!Get<RawImage>(Child(root,"Food3")).enabled,"five hunger shows half third food icon");
  var fill=Get<RawImage>(Child(root,"XPFill"));Check(fill.rectTransform.sizeDelta.x==91&&fill.uvRect.width==91/256f,"half XP crops fill and UV equally");
  Check(Get<TMPro.TextMeshProUGUI>(Child(root,"XPLevel")).text=="7","level displayed");
  foreach(var child in root.transform.children) foreach(var c in child.gameObject.components) if(c is RawImage)Check(!((RawImage)c).raycastTarget,"HUD images do not block input");
  var oldCount=root.transform.children.Count;Call(hud,"LateUpdate");Check(root.transform.children.Count==oldCount,"unchanged refresh does not create UI objects");
  GameManager.Instance.localPlayer=new Player();Call(hud,"LateUpdate");Check(Get<RawImage>(Child(root,"Heart9")).uvRect.x==52/256f&&!fill.enabled,"new player resets health and XP display");
  hotbar.SetActive(false);Call(hud,"LateUpdate");Check(!root.activeSelf,"hidden hotbar hides HUD");
  Call(hud,"OnDestroy");Check(root.destroyed,"runtime UI owner destroys root");
  Console.WriteLine(checks+" production HUD state/crop/layout/lifecycle checks passed (Unity UI mocked).");
 }
}
