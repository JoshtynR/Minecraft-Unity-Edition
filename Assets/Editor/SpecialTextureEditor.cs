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
        EditorGUILayout.HelpBox("Empty slots retain existing face images. Vertical fluid strips detect their own frame counts. Automatic still-lava playback runs forwards and backwards. Use an individual sheet override for other layouts.",MessageType.Info);
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
                entry.FindPropertyRelative("overrideAnimation").boolValue=false;
                entry.FindPropertyRelative("animate").boolValue=true;
                entry.FindPropertyRelative("columns").intValue=1;
                entry.FindPropertyRelative("rows").intValue=0;
                entry.FindPropertyRelative("frameCount").intValue=0;
                entry.FindPropertyRelative("framesPerSecond").floatValue=20;
                entry.FindPropertyRelative("playback").intValue=0;
                reference=entry.FindPropertyRelative("image");
            }
            reference.objectReferenceValue=image;
        }
        foreach(var part in parts)
        {
            for(int i=0;i<entries.arraySize;i++)
            {
                var entry=entries.GetArrayElementAtIndex(i);
                if(entry.FindPropertyRelative("part").intValue!=(int)part||entry.FindPropertyRelative("image").objectReferenceValue==null)continue;
                EditorGUILayout.LabelField(part+" animation",EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("playback"),new GUIContent("Playback"));
                var own=entry.FindPropertyRelative("overrideAnimation");
                EditorGUILayout.PropertyField(own,new GUIContent("Override sheet settings"));
                if(!own.boolValue)break;
                foreach(var field in new[]{"animate","columns","rows","frameCount","framesPerSecond"})
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative(field));
                break;
            }
        }
    }
}
