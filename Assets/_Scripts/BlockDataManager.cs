using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-1000)]
public class BlockDataManager : MonoBehaviour
{
    public static BlockTypeData[] blockTypeDataDictionary = BlockDataDefaults.CreateLookup();
    public BlockDataSO textureData;
    private Texture2DArray faceTextures;
    private static BlockDataManager activeManager;

    private void OnEnable()
    {
        var images = InitializeData();
        if (!Application.isPlaying) return;
        activeManager = this;
#if !UNITY_SERVER
        if (!SystemInfo.supports2DArrayTextures)
        {
            Debug.LogError("Block rendering requires texture array support.", this);
            return;
        }
        BuildFaceTextures(images);
#endif
    }

    private List<Texture2D> InitializeData()
    {
        var entries = textureData != null ? textureData.textureDataList : null;
        int maxType = 0;
        if (entries != null)
            foreach (var item in entries)
                if (item != null) maxType = Mathf.Max(maxType, (int)item.blockType);
        var definitions = BlockDataDefaults.CreateLookup(maxType + 1);
        var assigned = new bool[definitions.Length];
        var images = new List<Texture2D>();
        var indices = new Dictionary<Texture2D, int>();
        if (entries != null)
            foreach (var item in entries)
            {
                if (item == null || (int)item.blockType < 0) continue;
                int id = (int)item.blockType;
                definitions[id] = item;
                assigned[id] = true;
                if (item.textureData == null) item.textureData = new TextureData();
                item.textureData.topIndex = Register(item.textureData.top, images, indices);
                item.textureData.bottomIndex = Register(item.textureData.bottom, images, indices);
                item.textureData.sideIndex = Register(item.textureData.side, images, indices);
                item.textureData.frontIndex = Register(item.textureData.front, images, indices);
                item.textureData.backIndex = Register(item.textureData.back, images, indices);
                item.textureData.leftIndex = Register(item.textureData.left, images, indices);
                item.textureData.rightIndex = Register(item.textureData.right, images, indices);
            }
        // Publish a complete map, never a partially filled array, to chunk workers.
        blockTypeDataDictionary = definitions;
        if (Application.isPlaying)
        {
            var missing = new List<string>();
            foreach (BlockType type in System.Enum.GetValues(typeof(BlockType)))
                if (!assigned[(int)type]) missing.Add(type.ToString());
            if (missing.Count > 0)
            {
                string examples = string.Join(", ", missing.GetRange(0, Mathf.Min(8, missing.Count)));
                Debug.LogWarning("Block Data is missing " + missing.Count + " definitions (" + examples +
                    "). Runtime defaults and missing-image textures will be used; assigned asset entries are preserved.", this);
            }
        }
        return images;
    }

    private static int Register(Texture2D image, List<Texture2D> images, Dictionary<Texture2D, int> indices)
    {
        if (image == null) return 0;
        if (indices.TryGetValue(image, out int index)) return index;
        index = images.Count + 1;
        images.Add(image);
        indices.Add(image, index);
        return index;
    }

    private void BuildFaceTextures(List<Texture2D> images)
    {
        int size = Mathf.Clamp(textureData != null ? textureData.faceTextureResolution : 32, 16, 512);
        var array = new Texture2DArray(size, size, images.Count + 1, TextureFormat.RGBA32, false, false)
        {
            name = "Block face images",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var missing = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            missing[y * size + x] = ((x / 4 + y / 4) % 2 == 0)
                ? new Color32(255, 0, 255, 255) : new Color32(0, 0, 0, 255);
        array.SetPixels32(missing, 0);
        try
        {
            for (int i = 0; i < images.Count; i++)
                array.SetPixels32(ReadFacePixels(images[i], size), i + 1);
            array.Apply(false, true);
        }
        catch
        {
            Destroy(array);
            throw;
        }
        if (faceTextures != null) Destroy(faceTextures);
        faceTextures = array;
        // Global binding serves chunks and inventory cubes without instantiating
        // a material for each block/texture. Both use the Voxel shader.
        Shader.SetGlobalTexture("_BlockFaceTextures", faceTextures);
    }

    private static Color32[] ReadFacePixels(Texture2D image, int size)
    {
        if (image.isReadable && image.width == size && image.height == size)
            return image.GetPixels32();

        // Accept normal imported PNGs without requiring Read/Write or a specific
        // compression format. This readback happens once, when the world starts.
        var previousTarget = RenderTexture.active;
        bool previousSRGB = GL.sRGBWrite;
        var previousFilter = image.filterMode;
        var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var readable = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        try
        {
            image.filterMode = FilterMode.Point;
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(image, target);
            RenderTexture.active = target;
            readable.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
            return readable.GetPixels32();
        }
        finally
        {
            image.filterMode = previousFilter;
            GL.sRGBWrite = previousSRGB;
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            Destroy(readable);
        }
    }

    private void OnDisable()
    {
        if (activeManager == this)
        {
            Shader.SetGlobalTexture("_BlockFaceTextures", null);
            activeManager = null;
        }
        if (faceTextures != null) Destroy(faceTextures);
        faceTextures = null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BlockDataManager))]
public class BlockDataManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Assign Top, Bottom and Side images in Block Data. Restart Play Mode after changing face images.", MessageType.Info);
    }
}
#endif
