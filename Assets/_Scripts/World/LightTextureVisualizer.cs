using UnityEngine;

public class LightTextureVisualizer : MonoBehaviour
{
    public Texture2D lightTexture;
    private Texture2D ownedLightTexture;

    private void OnDestroy()
    {
        if (ownedLightTexture != null)
        {
            if (Application.isPlaying) Destroy(ownedLightTexture);
            else DestroyImmediate(ownedLightTexture);
        }
    }
    [Range(0, 1)] public float gamma;
    [Range(0.15f, 1)]
    public float skyLightMultiplier = 0.75f;
    public float blockLightMultiplier = 1.5f;

    private void OnValidate()
    {
        if (lightTexture != null)
        {
            LightTextureCreator.gamma = gamma;
            LightTextureCreator.skyLightMultiplier = skyLightMultiplier;
            LightTextureCreator.blockLightMultiplier = blockLightMultiplier;
            LightTextureCreator.CreateLightTexture();
            var pixels = lightTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = LightTextureCreator.lightColors[i];
            }
            lightTexture.SetPixels(pixels);
            lightTexture.Apply();
        }
        else
        {
            if (ownedLightTexture != null)
            {
                if (Application.isPlaying) Destroy(ownedLightTexture);
                else DestroyImmediate(ownedLightTexture);
            }
            lightTexture = new Texture2D(16, 16);
            ownedLightTexture = lightTexture;
            lightTexture.filterMode = FilterMode.Point;
            lightTexture.wrapMode = TextureWrapMode.Clamp;
        }
        GetComponent<Renderer>().sharedMaterial.mainTexture = lightTexture;
    }
}