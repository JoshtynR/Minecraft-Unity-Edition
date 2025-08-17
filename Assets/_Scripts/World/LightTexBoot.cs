using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class LightTexBoot : MonoBehaviour
{
    public Material Main_Mat;  // Drag in the actual material asset from the Project view
    static readonly int LightTexID = Shader.PropertyToID("_LightTex");

    void Awake()
    {
        LightTextureCreator.CreateLightTexture();
        var lut = LightTextureCreator.BuildLightTex2D();
        lut.wrapMode   = TextureWrapMode.Clamp;
        lut.filterMode = FilterMode.Bilinear;
        lut.Apply(false, false);

        if (Main_Mat == null)
        {
            Debug.LogError("[LightTexBoot] No material assigned.");
            return;
        }

        if (!Main_Mat.HasProperty(LightTexID))
        {
            Debug.LogError($"[LightTexBoot] Assigned material '{Main_Mat.name}' does not have _LightTex property.");
            return;
        }

        Main_Mat.SetTexture(LightTexID, lut);
        Debug.Log($"[LightTexBoot] Successfully assigned light texture to material '{Main_Mat.name}'.");
    }
}
