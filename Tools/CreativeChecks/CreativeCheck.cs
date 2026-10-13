using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public class Object { public static void Destroy(Object o) { var go=o as GameObject;if(go!=null)go.transform.parent.children.Remove(go.transform); } }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component {}
 public class ScriptableObject:Object {}
 public class Texture2D:Object {} public class Material:Object {}
 public class Tooltip:Attribute {public Tooltip(string s){}}
 public struct Color {public Color(float r,float g,float b,float a){}public static Color white=>new Color();}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public class Transform:Component {public Transform parent;public Vector3 localPosition;public List<Transform> children=new List<Transform>();public int childCount=>children.Count;public Transform GetChild(int i)=>children[i];public void SetParent(Transform p,bool b){parent=p;p.children.Add(this);}}
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
 public class GameObject:Object {public string name;public int layer;public bool activeSelf=true;public RectTransform transform;List<Component> components=new List<Component>();public GameObject(string name,params Type[] t){this.name=name;transform=new RectTransform{gameObject=this};}public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T GetComponent<T>()where T:Component{return (T)components.Find(c=>c is T);}public void SetActive(bool b){activeSelf=b;}}
 public static class Resources {public static T Load<T>(string name)where T:Object{return null;}}
}
namespace UnityEngine.Events {public delegate void UnityAction();}
namespace UnityEngine.UI
{
 public class Image:Component {public Color color;}
 public class Button:Component {public bool interactable=true;public Click onClick=new Click();public class Click {UnityEngine.Events.UnityAction action;public void AddListener(UnityEngine.Events.UnityAction a){action+=a;}public void Invoke(){action();}}}
}
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object{}public enum TextAlignmentOptions{Center}public class TextMeshProUGUI:Component {public TMP_FontAsset font;public float fontSize;public string text;public Color color;public TextAlignmentOptions alignment;public bool raycastTarget;}}
public static class BlockDataManager {public static object[] blockTypeDataDictionary=new object[133];}
public partial class PlayerInventory:MonoBehaviour
{
 private RectTransform inventoryRect,inventoryHighlightRect;
 private GameObject SlotTextPrefab;
 private const int maxStackSize=64;
 public int selectedSlotIndex;public Slot pickedUpSlot;public Slot[] slots=new Slot[72];
 public class Slot {public BlockType type;public int count;public void SetCount(int n){count=n;}}
 void SetSlotBlockType(int i,BlockType t){slots[i].type=t;}
 void CreateBlockMesh(BlockType t,Transform parent){new GameObject("Icon").transform.SetParent(parent,false);}
 static int checks;
 static void Check(bool b,string label){checks++;if(!b)throw new Exception(label);}
 public static void Main()
 {
  for(int i=0;i<133;i++)BlockDataManager.blockTypeDataDictionary[i]=new object();
  var inv=new PlayerInventory();var parent=new GameObject("GameUI");parent.layer=5;
  inv.inventoryRect=new GameObject("Inventory").transform;inv.inventoryRect.SetParent(parent.transform,false);
  inv.inventoryHighlightRect=new GameObject("Highlight").transform;
  inv.SlotTextPrefab=new GameObject("Text");inv.SlotTextPrefab.AddComponent<TMPro.TextMeshProUGUI>();
  for(int i=0;i<72;i++)inv.slots[i]=new Slot();
  inv.BuildCreativeInventory();Check(inv.creativeBlocks.Count==131,"all non-air blocks available");
  Check(!inv.creativeRoot.gameObject.activeSelf,"closed initially");
  Check(inv.creativeRoot.gameObject.layer==5,"inherits UI camera layer");
  var seen=new HashSet<BlockType>();
  for(int page=0;page<3;page++)
  {
   for(int cell=0;cell<45;cell++)
   {
    int index=page*45+cell;Check(inv.creativeCells[cell].gameObject.activeSelf==(index<131),"last-page unused cells hidden");
    if(index>=131)continue;
    inv.selectedSlotIndex=index%9;inv.creativeCells[cell].gameObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    Check(inv.slots[inv.selectedSlotIndex].type==inv.creativeBlocks[index]&&inv.slots[inv.selectedSlotIndex].count==64,"button grants correct stack to selected slot");
    Check(inv.creativeCells[cell].childCount==1,"one preview instance per cell");
    Check(inv.creativeCells[cell].GetChild(0).localPosition.z==-20,"preview in front of opaque panel");
    seen.Add(inv.slots[inv.selectedSlotIndex].type);
   }
   inv.ChangeCreativePage(1);
  }
  Check(seen.Count==131,"every catalog variant reachable");Check(inv.creativePage==2&&!inv.nextPage.interactable,"last page bounded");
  inv.ChangeCreativePage(-100);Check(inv.creativePage==0&&!inv.previousPage.interactable,"first page bounded");
  inv.ShowInventoryTab(false);Check(inv.inventoryRect.gameObject.activeSelf&&!inv.creativePanel.gameObject.activeSelf,"regular inventory accessible");
  inv.pickedUpSlot=new Slot();inv.ShowInventoryTab(true);Check(inv.inventoryRect.gameObject.activeSelf,"carried stack stays accessible");
  var old=inv.slots[0].type;inv.SelectCreativeBlock(0);Check(inv.slots[0].type==old,"no replacement while carrying");
  inv.pickedUpSlot=null;inv.ShowInventoryTab(true);Check(!inv.inventoryRect.gameObject.activeSelf&&inv.creativePanel.gameObject.activeSelf,"catalog tab accessible");
  Check(inv.slots.Length==72,"save-slot count unchanged");
  Console.WriteLine(checks+" creative catalog paging/button/stack/layout checks passed (Unity UI mocked).");
 }
}
