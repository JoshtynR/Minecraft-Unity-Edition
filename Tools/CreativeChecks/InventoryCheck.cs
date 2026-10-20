using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace JetBrains.Annotations {public class CanBeNullAttribute:Attribute {}}
namespace Mirror
{
 public class CommandAttribute:Attribute {}public class TargetRpcAttribute:Attribute {}
 public class NetworkConnection {}
 public interface NetworkMessage {}
 public class ClientRpcAttribute:Attribute {}
 public class NetworkConnectionToClient {public GameObject identity;public int connectionId;public ChestReply last;public FurnaceReply lastFurnace;public void Send(ChestReply reply){last=reply;}public void Send(FurnaceReply reply){lastFurnace=reply;}}
 public static class NetworkServer {public static void SendToAll<T>(T message){}public static bool active;public static Dictionary<int,NetworkConnectionToClient> connections=new Dictionary<int,NetworkConnectionToClient>();}
 public static class NetworkClient {public static void RegisterHandler<T>(Action<T> callback){}public static bool ready=true;public static bool active;public static ChestRequest last;public static FurnaceRequest lastFurnace;public static void Send(FurnaceRequest request){lastFurnace=request;}public static void Send(DropSyncRequest request){}public static void Send(ChestRequest request){last=request;}}
}
namespace UnityEngine
{
 public class AudioClip:Object {}
 public class AudioSource {public static void PlayClipAtPoint(AudioClip clip,Vector3 position,float volume){}}
 public class Font:Object {}
 public class Object
 {
  public static bool deferDestroy;public string name;public bool destroyed;
  public static void Destroy(Object value){if(value==null)return;value.destroyed=true;var go=value as GameObject;if(go!=null&&!deferDestroy&&go.transform.parent!=null)go.transform.parent.children.Remove(go.transform);}
  public static GameObject Instantiate(GameObject original,Transform parent)
  {
   var go=new GameObject(original.name,original.components.Select(c=>c.GetType()).ToArray());go.layer=original.layer;go.transform.SetParent(parent,false);return go;
  }
 }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 [AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=true)]
 public class RequireComponent:Attribute {public Type required,second;public RequireComponent(Type type){required=type;}public RequireComponent(Type type,Type other){required=type;second=other;}}
 public class CanvasRenderer:Component {}
 public class ScriptableObject:Object {}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public class Texture:Object {public int width=64,height=32;}
 public class Texture2D:Texture {public static readonly Texture2D whiteTexture=new Texture2D{width=1,height=1};}
 public static class Debug {public static void LogWarning(string value){}}
 public struct UIVertex {public Vector3 position;public Vector2 uv0;public Color color;public static UIVertex simpleVert=>new UIVertex();}
 public class Material:Object {public Texture2D mainTexture=new Texture2D();public Dictionary<string,float> floats=new Dictionary<string,float>();public Dictionary<string,Color> colors=new Dictionary<string,Color>();public HashSet<string> keywords=new HashSet<string>();public Material(){}public Material(Material value){mainTexture=value.mainTexture;floats=new Dictionary<string,float>(value.floats);colors=new Dictionary<string,Color>(value.colors);keywords=new HashSet<string>(value.keywords);}public void SetFloat(string n,float v){floats[n]=v;}public float GetFloat(string n)=>floats.ContainsKey(n)?floats[n]:1;public void SetColor(string n,Color v){colors[n]=v;}public void EnableKeyword(string n){keywords.Add(n);}public void DisableKeyword(string n){keywords.Remove(n);}}
 public class Mesh:Object
 {
  public UnityEngine.Rendering.IndexFormat indexFormat;public int subMeshCount;public void Clear(){vertices=new List<Vector3>();}public void MarkDynamic(){}
  public List<Vector3> vertices;public void SetVertices(List<Vector3> v){vertices=v;}public void SetUVs<T>(int channel,List<T> uv){}public void SetUVs(int channel,Vector2[] uv){}public void SetTriangles(List<int> t,int sub){}public void SetColors(List<Color> c){}public void RecalculateNormals(){}public void RecalculateBounds(){}
 }
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MeshRenderer:Component {public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;public Material sharedMaterial;public void SetPropertyBlock(MaterialPropertyBlock properties){}}
 public class MaterialPropertyBlock {public void SetFloat(string key,float value){}}
 public static class Application {public static bool isPlaying=true;}
 public static class Random {public static int Range(int a,int b)=>a;public static float Range(float a,float b)=>(a+b)/2;}
 public static class Time {public static float deltaTime=.05f,unscaledTime;}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public class HideInInspector:Attribute {}
 public class RangeAttribute:Attribute {public RangeAttribute(int a,int b){}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color black=>new Color(0,0,0,1);public static Color white=>new Color(1,1,1,1);public static Color clear=>new Color(0,0,0,0);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 [Serializable] public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator*(Vector3 v,float s)=>new Vector3(v.x*s,v.y*s,v.z*s);
  public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
  public Vector3 normalized=>magnitude<.000001f?zero:new Vector3(x/magnitude,y/magnitude,z/magnitude);
  public static Vector3 operator/(Vector3 v,float n)=>new Vector3(v.x/n,v.y/n,v.z/n);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
 }
 [Serializable] public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}public static Vector3Int zero=>new Vector3Int();public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);
  public static implicit operator Vector3(Vector3Int v)=>new Vector3(v.x,v.y,v.z);
  public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static bool operator==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator!=(Vector3Int a,Vector3Int b)=>!(a==b);public override bool Equals(object value)=>value is Vector3Int&&(Vector3Int)value==this;public override int GetHashCode()=>x*31+y*17+z;
 }
 public struct Vector4 {public static Vector4 zero=>new Vector4();public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
 public struct Quaternion {public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}}
 public static class Mathf {public const float PI=(float)Math.PI;public const float Rad2Deg=180f/PI;public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public static float Max(float a,float b)=>Math.Max(a,b);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);public static int Max(int a,int b)=>Math.Max(a,b);public static float MoveTowards(float a,float b,float step)=>a<b?Math.Min(b,a+step):Math.Max(b,a-step);public static float Clamp01(float x)=>Clamp(x,0,1);public static float Pow(float x,float y)=>(float)Math.Pow(x,y);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));}
 public class Transform:Component
 {
  public Vector3 forward=new Vector3(0,0,1),eulerAngles;public Transform parent;public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation,rotation;public List<Transform> children=new List<Transform>();public int childCount=>children.Count;
  public Vector3 position {get=>parent==null?localPosition:parent.position+localPosition;set{var p=parent==null?new Vector3():parent.position;localPosition=new Vector3(value.x-p.x,value.y-p.y,value.z-p.z);}}
  public Transform GetChild(int i)=>children[i];public void SetParent(Transform p,bool world){if(parent!=null)parent.children.Remove(this);parent=p;if(p!=null)p.children.Add(this);}
  public void SetAsLastSibling(){parent.children.Remove(this);parent.children.Add(this);}
  public Transform Find(string path){Transform current=this;foreach(var name in path.Split('/')){current=current.children.Find(c=>c.gameObject.name==name);if(current==null)return null;}return current;}
 }
 public class RectTransform:Transform
 {
  public Vector2 anchorMin,anchorMax,pivot,sizeDelta;
  public Vector2 anchoredPosition {get=>new Vector2(localPosition.x,localPosition.y);set{localPosition=new Vector3(value.x,value.y,localPosition.z);}}
  public Rect rect=>new Rect(0,0,sizeDelta.x,sizeDelta.y);
 }
 public class GameObject:Object
 {
  static List<GameObject> all=new List<GameObject>();public int layer;public bool activeSelf=true;public RectTransform transform;public List<Component> components=new List<Component>();
  public GameObject(string name,params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};all.Add(this);foreach(var t in types)if(t!=typeof(RectTransform))Add(t);}
  Component Add(Type type)
  {
   foreach(RequireComponent requirement in type.GetCustomAttributes(typeof(RequireComponent),true))
   {
    if(!components.Any(c=>requirement.required.IsInstanceOfType(c)))Add(requirement.required);
    if(requirement.second!=null&&!components.Any(c=>requirement.second.IsInstanceOfType(c)))Add(requirement.second);
   }
   var value=(Component)Activator.CreateInstance(type);value.gameObject=this;components.Add(value);return value;
  }
  public T AddComponent<T>()where T:Component,new()=>(T)Add(typeof(T));public T GetComponent<T>()where T:Component=>typeof(T)==typeof(RectTransform)?(T)(Component)transform:(T)components.Find(c=>c is T);
  public void SetActive(bool value){activeSelf=value;}public static GameObject Find(string name)=>all.Find(o=>o.name==name&&o.activeSelf&&!o.destroyed);
 }
 public static class Resources
 {
  public static PlayerHudAssets hud;public static bool omitPreviewMaterial;
  public static T Load<T>(string name)where T:Object
  {
   if(name=="DroppedItemMaterial"||name=="DroppedBlockMaterial")return (T)(Object)new Material();
   if(name=="InventoryPlayerMaterial"&&omitPreviewMaterial)return null;
   return name=="InventoryPlayerMaterial"||name=="ModernChestMaterial"?(T)(Object)new Material():name=="PlayerHudAssets"?(T)(Object)hud:null;
  }
 }
 public enum KeyCode {Alpha1=49,E=69,Q=81,LeftShift=304,RightShift=303}
 public static class Input {public static Vector3 mousePosition;public static bool GetKeyDown(KeyCode k)=>false;public static bool GetKey(KeyCode k)=>false;public static float GetAxis(string n)=>0;}
 public enum RenderMode {ScreenSpaceOverlay,ScreenSpaceCamera}
 public class Camera:Component {public static Camera main;}
 public class Canvas:Component {public RenderMode renderMode;public Camera worldCamera;}
 public static class RectTransformUtility {public static bool ScreenPointToLocalPointInRectangle(RectTransform r,Vector3 p,Camera camera,out Vector2 point){point=new Vector2(p.x,p.y);return true;}}
 public enum CursorLockMode {None,Locked}
 public static class Cursor {public static CursorLockMode lockState;public static bool visible;}
}
namespace UnityEngine.Events {public delegate void UnityAction();}
namespace UnityEngine.UI
{
 public class MaskableGraphic:Component
 {
  public Color color=Color.white;public bool raycastTarget=true;public int dirty;
  public virtual Texture mainTexture=>null;public void SetVerticesDirty(){dirty++;}public void SetMaterialDirty(){}
  protected virtual void OnPopulateMesh(VertexHelper helper){}
 }
 public class VertexHelper
 {
  public readonly List<UIVertex> vertices=new List<UIVertex>();public readonly List<int> triangles=new List<int>();
  public int currentVertCount=>vertices.Count;public void Clear(){vertices.Clear();triangles.Clear();}
  public void AddVert(UIVertex v){vertices.Add(v);}public void AddTriangle(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
 }
 public class RawImage:Component {public Texture2D texture;public Rect uvRect;public bool raycastTarget=true;}
 public class Image:Component {public Color color;public bool raycastTarget=true;}
 public class Button:Component {public bool interactable=true;public Click onClick=new Click();public class Click {UnityEngine.Events.UnityAction action;public void AddListener(UnityEngine.Events.UnityAction a){action+=a;}public void Invoke(){action();}}}
}
namespace TMPro
{
 public class TMP_FontAsset:UnityEngine.Object {public Material material=new Material();public FaceInfo faceInfo=new FaceInfo();public class FaceInfo {public float pointSize=8;}}
 public enum TextAlignmentOptions {Center,BottomRight,Left}
 public class TextMeshProUGUI:Component {public TMP_FontAsset font=new TMP_FontAsset();public Material fontSharedMaterial;public Vector4 margin;public float fontSize;public string text;public Color color;public TextAlignmentOptions alignment;public bool raycastTarget,enableWordWrapping,enableExtraPadding,enableAutoSizing;public RectTransform rectTransform=>(RectTransform)transform;}
}
namespace UnityEngine.EventSystems
{
 public class PointerEventData {public enum InputButton {Left,Right,Middle}public InputButton button;}
 public interface IPointerEnterHandler {void OnPointerEnter(PointerEventData data);}
 public interface IPointerExitHandler {void OnPointerExit(PointerEventData data);}
 public interface IPointerDownHandler {void OnPointerDown(PointerEventData data);}
}
public enum Direction {down,up,backwards,forwards,left,right}
public class ChunkData
{
 public bool modifiedByPlayer;public Vector3Int worldPos;public World worldRef;
 public Block GetBlock(Vector3Int local)=>worldRef.GetBlock(worldPos+local);
}
public partial class Player:MonoBehaviour
{
 private bool serverMining;
 public bool escapeMenuOpen,networkStarted,isServer=true;public float reach=4.5f,eyeHeight=1.578f;public int health=20;
 public PlayerInventory inventory=>GetComponent<PlayerInventory>();public World world=>World.Instance;
 public Transform cam;public Mirror.NetworkConnection connectionToClient=new Mirror.NetworkConnection();
}
public class GameManager {public static GameManager Instance;public Player localPlayer;}
public static class PlayerSurvivalHUD {public static int attaches;public static void EnsureOn(Transform parent){attaches++;}}
public class Block {public int sky=15,light;public Vector3Int globalWorldPosition;public BlockType type;public object blockShape;public Block(BlockType type,Vector3Int p,object section){this.type=type;globalWorldPosition=p;}public int GetSkyLight()=>sky;public int GetBlockLight()=>light;}
public static class BlockMapping {public static Block MapTypeToBlock(BlockType t,Block b)=>b;}
public class TextureData {public Texture2D top,bottom,side,front,back,left,right;}
public class BlockDefinition {public Color albedoTint;public TextureData textureData=new TextureData();}
public static class BlockDataManager {public static BlockDefinition[] blockTypeDataDictionary=new BlockDefinition[133];}
public static class BlockShapesExtensions {public static bool IsPlant(BlockType t)=>false;}
public static class SpecialBlockGeometry {public static object Shape(BlockType t,byte s)=>s;public static bool FlatShading(BlockType t)=>false;public static bool NormalCube(BlockType type)=>type==BlockType.Stone;public static byte State(BlockType type,object shape)=>shape is byte?(byte)shape:WheatCropRules.IsCrop(type)?WheatCropRules.LegacyAge(type):type==BlockType.MoistFarmland?(byte)7:(byte)2;}
public static class BlockHelper
{
 public static Direction[] directions=(Direction[])Enum.GetValues(typeof(Direction));
 public static void GetFaceDataIn(Direction d,Vector3Int p,MeshData m,Block b,BlockDefinition definition,object chunk){for(int i=0;i<4;i++)m.AddVertex(new Vector3());m.AddQuadTriangles();m.AddFaceUVs(0);m.AddQuadColor(new Color());}
}
public partial class PlayerInventory
{
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static GameObject Child(string name,Transform parent,params Type[] types){var go=new GameObject(name,types);go.layer=5;go.transform.SetParent(parent,false);return go;}
 static int Total(PlayerInventory inv,ItemType type)
 {
  int n=inv.slots.Where(s=>s.type==type).Sum(s=>s.count)+inv.craftingSlots.Where(s=>s.type==type).Sum(s=>s.count)+inv.equipmentSlots.Where(s=>s.type==type).Sum(s=>s.count)+inv.recoveryItems.Where(s=>s.type==type).Sum(s=>s.count);
  return n+(inv.pickedUpSlot!=null&&inv.pickedUpSlot.type==type?inv.pickedUpSlot.count:0);
 }
 static PlayerInventory BuildMenu()
 {
  for(int i=0;i<133;i++)BlockDataManager.blockTypeDataDictionary[i]=new BlockDefinition();
  var canvas=new GameObject("PlayerCanvas",typeof(Canvas));canvas.transform.sizeDelta=new Vector2(640,360);
  var ui=Child("GameUI",canvas.transform);var hotbar=Child("Hotbar",ui.transform);Child("Selected",hotbar.transform);
  for(int i=0;i<9;i++){var cell=Child("Hotbar"+i,hotbar.transform);Child("Count",cell.transform,typeof(TMPro.TextMeshProUGUI));}
  Child("Inventory",ui.transform).SetActive(false);
  var go=new GameObject("Player",typeof(Player));var inv=go.AddComponent<PlayerInventory>();
  inv.UIBlockPrefab=new GameObject("BlockIcon",typeof(MeshFilter));inv.SlotTextPrefab=new GameObject("SlotText",typeof(TMPro.TextMeshProUGUI));inv.Start();return inv;
 }
 static UnityEngine.UI.VertexHelper DrawPreview(InventoryPlayerGraphic graphic)
 {
  var helper=new UnityEngine.UI.VertexHelper();
  typeof(InventoryPlayerGraphic).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(graphic,new object[]{helper});
  return helper;
 }
 static void CheckCanvasPreview(PlayerInventory menu)
 {
  Check(menu.inventoryPreview.GetComponent<MeshRenderer>()==null&&menu.inventoryPreviewGraphic is UnityEngine.UI.MaskableGraphic,"paper doll uses canvas graphics instead of a camera-dependent world renderer");
  Check(menu.inventoryPreview.GetComponent<CanvasRenderer>()!=null,"custom player graphic must have a CanvasRenderer to reach Unity's UI render pipeline");
  Check(ReferenceEquals(menu.inventoryPreviewGraphic.mainTexture,menu.inventoryPreviewSkin)&&!menu.inventoryPreviewGraphic.raycastTarget,"canvas receives skin and preview cannot intercept inventory input");
  foreach(var mode in new[]{RenderMode.ScreenSpaceCamera,RenderMode.ScreenSpaceOverlay})
  foreach(float pitch in new[]{-12f,0f,12f})foreach(float yaw in new[]{-30f,0f,30f})
  {
   menu.inventoryCanvas.renderMode=mode;
   menu.inventoryPreviewGraphic.SetLook(pitch,yaw);
   var drawn=DrawPreview(menu.inventoryPreviewGraphic);
   Check(menu.inventoryPreviewGraphic.PopulateCount>0&&menu.inventoryPreviewGraphic.PreviewVertexCount==drawn.vertices.Count,"runtime diagnostic counters reflect actual canvas mesh population");
   Check(drawn.triangles.Count>=36&&drawn.vertices.Count>=36,"canvas emits visible skinned triangles for either canvas mode and all look limits");
   foreach(var v in drawn.vertices)
   {
    Check(Math.Abs(v.position.x)<25&&Math.Abs(v.position.y)<35&&v.position.z==0,"projected player fits window and stays on canvas plane");
    Check(v.uv0.x>=0&&v.uv0.x<=1&&v.uv0.y>=0&&v.uv0.y<=1&&v.color.a==1&&v.color.r>=.6f,"visible canvas vertices carry valid skin UVs and lighting");
   }
   foreach(int index in drawn.triangles)Check(index>=0&&index<drawn.vertices.Count,"canvas triangle indices valid");
  }
  int dirty=menu.inventoryPreviewGraphic.dirty;menu.inventoryPreviewGraphic.SetLook(12,30);
  Check(menu.inventoryPreviewGraphic.dirty==dirty,"unchanged cursor pose does not trigger mesh rebuild");
 }
 static void CheckInventoryArtwork()
 {
  foreach(int multiplier in new[]{1,2})
  {
   var sheet=new Texture2D{width=256*multiplier,height=256*multiplier};
   Resources.hud=new PlayerHudAssets{inventory=sheet};
   var menu=BuildMenu();var background=menu.inventoryRect.GetComponent<UnityEngine.UI.RawImage>();
   Check(background!=null&&ReferenceEquals(background.texture,sheet),"inventory uses configured sheet at native and HD sizes");
   Check(background.uvRect.x==0&&background.uvRect.y==90/256f&&background.uvRect.width==176/256f&&background.uvRect.height==166/256f,"only top-left survival panel is sampled; extra atlas widgets excluded");
   Check(menu.inventoryRect.Find("Bevel")==null&&menu.inventoryRect.Find("CraftArrow")==null,"atlas bevels and arrow are not painted over");
   for(int i=0;i<36;i++)
   {
    var cell=menu.inventorySlotsRect.GetChild(i);
    Check(cell.Find("Inset")==null&&cell.GetComponent<UnityEngine.UI.Image>().color.a==0&&cell.GetComponent<InventorySlot>()!=null,"sheet slots retain invisible clickable targets without duplicate borders");
   }
   for(int i=0;i<4;i++)Check(menu.craftingRects[i].anchoredPosition.x==18+i%2*18&&menu.craftingRects[i].anchoredPosition.y==57-i/2*18,"craft controls align with atlas pixel centers");
   Check(menu.craftResultRect.anchoredPosition.x==74&&menu.craftResultRect.anchoredPosition.y==47,"result control aligns with atlas output slot");
   var window=menu.inventoryRect.Find("PlayerPreview");
   Check(window.localPosition.x==-37&&window.localPosition.y==40&&window.GetComponent<UnityEngine.UI.Image>()==null,"preview sits in existing black sheet window");
   CheckCanvasPreview(menu);
   menu.OnDestroy();Check(!sheet.destroyed,"inventory cleanup preserves source sheet");
  }
  Resources.hud=null;
  Resources.omitPreviewMaterial=true;
  var directSkin=new Texture2D{width=128,height=128};Resources.hud=new PlayerHudAssets{inventoryPlayerSkin=directSkin};
  var directMenu=BuildMenu();
  Check(directMenu.inventoryPreview!=null&&ReferenceEquals(directMenu.inventoryPreviewGraphic.mainTexture,directSkin),"direct skin creates a preview even when the optional material cannot be loaded");
  CheckCanvasPreview(directMenu);directMenu.OnDestroy();
  Check(!directSkin.destroyed,"direct skin remains shared after inventory closes/destroys");
  Resources.hud=null;
  var missingSkinMenu=BuildMenu();
  Check(missingSkinMenu.inventoryPreview!=null&&ReferenceEquals(missingSkinMenu.inventoryPreviewGraphic.mainTexture,Texture2D.whiteTexture),"missing skin and missing material still construct a visible fallback model");
  CheckCanvasPreview(missingSkinMenu);missingSkinMenu.OnDestroy();Resources.omitPreviewMaterial=false;
 }
 public static void Main(string[] args)
 {
  CheckRules();CheckDurability();CheckDrops();CheckCraftingReference(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]),"beta-crafting.tsv"));CheckCrafting();CheckFurnaces();CheckFarming();CheckCows();CheckItems();CheckBetaItems();var inv=BuildMenu();CheckItemUI(inv);CheckChests(inv);
  var headless=new GameObject("DedicatedInventory").AddComponent<PlayerInventory>();
  Check(headless.AddItem(ItemType.Stone,64)&&headless.slots[0].count==64,"storage logic works without a canvas on nonlocal/server players");
  headless.LoadInventory(null);headless.AddItem(ItemType.Stone,63);headless.AddItem(ItemType.Dirt,35*64);
  Check(!headless.AddItem(ItemType.Stone,2)&&headless.slots[0].count==63,"bool add failure leaves partial-stack room unchanged");
  Check(headless.AddItem(ItemType.Stone,1)&&headless.slots[0].count==64,"exact-capacity add succeeds");
  Check(inv.slots.Length==36&&inv.inventorySlotsRect.childCount==36,"survival capacity is 27 backpack plus nine hotbar");
  Check(!inv.creativeRoot.gameObject.activeSelf&&inv.inventoryPreviewData.vertices.Count==144,"menu initially closed, paper doll is six skinned cuboids");
  CheckInventoryArtwork();
  inv.OpenInventory();Check(inv.inventoryRect.gameObject.activeSelf&&inv.creativePanel.gameObject.activeSelf&&Cursor.visible,"survival panel and catalogue open together");
  Check(inv.creativeBlocks.Count==239&&inv.creativePageSize<inv.creativeBlocks.Count&&inv.nextPage.interactable,"complete item catalogue pages on a normal 1080p GUI screen");
  Check(inv.inventoryRect.anchoredPosition.x+88<=inv.creativePanel.anchoredPosition.x-inv.creativePanel.sizeDelta.x/2-8,"sidebar does not overlap survival panel");
  for(int i=0;i<131;i++)
  {
   Check(inv.creativeCells[i].gameObject.activeSelf&&inv.creativeCells[i].childCount==1,"catalogue has one visible cached icon per block");
   Check(inv.creativeCells[i].GetChild(0).localPosition.z==-20,"catalogue icons render in front of backgrounds");
   Check(inv.creativeCells[i].GetChild(0).gameObject.layer==5,"catalogue meshes inherit the UI camera layer");
  }
  inv.GiveCatalogItem(0,false);var first=inv.creativeBlocks[0];Check(inv.slots[0].type==first&&inv.slots[0].count==64,"catalogue click grants full stack into inventory");
  foreach(var count in new[]{inv.slots[0].countText,inv.slots[0].hotbarCountText})
  {
   Check(count.transform.parent.GetChild(count.transform.parent.childCount-1)==count.transform,"stack counts draw after replacement item icons");
   Check(count.fontSize==8&&count.color.r==1&&count.color.g==1&&count.color.b==1&&count.rectTransform.localPosition.y==-5,"inventory and hotbar share white pixel-sized count placement");
   Check(count.fontSharedMaterial.keywords.Contains("UNDERLAY_ON")&&count.fontSharedMaterial.GetFloat("unity_GUIZTestMode")==8,"count shadows use TMP underlay and cannot fail icon depth tests");
  }
  inv.GiveCatalogItem(1,true);Check(inv.slots[0].type==first&&inv.slots[1].type==inv.creativeBlocks[1]&&inv.slots[1].count==1,"right-click gives one without replacing selected stack");
  inv.LoadInventory(null);Check(inv.slots.All(s=>s.type==ItemType.Nothing&&s.count==0),"missing save inventory loads empty");
  inv.SetCursor(new InventoryStack(ItemType.Stone,1));inv.ClickInventorySlot(0,true,false);
  Check(inv.pickedUpSlot==null&&inv.slots[0].count==1,"placing last carried item clears cursor without null access");inv.LoadInventory(null);
  inv.SetCursor(new InventoryStack(ItemType.Stone,1));inv.ClickInventorySlot(41,false,false);
  Check(inv.equipmentSlots[0].count==0&&inv.pickedUpSlot.count==1,"helmet slot rejects normal blocks");
  inv.SetCursor(new InventoryStack(ItemType.Pumpkin,64));inv.ClickInventorySlot(41,false,false);
  Check(inv.equipmentSlots[0].count==1&&inv.pickedUpSlot.count==63&&inv.inventoryPreviewHelmet.activeSelf,"Beta pumpkin helmet consumes one and appears on preview");
  Check(inv.inventoryHelmetGraphics.Length==6&&inv.inventoryHelmetGraphics.All(g=>g is UnityEngine.UI.MaskableGraphic),"equipped pumpkin also renders through canvas faces");
  Check(inv.inventoryHelmetGraphics.All(g=>g.GetComponent<CanvasRenderer>()!=null),"every pumpkin graphic must also have a CanvasRenderer");
  Check(inv.inventoryHelmetGraphics.Sum(g=>DrawPreview(g).triangles.Count)==6,"pumpkin front face reaches canvas at neutral pose");
  var helmet=inv.SavedEquipment();inv.LoadInventory(null,null,null,null,helmet);
  Check(inv.equipmentSlots[0].type==ItemType.Pumpkin&&inv.equipmentSlots[0].count==1,"equipment survives save/load separately from storage");
  inv.ClickInventorySlot(41,false,true);Check(inv.equipmentSlots[0].count==0&&inv.slots[0].type==ItemType.Pumpkin,"shift-removing equipment moves it into storage");inv.LoadInventory(null);
  inv.AddItem(ItemType.Stone,63);inv.ClickInventorySlot(0,false,false);inv.ClickInventorySlot(1,true,false);
  Check(inv.slots[1].count==1&&inv.pickedUpSlot.count==62,"right click places exactly one");
  inv.ClickInventorySlot(1,false,false);Check(inv.slots[1].count==63&&inv.pickedUpSlot==null,"normal merge empties cursor without duplicate");
  inv.ClickInventorySlot(1,true,false);Check(inv.pickedUpSlot.count==32&&inv.slots[1].count==31,"right click takes rounded-up half");
  inv.ClickInventorySlot(0,false,false);inv.ClickInventorySlot(0,false,true);
  Check(inv.slots[0].count==0&&inv.slots[9].count==32,"shift transfer targets backpack rather than hidden storage");
  Check(Total(inv,ItemType.Stone)==63,"clicks conserve all items");
  inv.LoadInventory(null);inv.SetCursor(new InventoryStack(ItemType.Log,2));inv.ClickInventorySlot(36,false,false);
  Check(inv.craftedOutput.type==ItemType.WoodenPlanks&&inv.craftedOutput.count==4,"single-log recipe preview");
  inv.ClickInventorySlot(40,false,false);Check(inv.pickedUpSlot.count==4&&inv.craftingSlots[0].count==1,"taking output consumes one ingredient");
  inv.SetCursor(new InventoryStack(ItemType.Stone,1));inv.ClickInventorySlot(40,false,false);Check(inv.craftingSlots[0].count==1,"incompatible cursor cannot consume recipe");
  inv.SetCursor(new InventoryStack());inv.ClickInventorySlot(40,false,true);Check(inv.slots[0].type==ItemType.WoodenPlanks&&inv.slots[0].count==4&&inv.craftingSlots[0].count==0,"shift craft consumes input and inserts only actual output");
  var old=new Slot[72];for(int i=0;i<72;i++)old[i]=new Slot{type=ItemType.Stone,count=64,index=i};inv.LoadInventory(old);
  Check(inv.slots.Length==36&&inv.recoveryItems.Count==36&&Total(inv,ItemType.Stone)==72*64,"old 72-slot inventory preserved without hidden live storage");
  inv.RemoveItem(0);inv.RecoverSavedItems();Check(inv.slots[0].count==64&&inv.recoveryItems.Count==35,"legacy recovery fills free slots only");
  inv.SetCursor(new InventoryStack(ItemType.Sand,1));inv.CloseInventory();Check(inv.pickedUpSlot!=null,"closing a full inventory preserves cursor");
  var snapshot=new WorldServer.SavePlayerMessage(new Vector3(),new Quaternion(),new Quaternion(),inv.SavedInventory(),1,inv.SavedCrafting(),inv.SavedCursor(),inv.SavedRecovery(),inv.SavedEquipment());
  inv.slots[0].count=1;Check(snapshot.inventory[0].count==64,"save snapshots do not alias live stacks");
  inv.LoadInventory(snapshot.inventory,snapshot.crafting,snapshot.cursor,snapshot.recovery,snapshot.equipment);Check(Total(inv,ItemType.Stone)==71*64&&Total(inv,ItemType.Sand)==1,"cursor and legacy overflow survive actual save-message construction and load");
  inv.OpenInventory();inv.canvasRect.sizeDelta=new Vector2(320,200);inv.UpdateInventoryLayout();
  Check(inv.creativePageSize<131&&inv.nextPage.interactable,"small windows page catalogue while both panes remain visible");
  var seen=new HashSet<ItemType>();int pages=(inv.creativeBlocks.Count+inv.creativePageSize-1)/inv.creativePageSize;
  for(int p=0;p<pages;p++){for(int i=0;i<inv.creativePageSize;i++){int n=p*inv.creativePageSize+i;if(n<inv.creativeBlocks.Count)seen.Add(inv.creativeBlocks[n]);}inv.ChangeCreativePage(1);}
  Check(seen.Count==inv.creativeBlocks.Count&&!inv.nextPage.interactable,"all blocks remain reachable on smaller windows");
  var meshes=inv.iconMeshes.Values.ToArray();var skin=inv.inventoryPreviewSkin;var textMaterial=inv.inventoryTextMaterial;var labelMaterial=inv.inventoryLabelMaterial;var sharedFont=inv.creativeFont.material;inv.OnDestroy();
  Check(textMaterial.destroyed&&labelMaterial.destroyed&&!sharedFont.destroyed,"owned text materials are released without changing shared font resources");
  Check(meshes.All(m=>m.destroyed)&&inv.inventoryPreviewData==null&&!skin.destroyed,"owned meshes released and shared preview skin preserved");
  Console.WriteLine(checks+" inventory arithmetic/layout/crafting/migration/lifecycle checks passed (Unity UI/transport mocked).");
 }
}
