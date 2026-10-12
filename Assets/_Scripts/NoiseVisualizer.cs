using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class NoiseVisualizer : MonoBehaviour
{
    public NoiseSettings settings;
    
    public int resolution = 256;
    
    public Renderer _renderer;
    private Texture2D generatedTexture;

    private void OnDestroy()
    {
        if (generatedTexture != null)
        {
            if (Application.isPlaying) Destroy(generatedTexture);
            else DestroyImmediate(generatedTexture);
        }
    }

    public void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void OnValidate()
    {
        // UpdateTexture();
    }

    public void UpdateTexture()
    {
        Color[] pixs = new Color[resolution * resolution];
        for (var x = 0; x < resolution; x++)
        {
            for (var y = 0; y < resolution; y++)
            {
                var value = MyNoise.OctavePerlin(x, y, settings);
                if (value > 0.9f)
                {
                    var g = 0;
                }
                var color = new Color(value, value, value);
                pixs[y * resolution + x] = color;
            }
        }
        
        if (generatedTexture != null)
        {
            if (Application.isPlaying) Destroy(generatedTexture);
            else DestroyImmediate(generatedTexture);
        }
        var texture = new Texture2D(resolution, resolution);
        generatedTexture = texture;
        texture.SetPixels(pixs);
        texture.Apply();
        
        _renderer.sharedMaterial.mainTexture = texture;
    }
    
#if UNITY_EDITOR

    [CustomEditor(typeof(NoiseVisualizer))]
    public class NoiseVisualizerEditor : Editor
    {
        private Editor settingsEditor;
        private void OnDisable()
        {
            if (settingsEditor != null) DestroyImmediate(settingsEditor);
        }
        public override void OnInspectorGUI()
        {
            var biomeGenerator = (NoiseVisualizer)this.target;
            DrawDefaultInspector();
            if (GUILayout.Button("Update Texture"))
            {
                biomeGenerator.UpdateTexture();
            }
            if (biomeGenerator.settings != null)
            {
                Editor.CreateCachedEditor(biomeGenerator.settings, null, ref settingsEditor);
                settingsEditor.OnInspectorGUI();
            }
        }
    }
    
#endif
}
