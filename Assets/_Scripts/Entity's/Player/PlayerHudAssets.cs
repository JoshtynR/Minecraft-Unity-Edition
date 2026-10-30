using TMPro;
using UnityEngine;

// The editor binds the user's existing icons.png here. Resources keeps that
// reference available in builds without runtime AssetDatabase/file access.
public class PlayerHudAssets : ScriptableObject
{
    [Tooltip("Fallback world-list row for scenes whose menu references were lost.")]
    public GameObject worldListEntryPrefab;
    public Texture2D icons;
    [Tooltip("256x256 inventory sheet; the upper-left 176x166 region is the survival panel.")]
    public Texture2D inventory;
    public Texture2D craftingTable;
    public Texture2D furnace;
    [Tooltip("Player skin used directly by the inventory preview. A skin material is optional.")]
    public Texture2D inventoryPlayerSkin;
    [Tooltip("Armour textures in pairs: leather, chainmail, iron, diamond, gold; layer 1 then layer 2.")]
    public Texture2D[] armourLayers=new Texture2D[10];
    [Tooltip("Optional modern leather detail overlays: layer 1 then layer 2.")]
    public Texture2D[] leatherArmourOverlays=new Texture2D[2];
    public Texture2D chestSkin;
    public Texture2D chestContainer;
    public Texture2D waterOverlay,fireOverlay;
    [Tooltip("Packed 256x256 Minecraft particle atlas. Runtime effects animate regions of this sheet directly.")]
    public Texture2D particleAtlas;
    [Tooltip("Legacy per-particle fallbacks. The packed atlas is preferred when assigned.")]
    public Texture2D smokeParticle,splashParticle,lavaParticle,bubbleParticle;
    public AudioClip fluidFizz,fluidSplash,fluidFlow;
    public AudioClip itemPickup;
    public TMP_FontAsset levelFont;
    public Font guiFont;
    public Texture2D guiWidgets;
    public Texture2D loadingBackground;
    public Texture2D sunTexture,moonTexture,cloudTexture;
    public Texture2D blockBreak;
    public Material blockBreakMaterial;
    [Tooltip("Zero detects horizontal/vertical ten-frame strips or a 5x2/2x5 grid.")]
    public int breakColumns, breakRows;
}
