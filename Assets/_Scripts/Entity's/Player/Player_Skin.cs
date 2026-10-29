using System.Collections.Generic;
using UnityEngine;

public partial class Player
{
    private Material bodySkinMaterial;
    private readonly List<MeshRenderer> bodySkinRenderers=new List<MeshRenderer>(6);
    private MaterialPropertyBlock bodySkinProperties;

    private void InitializeBodySkin()
    {
        // Same priority as the inventory preview and first-person arm.
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        Texture skin=assets!=null?assets.inventoryPlayerSkin:null;
        if(skin==null)
        {
            var fallback=Resources.Load<Material>("InventoryPlayerMaterial");
            if(fallback!=null)skin=fallback.mainTexture;
        }
        if(skin==null)skin=Texture2D.whiteTexture;
        var template=Resources.Load<Material>("PlayerBodyMaterial");
        if(template==null){Debug.LogWarning("PlayerBodyMaterial is missing; keeping the existing body material.");return;}
        bodySkinMaterial=new Material(template){name="Player body skin (runtime)",mainTexture=skin};
        bodySkinProperties=new MaterialPropertyBlock();
        // Explicit body parts exclude the camera's held item, nameplate and UI.
        var parts=new[]{objects.head,objects.leftArm,objects.rightArm,objects.leftLeg,objects.rightLeg};
        foreach(var part in parts)AddBodySkinRenderers(part);
        // The torso is a sibling of the articulated parts under Model.
        if(objects.body!=null)
            foreach(Transform child in objects.body)
                if(child.name=="Torso")AddBodySkinRenderers(child);
    }

    private void AddBodySkinRenderers(Transform part)
    {
        if(part==null)return;
        foreach(var renderer in part.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(bodySkinRenderers.Contains(renderer))continue;
            renderer.sharedMaterial=bodySkinMaterial;
            bodySkinRenderers.Add(renderer);
        }
    }

    private void UpdateBodySkinLight(int sky,int block)
    {
        if(bodySkinProperties==null)return;
        foreach(var renderer in bodySkinRenderers)
        {
            if(renderer==null)continue;
            renderer.GetPropertyBlock(bodySkinProperties);
            bodySkinProperties.SetVector("_LightLevels",new Vector4(sky,block,1,0));
            renderer.SetPropertyBlock(bodySkinProperties);
        }
    }

    private void DisposeBodySkin()
    {
        bodySkinRenderers.Clear();bodySkinProperties=null;
        if(bodySkinMaterial!=null)Destroy(bodySkinMaterial);
        bodySkinMaterial=null;
    }
}
