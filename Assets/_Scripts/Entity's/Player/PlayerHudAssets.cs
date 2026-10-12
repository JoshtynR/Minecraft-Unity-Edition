using TMPro;
using UnityEngine;

// The editor binds the user's existing icons.png here. Resources keeps that
// reference available in builds without runtime AssetDatabase/file access.
public class PlayerHudAssets : ScriptableObject
{
    public Texture2D icons;
    public TMP_FontAsset levelFont;
}
