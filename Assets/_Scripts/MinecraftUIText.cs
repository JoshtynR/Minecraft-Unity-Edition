using TMPro;
using UnityEngine;

// The caller owns the returned material. Never recolor or enable effects on
// the shared font asset, which is also used by scene labels and other screens.
public static class MinecraftUIText
{
    public static Material CreateMaterial(TMP_FontAsset font,float size,bool shadow)
    {
        if(font==null||font.material==null)return null;
        var material=new Material(font.material);
        material.DisableKeyword("UNDERLAY_ON");material.DisableKeyword("UNDERLAY_INNER");
        material.DisableKeyword("OUTLINE_ON");material.DisableKeyword("GLOW_ON");
        material.SetColor("_FaceColor",Color.white);
        material.SetFloat("_OutlineWidth",0);material.SetFloat("_FaceDilate",0);
        // This is the actual depth-test property used by the bundled TMP shader.
        material.SetFloat("unity_GUIZTestMode",8);
        if(shadow)
        {
            material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",Color.black);
            float gradient=Mathf.Max(1,material.GetFloat("_GradientScale"));
            float ratio=Mathf.Max(.01f,material.GetFloat("_ScaleRatioC"));
            float pixel=font.faceInfo.pointSize/(Mathf.Max(1,size)*gradient*ratio);
            material.SetFloat("_UnderlayOffsetX",pixel);material.SetFloat("_UnderlayOffsetY",-pixel);
            material.SetFloat("_UnderlayDilate",0);material.SetFloat("_UnderlaySoftness",0);
        }
        return material;
    }
}
