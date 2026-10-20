using TMPro;
using UnityEngine;

// The editor binds the user's existing icons.png here. Resources keeps that
// reference available in builds without runtime AssetDatabase/file access.
public class PlayerHudAssets : ScriptableObject
{
    public Texture2D icons;
    [Tooltip("256x256 inventory sheet; the upper-left 176x166 region is the survival panel.")]
    public Texture2D inventory;
    public Texture2D craftingTable;
    [Tooltip("Player skin used directly by the inventory preview. A skin material is optional.")]
    public Texture2D inventoryPlayerSkin;
    public Texture2D chestSkin;
    public Texture2D chestContainer;
    public Texture2D waterOverlay,fireOverlay;
    public Texture2D smokeParticle,splashParticle,lavaParticle,bubbleParticle;
    public AudioClip fluidFizz,fluidSplash,fluidFlow;
    public TMP_FontAsset levelFont;
    public Font guiFont;
    public Texture2D guiWidgets;
    public Texture2D blockBreak;
    public Material blockBreakMaterial;
    [Tooltip("Zero detects horizontal/vertical ten-frame strips or a 5x2/2x5 grid.")]
    public int breakColumns, breakRows;
}
