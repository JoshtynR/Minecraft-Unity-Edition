using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerHudAssetSetup
{
    public const string BreakPath = "Assets/Textures/Minecraft/block_break.png";
    public const string IconsPath = "Assets/Textures/Minecraft/gui/icons.png";
    static PlayerHudAssetSetup() { EditorApplication.delayCall += Bind; }

    public static void Bind()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Bind;
            return;
        }
        var config = AssetDatabase.LoadAssetAtPath<PlayerHudAssets>("Assets/Resources/PlayerHudAssets.asset");
        if (config == null) return;
        var icons = Prepare(IconsPath);
        var cracks = Prepare(BreakPath);
        if ((icons == null || config.icons == icons) && (cracks == null || config.blockBreak == cracks)) return;
        if (icons != null) config.icons = icons;
        if (cracks != null) config.blockBreak = cracks;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssetIfDirty(config);
    }

    private static Texture2D Prepare(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return null;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed || importer.npotScale != TextureImporterNPOTScale.None))
        {
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return texture;
    }
}

public class PlayerHudIconImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath != PlayerHudAssetSetup.IconsPath && assetPath != PlayerHudAssetSetup.BreakPath) return;
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
            if (path == PlayerHudAssetSetup.IconsPath || path == PlayerHudAssetSetup.BreakPath) { EditorApplication.delayCall += PlayerHudAssetSetup.Bind; break; }
    }
}
