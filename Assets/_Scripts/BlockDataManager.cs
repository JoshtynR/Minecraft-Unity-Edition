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
    private Texture2D faceAnimations;
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

    private sealed class FaceSheet
    {
        public Texture2D image;
        public int columns, rows, count, first;
        public float fps;
        public bool pingPong;
    }

    private List<FaceSheet> InitializeData()
    {
        var entries = textureData != null ? textureData.textureDataList : null;
        int maxType = 0;
        if (entries != null)
            foreach (var item in entries)
                if (item != null) maxType = Mathf.Max(maxType, (int)item.blockType);
        var definitions = BlockDataDefaults.CreateLookup(maxType + 1);
        var assigned = new bool[definitions.Length];
        var images = new List<FaceSheet>();
        var indices = new Dictionary<System.Tuple<Texture2D,int,int,int,float,bool>, int>();
        int nextLayer = 1;
        if (entries != null)
            foreach (var item in entries)
            {
                if (item == null || (int)item.blockType < 0) continue;
                int id = (int)item.blockType;
                definitions[id] = item;
                assigned[id] = true;
                if (item.textureData == null) item.textureData = new TextureData();
                var settings=item.textureData;
                bool fluid=item.blockType==BlockType.Water||item.blockType==BlockType.Lava;
                bool lava=item.blockType==BlockType.Lava;
                settings.topIndex = Register(settings.top, settings, images, indices, ref nextLayer,fluid,null,lava);
                settings.bottomIndex = Register(settings.bottom, settings, images, indices, ref nextLayer,fluid,null,lava);
                settings.sideIndex = Register(settings.side, settings, images, indices, ref nextLayer,fluid,null,false);
                settings.frontIndex = Register(settings.front, settings, images, indices, ref nextLayer,fluid,null,false);
                settings.backIndex = Register(settings.back, settings, images, indices, ref nextLayer,fluid,null,false);
                settings.leftIndex = Register(settings.left, settings, images, indices, ref nextLayer,fluid,null,false);
                settings.rightIndex = Register(settings.right, settings, images, indices, ref nextLayer,fluid,null,false);
                settings.ResetPartIndices();
                if(settings.specialTextures!=null)
                {
                    var parts=new HashSet<BlockTexturePart>();
                    foreach(var part in settings.specialTextures)
                        if(part!=null&&parts.Add(part.part))
                            settings.SetPartIndex(part.part,Register(part.image,settings,images,indices,ref nextLayer,fluid,part,lava&&part.part==BlockTexturePart.FluidStill));
                }
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

    private int Register(Texture2D image, TextureData settings, List<FaceSheet> images,
        Dictionary<System.Tuple<Texture2D,int,int,int,float,bool>, int> indices, ref int nextLayer,bool fluid=false,SpecialBlockTexture part=null,bool pingPong=false)
    {
        if (image == null || image.width <= 0 || image.height <= 0) return 0;
        int columns=1,rows=1,count=1;
        float fps=0;
        bool animate=settings.animateFaces;
        int requestedColumns=settings.animationColumns,requestedRows=settings.animationRows,requestedCount=settings.animationFrameCount;
        float requestedFps=settings.animationFramesPerSecond;
        if(part!=null&&part.overrideAnimation)
        {
            animate=part.animate;requestedColumns=part.columns;requestedRows=part.rows;
            requestedCount=part.frameCount;requestedFps=part.framesPerSecond;
        }
        else if(fluid&&requestedColumns==1)
        {
            // Still/flow strips can have different frame counts. A block-wide
            // row/count setting must not squeeze one strip into the other's grid.
            requestedRows=0;requestedCount=0;
        }
        if(part!=null&&part.playback!=SheetPlayback.Automatic)pingPong=part.playback==SheetPlayback.PingPong;
        // Repair legacy 20fps defaults for the actual modern fluid filenames at
        // runtime, including already-assigned slots. Custom speeds/playback stay
        // explicit overrides; no Block Data asset links need to be rewritten.
        if(fluid&&requestedFps==20&&(part==null||part.playback==SheetPlayback.Automatic))
            requestedFps=ModernFluidTextures.FramesPerSecond(image.name);
        if(animate)
        {
            columns=Mathf.Clamp(requestedColumns,1,64);
            rows=requestedRows<=0 ? Mathf.Max(1,image.height*columns/image.width) : Mathf.Clamp(requestedRows,1,256);
            bool invalidAuto=requestedRows<=0 && image.height*columns%image.width!=0;
            if(invalidAuto || image.width%columns!=0 || image.height%rows!=0 || columns>image.width || rows>image.height)
            {
                Debug.LogWarning("Invalid animation grid for '"+image.name+"'. Using it as a static face image.",this);
                columns=rows=1;
            }
            int cells=columns*rows;
            count=requestedCount<=0 ? cells : Mathf.Clamp(requestedCount,1,cells);
            fps=float.IsNaN(requestedFps)||float.IsInfinity(requestedFps)?20:Mathf.Clamp(requestedFps,0,120);
            if(count==1)fps=0;
        }
        pingPong=pingPong&&count>1;
        var key=System.Tuple.Create(image,columns,rows,count,fps,pingPong);
        if (indices.TryGetValue(key, out int index)) return index;
        index = nextLayer;
        nextLayer+=count;
        images.Add(new FaceSheet{image=image,columns=columns,rows=rows,count=count,fps=fps,first=index,pingPong=pingPong});
        indices.Add(key, index);
        return index;
    }

    private void BuildFaceTextures(List<FaceSheet> images)
    {
        int size = Mathf.Clamp(textureData != null ? textureData.faceTextureResolution : 32, 16, 512);
        int depth=1;
        foreach(var sheet in images)depth+=sheet.count;
        var array = new Texture2DArray(size, size, depth, TextureFormat.RGBA32, false, false)
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
        Texture2D animations=null;
        try
        {
            var timing=new Color[depth];
            timing[0]=new Color(0,1,0,0);
            foreach(var sheet in images)
            {
                // Read at source resolution; resizing a whole vertical strip to
                // output resolution could exceed the GPU's maximum 2D height.
                var pixels=ReadFacePixels(sheet.image,sheet.image.width,sheet.image.height);
                int frameWidth=sheet.image.width/sheet.columns,frameHeight=sheet.image.height/sheet.rows;
                for(int frame=0;frame<sheet.count;frame++)
                {
                    int x=frame%sheet.columns,y=sheet.rows-1-frame/sheet.columns;
                    var tile=new Color32[size*size];
                    for(int row=0;row<size;row++)for(int column=0;column<size;column++)
                    {
                        int sourceX=x*frameWidth+(2*column+1)*frameWidth/(2*size);
                        int sourceY=y*frameHeight+(2*row+1)*frameHeight/(2*size);
                        tile[row*size+column]=pixels[sourceY*sheet.image.width+sourceX];
                    }
                    array.SetPixels32(tile,sheet.first+frame);
                    timing[sheet.first+frame]=new Color(sheet.first,sheet.count,sheet.fps,sheet.pingPong?1:0);
                }
            }
            array.Apply(false, true);
            animations=new Texture2D(depth,1,TextureFormat.RGBAFloat,false,true)
            {
                name="Block face animation timing",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp
            };
            animations.SetPixels(timing);
            animations.Apply(false,true);
        }
        catch
        {
            Destroy(array);
            if(animations!=null)Destroy(animations);
            throw;
        }
        if (faceTextures != null) Destroy(faceTextures);
        if (faceAnimations != null) Destroy(faceAnimations);
        faceTextures = array;
        faceAnimations = animations;
        // Global binding serves chunks and inventory cubes without instantiating
        // a material for each block/texture. Both use the Voxel shader.
        Shader.SetGlobalTexture("_BlockFaceTextures", faceTextures);
        Shader.SetGlobalTexture("_BlockFaceAnimations", faceAnimations);
        Shader.SetGlobalFloat("_BlockFaceAnimationWidth",depth);
    }

    private static Color32[] ReadFacePixels(Texture2D image, int width, int height)
    {
        if (image.isReadable && image.width == width && image.height == height)
            return image.GetPixels32();

        // Accept normal imported PNGs without requiring Read/Write or a specific
        // compression format. This readback happens once, when the world starts.
        var previousTarget = RenderTexture.active;
        bool previousSRGB = GL.sRGBWrite;
        var previousFilter = image.filterMode;
        var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        try
        {
            image.filterMode = FilterMode.Point;
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(image, target);
            RenderTexture.active = target;
            readable.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
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
            Shader.SetGlobalTexture("_BlockFaceAnimations", (Texture2D)null);
            Shader.SetGlobalFloat("_BlockFaceAnimationWidth",0);
            activeManager = null;
        }
        if (faceTextures != null) Destroy(faceTextures);
        if (faceAnimations != null) Destroy(faceAnimations);
        faceTextures = null;
        faceAnimations = null;
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
