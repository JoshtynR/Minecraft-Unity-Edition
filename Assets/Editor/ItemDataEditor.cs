using UnityEditor;
using UnityEngine;

public static class ItemDataEditor
{
    [MenuItem("Minecraft/Select Item Data")]
    public static void SelectCatalogue()
    {
        var data=AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/Resources/Item Data.asset");
        if(data==null)
        {
            data=ScriptableObject.CreateInstance<ItemDataSO>();
            foreach(var item in ItemCatalog.All)data.items.Add(ItemCatalog.Default(item.id));
            AssetDatabase.CreateAsset(data,"Assets/Resources/Item Data.asset");
            AssetDatabase.SaveAssets();
        }
        Selection.activeObject=data;
        EditorGUIUtility.PingObject(data);
    }
}

[CustomEditor(typeof(ItemDataSO))]
public class ItemDataInspector : Editor
{
    private string search="";
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var items=serializedObject.FindProperty("items");
        EditorGUILayout.LabelField("Item catalogue",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign individual images here. Block entries reuse their block mesh when the image is empty. Item actions are implemented separately. Restart Play Mode after edits.",MessageType.Info);
        search=EditorGUILayout.TextField("Search",search);
        int shown=0;
        for(int i=0;i<items.arraySize;i++)
        {
            var entry=items.GetArrayElementAtIndex(i);
            var id=(ItemType)entry.FindPropertyRelative("id").intValue;
            string label=entry.FindPropertyRelative("displayName").stringValue;
            string title=label+" ("+id+")";
            if(!string.IsNullOrEmpty(search)&&title.IndexOf(search,System.StringComparison.OrdinalIgnoreCase)<0)continue;
            shown++;
            EditorGUILayout.PropertyField(entry,new GUIContent(title),true);
        }
        EditorGUILayout.LabelField(shown+" / "+items.arraySize+" entries");
        serializedObject.ApplyModifiedProperties();
    }
}
