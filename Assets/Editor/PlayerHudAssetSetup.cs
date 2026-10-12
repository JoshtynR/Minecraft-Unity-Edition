using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerHudAssetSetup
{
    public const string IconsPath = "Assets/Textures/Minecraft/gui/icons.png";
    static PlayerHudAssetSetup() { EditorApplication.delayCall += Bind; }

    public static void Bind()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Bind;
            return;
        }
        var icons = AssetDatabase.LoadAssetAtPath<Texture2D>(IconsPath);
        var config = AssetDatabase.LoadAssetAtPath<PlayerHudAssets>("Assets/Resources/PlayerHudAssets.asset");
        if (icons == null || config == null) return;
        var importer = AssetImporter.GetAtPath(IconsPath) as TextureImporter;
        if (importer != null && (importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed || importer.npotScale != TextureImporterNPOTScale.None))
        {
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            icons = AssetDatabase.LoadAssetAtPath<Texture2D>(IconsPath);
        }
        if (config.icons == icons) return;
        config.icons = icons;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssetIfDirty(config);
    }
}

public class PlayerHudIconImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath != PlayerHudAssetSetup.IconsPath) return;
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
            if (path == PlayerHudAssetSetup.IconsPath) { EditorApplication.delayCall += PlayerHudAssetSetup.Bind; break; }
    }
}
