using System;
using System.Collections.Generic;
using UnityEngine;
public enum MobType {Cow,Pig,Sheep,Chicken,Zombie,Skeleton,Creeper,Spider}
public class Player{}
public class ModernChestVisual {public static float LidSpeed=2;}
public class PlayerHudAssets:UnityEngine.Object{}
namespace UnityEngine
{
 public class Object{}
 public class Texture:Object{}
 public class Texture2D:Texture{}
 public class Material:Object {public Texture mainTexture;}
 public class ScriptableObject:Object {public static T CreateInstance<T>()where T:new()=>new T();}
 public class CreateAssetMenuAttribute:Attribute {public string fileName,menuName;}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public enum RuntimeInitializeLoadType {BeforeSceneLoad}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public static class Resources {public static T Load<T>(string p)where T:class=>null;}
 public class GUIContent {public GUIContent(string s){}}
 public static class GUILayout {public static int Toolbar(int n,string[] s)=>n;public static bool Button(string s)=>false;}
 public static class Mathf
 {
  public const float Deg2Rad=(float)(Math.PI/180),Rad2Deg=(float)(180/Math.PI);
  public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
  public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float Clamp01(float v)=>Clamp(v,0,1);
  public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sin(float f)=>(float)Math.Sin(f);public static float Cos(float f)=>(float)Math.Cos(f);
 }
}
namespace UnityEditor
{
 public class Editor {public UnityEngine.Object target;public SerializedObject serializedObject=new SerializedObject(null);public virtual void OnInspectorGUI(){}}
 public class MenuItem:Attribute {public MenuItem(string s){}}
 public class CustomEditor:Attribute {public CustomEditor(Type t){}}
 public enum MessageType {Info,Warning}
 public static class EditorStyles {public static object boldLabel;}
 public static class EditorApplication {public static bool isPlayingOrWillChangePlaymode;}
 public static class EditorGUI {public static int indentLevel;public static void BeginDisabledGroup(bool b){}public static void EndDisabledGroup(){}public static void BeginChangeCheck(){}public static bool EndChangeCheck()=>false;}
 public static class EditorGUILayout
 {
  public static void LabelField(string s,object style){}public static void HelpBox(string s,MessageType t){}public static string TextField(string label,string text)=>text;
  public static bool Foldout(bool b,string s,bool toggle)=>b;public static void PropertyField(SerializedProperty p,params object[] args){}
  public static UnityEngine.Object ObjectField(string label,UnityEngine.Object obj,Type t,bool scene)=>obj;
 }
 public static class AssetDatabase {public static T LoadAssetAtPath<T>(string p)where T:class=>null;public static void CreateAsset(UnityEngine.Object o,string p){}public static void SaveAssets(){}public static string GetAssetPath(UnityEngine.Object o)=>EntityDataEditor.AssetPath;}
 public static class Undo {public static void RecordObject(UnityEngine.Object o,string s){}}
 public static class Selection {public static UnityEngine.Object activeObject;}
 public static class EditorUtility {public static void SetDirty(UnityEngine.Object o){}}
 public static class EditorGUIUtility {public static void PingObject(UnityEngine.Object o){}}
 public class SerializedObject {public SerializedObject(UnityEngine.Object o){}public void Update(){}public void ApplyModifiedProperties(){}public SerializedProperty FindProperty(string s)=>new SerializedProperty();}
 public class SerializedProperty {public int arraySize,intValue;public bool isExpanded;public SerializedProperty GetArrayElementAtIndex(int i)=>new SerializedProperty();public SerializedProperty FindPropertyRelative(string s)=>new SerializedProperty();}
}
public static class Check
{
 static int checks;static void Assert(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 public static void Main()
 {
  var data=new EntityDataSO();data.AddMissingMobs();Assert(data.mobs.Count==8,"all species present");data.mobs[0].maxHealth=37;data.AddMissingMobs();Assert(data.mobs.Count==8&&data.mobs[0].maxHealth==37,"populate preserves existing settings");
  data.vehicles.boatMaxSpeed=2;data.vehicles.boatTurnSpeed=120;data.chestLidSpeed=4;EntityDataSO.Apply(data);
  Assert(MobDefinitions.Get(MobType.Cow).MaxHealth==37,"runtime asset applies health");Assert(VehicleTuning.Current.boatMaxSpeed==2&&ModernChestVisual.LidSpeed==4,"runtime vehicle and chest values");
  var boat=new VehicleState{occupied=true,throttle=1,inputYaw=90};for(int i=0;i<1000;i++){VehiclePhysics.BoatVelocity(boat,.4f,.05f);VehiclePhysics.BoatDrag(boat);}
  VehiclePhysics.BoatHeading(boat,new Vector3(.4f,0,0));
  Assert(Math.Abs(boat.velocity.x)<=2.001f&&Math.Abs(boat.yaw-6)<.001f,"configured per-axis speed cap and travel-following turn limit affect physics");
  data.vehicles.boatMaxSpeed=float.NaN;data.vehicles.cartMaxSpeed=500;data.chestLidSpeed=float.PositiveInfinity;EntityDataSO.Apply(data);
  Assert(VehicleTuning.Current.boatMaxSpeed==8&&VehicleTuning.Current.cartMaxSpeed==20&&ModernChestVisual.LidSpeed==2,"nonfinite values and bounds safe");Assert(float.IsNaN(data.vehicles.boatMaxSpeed),"asset not mutated by validation");
  EntityDataSO.Apply(null);Assert(MobDefinitions.Get(MobType.Cow).MaxHealth==10&&VehicleTuning.Current.cartMaxSpeed==8&&ModernChestVisual.LidSpeed==2,"missing asset resets all defaults");
  var first=MobTuning.From(MobDefinitions.Default(MobType.Cow));first.maxHealth=33;var duplicate=MobTuning.From(MobDefinitions.Default(MobType.Cow));duplicate.maxHealth=90;
  MobDefinitions.Configure(new[]{first,duplicate});Assert(MobDefinitions.Get(MobType.Cow).MaxHealth==33,"first duplicate wins");
  var zeros=new List<MobTuning>();foreach(MobType kind in Enum.GetValues(typeof(MobType))){var t=MobTuning.From(MobDefinitions.Default(kind));t.spawnWeight=0;zeros.Add(t);}MobDefinitions.Configure(zeros);
  Assert(MobDefinitions.TotalSpawnWeight(true)==0&&MobDefinitions.TotalSpawnWeight(false)==0,"all disabled spawn categories safe");MobDefinitions.SelectSpawn(true,0);EntityDataSO.Apply(null);
  EntityDataEditor.SelectCatalogue();Assert(UnityEditor.Selection.activeObject is EntityDataSO,"editor menu creates/selects catalogue");
  Console.WriteLine(checks+" entity configuration/defaults/validation/physics/menu checks passed (Unity/editor mocked).");
 }
}
