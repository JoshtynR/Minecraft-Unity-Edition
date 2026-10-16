using UnityEditor;
using UnityEngine;

public class BlockDataEditor : EditorWindow
{
    public BlockDataManager blockDataManager;
    public BlockTypeData selectedBlockTypeData;
    private BlockDataSO data;
    private string search = "";
    private Vector2 listScroll, detailScroll;

    [MenuItem("Minecraft/BlockDataEditor")]
    public static void ShowWindow() { GetWindow<BlockDataEditor>("Block Data"); }

    private void OnEnable()
    {
        blockDataManager = FindObjectOfType<BlockDataManager>();
        data = blockDataManager != null ? blockDataManager.textureData : AssetDatabase.LoadAssetAtPath<BlockDataSO>("Assets/Block Data.asset");
    }

    private void OnGUI()
    {
        data = (BlockDataSO)EditorGUILayout.ObjectField("Block Data", data, typeof(BlockDataSO), false);
        if (data == null || data.textureDataList == null) return;
        search = EditorGUILayout.TextField("Search", search);
        EditorGUILayout.BeginHorizontal();
        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Width(220));
        foreach (var block in data.textureDataList)
        {
            if (block == null || (!string.IsNullOrEmpty(search) && block.blockType.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
            if (GUILayout.Button(block.blockType.ToString())) selectedBlockTypeData = block;
        }
        EditorGUILayout.EndScrollView();
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        if (selectedBlockTypeData != null && data.textureDataList.Contains(selectedBlockTypeData)) DrawDetails();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Save Block Data")) AssetDatabase.SaveAssetIfDirty(data);
    }

    private void DrawDetails()
    {
        var block = selectedBlockTypeData;
        EditorGUILayout.LabelField(block.blockType.ToString(), EditorStyles.boldLabel);
        BetaBlockCatalog.Entry entry;
        if (BetaBlockCatalog.TryGet(block.blockType, out entry))
        {
            EditorGUILayout.LabelField("Beta ID / metadata", entry.SourceId + " / " + entry.Metadata);
            EditorGUILayout.LabelField("Hardness / hand harvest", entry.Hardness + " / " + entry.HandHarvestable);
        }
        EditorGUILayout.HelpBox("Assign Top, Bottom and Side images. Restart Play Mode to rebuild the runtime face texture array. Special block models and gameplay are separate from these catalog definitions.", MessageType.Info);
        if (block.textureData == null) block.textureData = new TextureData();
        EditorGUI.BeginChangeCheck();
        var top = (Texture2D)EditorGUILayout.ObjectField("Top", block.textureData.top, typeof(Texture2D), false);
        var bottom = (Texture2D)EditorGUILayout.ObjectField("Bottom", block.textureData.bottom, typeof(Texture2D), false);
        var side = (Texture2D)EditorGUILayout.ObjectField("Side", block.textureData.side, typeof(Texture2D), false);
        EditorGUILayout.LabelField("Optional directional overrides (blank = Side)", EditorStyles.boldLabel);
        var front = (Texture2D)EditorGUILayout.ObjectField("Front", block.textureData.front, typeof(Texture2D), false);
        var back = (Texture2D)EditorGUILayout.ObjectField("Back", block.textureData.back, typeof(Texture2D), false);
        var left = (Texture2D)EditorGUILayout.ObjectField("Left", block.textureData.left, typeof(Texture2D), false);
        var right = (Texture2D)EditorGUILayout.ObjectField("Right", block.textureData.right, typeof(Texture2D), false);
        bool animate=EditorGUILayout.Toggle("Animate face sheets",block.textureData.animateFaces);
        int columns=block.textureData.animationColumns,rows=block.textureData.animationRows,frames=block.textureData.animationFrameCount;
        float fps=block.textureData.animationFramesPerSecond;
        if(animate)
        {
            columns=EditorGUILayout.IntSlider("Sheet columns",columns,1,64);
            rows=EditorGUILayout.IntSlider("Sheet rows (0 = auto)",rows,0,256);
            frames=EditorGUILayout.IntField("Frames (0 = all)",frames);
            fps=EditorGUILayout.Slider("Frames per second",fps,0,120);
            EditorGUILayout.HelpBox("Frames run left-to-right, top-to-bottom. Vertical Minecraft strips: Columns 1, Rows 0, Frames 0. Apply sheets to Top/Bottom/Side; restart Play Mode after changes.",MessageType.Info);
        }
        bool transparent = EditorGUILayout.Toggle("Cutout / transparent faces", block.isTransparent);
        bool collider = EditorGUILayout.Toggle("Collidable", block.generateCollider);
        int opacity = EditorGUILayout.IntSlider("Light opacity", block.opacity, 0, 15);
        int emission = EditorGUILayout.IntSlider("Light emission", block.lightEmission, 0, 15);
        Color tint = EditorGUILayout.ColorField("Tint", block.albedoTint);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Edit block textures and lighting");
            block.textureData.top = top; block.textureData.bottom = bottom; block.textureData.side = side;
            block.textureData.front = front; block.textureData.back = back; block.textureData.left = left; block.textureData.right = right;
            block.textureData.animateFaces=animate;block.textureData.animationColumns=columns;block.textureData.animationRows=rows;
            block.textureData.animationFrameCount=System.Math.Max(0,frames);block.textureData.animationFramesPerSecond=fps;
            block.isTransparent = transparent; block.generateCollider = collider;
            block.opacity = (byte)opacity; block.lightEmission = (byte)emission; block.albedoTint = tint;
            EditorUtility.SetDirty(data);
        }
        var serialized=new SerializedObject(data);
        serialized.Update();
        int selectedIndex=data.textureDataList.IndexOf(block);
        if(selectedIndex>=0)
            SpecialTextureEditor.Draw(serialized.FindProperty("textureDataList").GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("textureData"),block.blockType);
        serialized.ApplyModifiedProperties();
        EditorGUILayout.BeginHorizontal();
        DrawImage(top); DrawImage(bottom); DrawImage(side);
        EditorGUILayout.EndHorizontal();
        var player = Application.isPlaying && GameManager.Instance != null ? GameManager.Instance.localPlayer : null;
        using (new EditorGUI.DisabledScope(player == null || block.blockType == BlockType.Nothing || block.blockType == BlockType.Air))
            if (GUILayout.Button("Give 64 to Local Player (Play Mode)") && player != null) player.inventory.AddItem(block.blockType, 64);
    }

    private void DrawImage(Texture2D image)
    {
        Rect rect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80), GUILayout.Height(80));
        if (image != null) EditorGUI.DrawPreviewTexture(rect, image, null, ScaleMode.ScaleToFit);
    }
}
