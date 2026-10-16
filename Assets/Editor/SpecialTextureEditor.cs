using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Both block editors operate on the original serialized slots, preserving image
// references, undo/redo, and properties hidden by the search filter.
public static class SpecialTextureEditor
{
    public static void Draw(SerializedProperty texture,BlockType type)
    {
        var parts=SpecialTextureLayout.Parts(type);
        if(parts.Length==0)return;
        var entries=texture.FindPropertyRelative("specialTextures");
        EditorGUILayout.LabelField("Special model images",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Optional images for this model. Empty slots retain your existing face images. Animation settings above also apply to these sheets.",MessageType.Info);
        foreach(var part in parts)
        {
            SerializedProperty entry=null;
            for(int i=0;i<entries.arraySize;i++)
            {
                var candidate=entries.GetArrayElementAtIndex(i);
                if(candidate.FindPropertyRelative("part").intValue==(int)part){entry=candidate;break;}
            }
            var reference=entry==null?null:entry.FindPropertyRelative("image");
            var oldImage=reference==null?null:reference.objectReferenceValue;
            string label=Regex.Replace(part.ToString(),"([a-z0-9])([A-Z])","$1 $2");
            EditorGUI.BeginChangeCheck();
            var image=EditorGUILayout.ObjectField(label,oldImage,typeof(Texture2D),false);
            if(!EditorGUI.EndChangeCheck())continue;
            if(entry==null)
            {
                int index=entries.arraySize;entries.arraySize++;
                entry=entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("part").intValue=(int)part;
                reference=entry.FindPropertyRelative("image");
            }
            reference.objectReferenceValue=image;
        }
    }
}
