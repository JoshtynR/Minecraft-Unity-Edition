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
