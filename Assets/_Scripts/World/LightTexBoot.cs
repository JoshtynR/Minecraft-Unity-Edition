using UnityEngine;

// Compatibility with legacy LUT shaders. The Beta Voxel shader evaluates
// brightness directly and deliberately has no _LightTex property.
[DefaultExecutionOrder(-1000)]
public class LightTexBoot : MonoBehaviour
{
    public Material Main_Mat;
    private static readonly int LightTexID = Shader.PropertyToID("_LightTex");
    private Texture2D ownedLut;
    private Texture previousTexture;

    private void Awake()
    {
        if (Main_Mat == null || !Main_Mat.HasProperty(LightTexID)) return;
        previousTexture = Main_Mat.GetTexture(LightTexID);
        LightTextureCreator.CreateLightTexture();
        ownedLut = LightTextureCreator.BuildLightTex2D();
        ownedLut.wrapMode = TextureWrapMode.Clamp;
        ownedLut.filterMode = FilterMode.Bilinear;
        Main_Mat.SetTexture(LightTexID, ownedLut);
    }

    private void OnDestroy()
    {
        if (ownedLut == null) return;
        if (Main_Mat != null && Main_Mat.GetTexture(LightTexID) == ownedLut)
            Main_Mat.SetTexture(LightTexID, previousTexture);
        Destroy(ownedLut);
        ownedLut = null;
    }
}
