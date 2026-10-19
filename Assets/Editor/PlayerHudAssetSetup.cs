using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerHudAssetSetup
{
    public const string BreakPath = "Assets/Textures/Minecraft/block_break.png";
    public const string IconsPath = "Assets/Textures/Minecraft/gui/icons.png";
    public const string InventoryPath = "Assets/Textures/Minecraft/gui/container/inventory.png";
    static PlayerHudAssetSetup() { EditorApplication.delayCall += Bind; }

    [MenuItem("Minecraft/Textures/Refresh GUI and Chest Assets")]
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
        var inventory = Prepare(InventoryPath);
        var guiWidgets=config.guiWidgets!=null?config.guiWidgets:Prepare("Assets/Textures/Minecraft/gui/widgets.png");
        if(guiWidgets==null)guiWidgets=Prepare("Assets/Resources/Sprites/UI/widgets.png");
        var guiFont=config.guiFont!=null?config.guiFont:AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/MinecraftRegular.otf");
        var playerSkin=config.inventoryPlayerSkin;
        var playerMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Player/player.mat");
        if(playerSkin==null&&playerMaterial!=null)playerSkin=playerMaterial.mainTexture as Texture2D;
        if(playerSkin==null)playerSkin=Prepare("Assets/Textures/Minecraft/entity/player/wide/steve.png");
        if(playerSkin==null)playerSkin=Prepare("Assets/Textures/Minecraft/entity/steve.png");
        if(playerSkin==null)playerSkin=Prepare("Assets/Textures/Player/steve.png");
        var chestSkin = Prepare("Assets/Textures/Minecraft/entity/chest/normal.png");
        if(chestSkin==null)chestSkin=Prepare("Assets/Textures/Minecraft/block/normal.png");
        var chestContainer=Prepare("Assets/Textures/Minecraft/gui/container/generic_54.png");
        var waterOverlay=config.waterOverlay!=null?config.waterOverlay:Prepare("Assets/Textures/Minecraft/misc/underwater.png");
        var fireOverlay=config.fireOverlay!=null?config.fireOverlay:Prepare("Assets/Textures/Minecraft/block/fire_0.png");
        if(fireOverlay==null)fireOverlay=Prepare("Assets/Textures/Minecraft/fire_0.png");
        var smokeParticle=config.smokeParticle!=null?config.smokeParticle:Prepare("Assets/Textures/Minecraft/particle/generic_0.png");
        var splashParticle=config.splashParticle!=null?config.splashParticle:Prepare("Assets/Textures/Minecraft/particle/splash_0.png");
        var lavaParticle=config.lavaParticle!=null?config.lavaParticle:Prepare("Assets/Textures/Minecraft/particle/lava.png");
        var bubbleParticle=config.bubbleParticle!=null?config.bubbleParticle:Prepare("Assets/Textures/Minecraft/particle/bubble.png");
        if ((icons == null || config.icons == icons) && (cracks == null || config.blockBreak == cracks) &&
            (inventory == null || config.inventory == inventory) &&
            (playerSkin == null || config.inventoryPlayerSkin == playerSkin) &&
            (chestSkin == null || config.chestSkin == chestSkin) &&
            (chestContainer == null || config.chestContainer == chestContainer) &&
            (waterOverlay == null || config.waterOverlay == waterOverlay) &&
            (fireOverlay == null || config.fireOverlay == fireOverlay)&&
            (smokeParticle == null || config.smokeParticle == smokeParticle)&&
            (splashParticle == null || config.splashParticle == splashParticle)&&
            (lavaParticle == null || config.lavaParticle == lavaParticle)&&
            (bubbleParticle == null || config.bubbleParticle == bubbleParticle)&&
            (guiWidgets == null || config.guiWidgets == guiWidgets)&&
            (guiFont == null || config.guiFont == guiFont)) return;
        if (icons != null) config.icons = icons;
        if (cracks != null) config.blockBreak = cracks;
        if (inventory != null) config.inventory = inventory;
        if(guiWidgets!=null)config.guiWidgets=guiWidgets;
        if(guiFont!=null)config.guiFont=guiFont;
        if (playerSkin != null) config.inventoryPlayerSkin = playerSkin;
        if (chestSkin != null) config.chestSkin = chestSkin;
        if (chestContainer != null) config.chestContainer = chestContainer;
        if (waterOverlay != null) config.waterOverlay=waterOverlay;
        if (fireOverlay != null) config.fireOverlay=fireOverlay;
        if (smokeParticle != null) config.smokeParticle=smokeParticle;
        if (splashParticle != null) config.splashParticle=splashParticle;
        if (lavaParticle != null) config.lavaParticle=lavaParticle;
        if (bubbleParticle != null) config.bubbleParticle=bubbleParticle;
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
        if (assetPath != PlayerHudAssetSetup.IconsPath && assetPath != PlayerHudAssetSetup.BreakPath &&
            assetPath != PlayerHudAssetSetup.InventoryPath) return;
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
            if (path == PlayerHudAssetSetup.IconsPath || path == PlayerHudAssetSetup.BreakPath ||
                path == PlayerHudAssetSetup.InventoryPath || path=="Assets/Textures/Minecraft/entity/chest/normal.png" ||
                path=="Assets/Textures/Minecraft/block/normal.png" || path=="Assets/Textures/Minecraft/gui/container/generic_54.png" ||
                path=="Assets/Textures/Player/player.mat" || path=="Assets/Textures/Player/steve.png" ||
                path=="Assets/Textures/Minecraft/entity/steve.png" || path=="Assets/Textures/Minecraft/entity/player/wide/steve.png")
            { EditorApplication.delayCall += PlayerHudAssetSetup.Bind; break; }
    }
}
