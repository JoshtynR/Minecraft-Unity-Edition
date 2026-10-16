using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockDataSO))]
public class BlockDataInspector : Editor
{
    private string search = "";

    [MenuItem("Minecraft/Select Block Data")]
    public static void SelectBlockData()
    {
        var data = AssetDatabase.LoadAssetAtPath<BlockDataSO>("Assets/Block Data.asset");
        if (data == null) return;
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "textureDataList");
        var blocks = serializedObject.FindProperty("textureDataList");
        EditorGUILayout.LabelField("Block catalogue", EditorStyles.boldLabel);
        search = EditorGUILayout.TextField("Search", search);
        string query = search.Trim();
        int shown = 0;
        for (int i = 0; i < blocks.arraySize; i++)
        {
            // Draw the original serialized entry, never a copied or filtered list.
            // This preserves element indices, object references and Unity undo.
            var entry = blocks.GetArrayElementAtIndex(i);
            var type = (BlockType)entry.FindPropertyRelative("blockType").intValue;
            string name = type.ToString();
            string label = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
            if (query.Length > 0 &&
                name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                label.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
            shown++;
            EditorGUILayout.PropertyField(entry, new GUIContent(label), true);
        }
        EditorGUILayout.LabelField(shown + " / " + blocks.arraySize + " entries");
        serializedObject.ApplyModifiedProperties();
    }
}
