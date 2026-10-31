using System;
using UnityEditor;
using UnityEngine;

public static class EntityDataEditor
{
    public const string AssetPath="Assets/Resources/Entity Data.asset";
    [MenuItem("Minecraft/Entities/Select Entity Data")]
    public static void SelectCatalogue()
    {
        var data=AssetDatabase.LoadAssetAtPath<EntityDataSO>(AssetPath);
        if(data==null)
        {
            data=ScriptableObject.CreateInstance<EntityDataSO>();data.AddMissingMobs();
            AssetDatabase.CreateAsset(data,AssetPath);AssetDatabase.SaveAssets();
        }
        Selection.activeObject=data;EditorGUIUtility.PingObject(data);
    }
}

[CustomEditor(typeof(EntityDataSO))]
public sealed class EntityDataInspector : Editor
{
    private string search="";
    private int tab;
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Entity catalogue",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Gameplay settings apply next Play Mode session/build launch. Existing skins/materials are edited in place, preserving their links. Zero spawn weight disables natural spawning only. Movement speed is blocks per simulation tick (20 Hz).",MessageType.Info);
        if(AssetDatabase.GetAssetPath(target)!=EntityDataEditor.AssetPath)
            EditorGUILayout.HelpBox("Only "+EntityDataEditor.AssetPath+" is loaded automatically. Use Minecraft → Entities → Select Entity Data.",MessageType.Warning);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
        serializedObject.Update();
        tab=GUILayout.Toolbar(tab,new[]{"Mobs","Vehicles","Chests"});
        if(tab==0)DrawMobs();else if(tab==1)DrawVehicles();else DrawChests();
        serializedObject.ApplyModifiedProperties();
        EditorGUI.EndDisabledGroup();
    }
    private void DrawMobs()
    {
        search=EditorGUILayout.TextField("Search",search);
        if(GUILayout.Button("Add missing mob entries (preserve existing)"))
        {
            serializedObject.ApplyModifiedProperties();Undo.RecordObject(target,"Add missing entity entries");
            ((EntityDataSO)target).AddMissingMobs();EditorUtility.SetDirty(target);serializedObject.Update();
        }
        var list=serializedObject.FindProperty("mobs");var seen=new System.Collections.Generic.HashSet<int>();
        for(int i=0;i<list.arraySize;i++)
        {
            var entry=list.GetArrayElementAtIndex(i);int id=entry.FindPropertyRelative("species").intValue;
            string name=((MobType)id).ToString();
            if(!seen.Add(id))EditorGUILayout.HelpBox("Duplicate "+name+": runtime uses the first entry.",MessageType.Warning);
            if(name.IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)continue;
            entry.isExpanded=EditorGUILayout.Foldout(entry.isExpanded,name,true);
            if(!entry.isExpanded)continue;
            EditorGUI.indentLevel++;
            foreach(string field in new[]{"maxHealth","walkSpeed","spawnWeight"})EditorGUILayout.PropertyField(entry.FindPropertyRelative(field));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxGroup"),new GUIContent("Spawn pack cap"));
            if(MobDefinitions.Valid((MobType)id))DrawMaterial(MobDefinitions.Default((MobType)id).MaterialResource);
            if((MobType)id==MobType.Sheep)DrawMaterial("SheepFurMaterial");
            if((MobType)id==MobType.Skeleton)DrawMaterial("SkeletonBowMaterial");
            EditorGUI.indentLevel--;
        }
    }
    private void DrawVehicles()
    {
        var settings=serializedObject.FindProperty("vehicles");
        EditorGUILayout.LabelField("Boat",EditorStyles.boldLabel);
        foreach(string field in new[]{"boatMaxSpeed","boatThrust","boatTurnSpeed","boatBuoyancy","boatGravity","boatWaterDrag"})EditorGUILayout.PropertyField(settings.FindPropertyRelative(field));
        DrawHud("boatSkin");
        EditorGUILayout.LabelField("Minecart",EditorStyles.boldLabel);
        foreach(string field in new[]{"cartMaxSpeed","cartSlopeAcceleration","cartPoweredAcceleration"})EditorGUILayout.PropertyField(settings.FindPropertyRelative(field));
        DrawHud("minecartSkin");
        EditorGUILayout.HelpBox("Vehicle speeds use blocks/second. Water drag is the retained horizontal speed per 20 Hz tick (0–1). Collision dimensions, rail rules and model UV layout remain fixed.",MessageType.Info);
    }
    private void DrawChests()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("chestLidSpeed"));
        DrawHud("chestSkin","chestLeftSkin","chestRightSkin","chestContainer");
        DrawMaterial("ModernChestMaterial",false);
        EditorGUILayout.HelpBox("Single and double chests share storage/interaction rules. This changes animation speed and existing visual assets, not slot counts, save format or the pairing rules.",MessageType.Info);
    }
    private static void DrawHud(params string[] fields)
    {
        var hud=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if(hud==null){EditorGUILayout.HelpBox("PlayerHudAssets is missing; skin references cannot be edited.",MessageType.Warning);return;}
        var so=new SerializedObject(hud);so.Update();
        foreach(string field in fields)EditorGUILayout.PropertyField(so.FindProperty(field));
        so.ApplyModifiedProperties();
    }
    private static void DrawMaterial(string resource,bool editTexture=true)
    {
        var material=Resources.Load<Material>(resource.Contains("/")?resource:"Materials/"+resource);
        EditorGUI.BeginDisabledGroup(true);EditorGUILayout.ObjectField(resource,material,typeof(Material),false);EditorGUI.EndDisabledGroup();
        if(material==null){EditorGUILayout.HelpBox("Missing Resources material: "+resource,MessageType.Warning);return;}
        if(GUILayout.Button("Open "+resource))Selection.activeObject=material;
        if(!editTexture)return;
        EditorGUI.BeginChangeCheck();
        var image=(Texture2D)EditorGUILayout.ObjectField("Skin",material.mainTexture as Texture2D,typeof(Texture2D),false);
        if(!EditorGUI.EndChangeCheck())return;
        Undo.RecordObject(material,"Change entity skin");material.mainTexture=image;EditorUtility.SetDirty(material);
    }
}
