using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Import scaling must never change a 16x320 strip into 16x256. Frames are
// extracted from the imported image, so preserve the complete source dimensions.
[InitializeOnLoad]
public class AnimatedBlockTextureImports : AssetPostprocessor
{
    static AnimatedBlockTextureImports()
    { EditorApplication.delayCall+=RepairAssignedSheets; }

    private void OnPreprocessTexture()
    {
        string name=Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        if(name=="lava_still"||name=="lava_flow"||name=="water_still"||name=="water_flow"||
            name=="fire_0"||name=="fire_1"||name=="fire_layer_0"||name=="fire_layer_1")
            Prepare((TextureImporter)assetImporter);
    }

    [MenuItem("Minecraft/Repair Animated Texture Imports")]
    public static void RepairAssignedSheets()
    {
        var paths=new HashSet<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:BlockDataSO"))
        {
            var data=AssetDatabase.LoadAssetAtPath<BlockDataSO>(AssetDatabase.GUIDToAssetPath(guid));
            if(data==null||data.textureDataList==null)continue;
            foreach(var block in data.textureDataList)
            {
                if(block==null||block.textureData==null)continue;
                var texture=block.textureData;
                if(texture.animateFaces)
                    foreach(var image in new[]{texture.top,texture.bottom,texture.side,texture.front,texture.back,texture.left,texture.right})
                        Add(paths,image);
                if(texture.specialTextures!=null)foreach(var part in texture.specialTextures)
                    if(part!=null&&(part.overrideAnimation?part.animate:texture.animateFaces))Add(paths,part.image);
            }
        }
        foreach(string path in paths)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null&&Prepare(importer))importer.SaveAndReimport();
        }
    }
    private static void Add(HashSet<string> paths,Texture2D image)
    { if(image!=null){string path=AssetDatabase.GetAssetPath(image);if(!string.IsNullOrEmpty(path))paths.Add(path);} }

    private static bool Prepare(TextureImporter importer)
    {
        int width,height;importer.GetSourceTextureWidthAndHeight(out width,out height);
        int required=32;while(required<width||required<height)required*=2;
        required=System.Math.Min(16384,required);
        bool changed=importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||
            importer.textureCompression!=TextureImporterCompression.Uncompressed||
            importer.filterMode!=FilterMode.Point||importer.maxTextureSize<required;
        if(!changed)return false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=System.Math.Max(importer.maxTextureSize,required);
        return true;
    }
}
