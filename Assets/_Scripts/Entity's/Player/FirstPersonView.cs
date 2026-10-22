using System.Collections.Generic;
using UnityEngine;

// Created only by the local Player, after the third-person lighting mesh list is captured.
// Everything generated here belongs to this component; source assets are never changed.
public sealed class FirstPersonView : MonoBehaviour
{
    private const int HandLayer = 7;
    private Player player;
    private Camera sourceCamera, handCamera, uiCamera;
    private GameObject cameraObject;
    private Transform pivot, geometry;
    private MeshFilter filter;
    private MeshRenderer modelRenderer;
    private Material spriteMaterial, blockMaterial;
    private Mesh armMesh;
    private readonly Dictionary<ItemType, Mesh> itemMeshes = new Dictionary<ItemType, Mesh>();
    private readonly Dictionary<ItemType, bool> blockModels = new Dictionary<ItemType, bool>();
    private Texture skin;
    private ItemType shown = ItemType.Nothing;
    private bool hasModel, isBlock, hidden;
    private float equip = 1, swing = 1, uiDepth, ownedUIDepth;
    private int sourceMask;

    public void Initialize(Player owner)
    {
        if (player != null || owner == null || owner.cam == null) return;
        player = owner;
        sourceCamera = owner.cam.GetComponent<Camera>();
        if (sourceCamera == null) { enabled = false; return; }
        sourceMask = sourceCamera.cullingMask;
        // Local scene copies may still render the third-person body layer.
        // Its animated right arm must not sit over the generated hand.
        sourceCamera.cullingMask &= ~((1 << HandLayer) | (1 << 6));
        cameraObject = new GameObject("First person camera");
        // The player prefab's camera has nonuniform inherited scale. A child
        // camera ignores that scale for projection, but its mesh children do not.
        // Keep this owned root unparented and copy only the real camera pose.
        cameraObject.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        handCamera = cameraObject.AddComponent<Camera>();
        handCamera.clearFlags = CameraClearFlags.Depth;
        handCamera.cullingMask = 1 << HandLayer;
        handCamera.nearClipPlane = .01f;
        handCamera.farClipPlane = 4;
        handCamera.fieldOfView = 70;
        handCamera.depth = sourceCamera.depth + 1;
        handCamera.useOcclusionCulling = false;
        handCamera.allowHDR = sourceCamera.allowHDR;
        handCamera.allowMSAA = sourceCamera.allowMSAA;
        handCamera.enabled = false;
        var canvasObject = GameObject.Find("PlayerCanvas");
        var canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        uiCamera = canvas != null ? canvas.worldCamera : null;
        if (uiCamera != null && uiCamera != sourceCamera)
        {
            uiDepth = uiCamera.depth;
            ownedUIDepth = Mathf.Max(uiDepth, handCamera.depth + 1);
            uiCamera.depth = ownedUIDepth;
        }
        pivot = new GameObject("Hand pose").transform;
        pivot.SetParent(cameraObject.transform, false);
        geometry = new GameObject("Held model").transform;
        geometry.SetParent(pivot, false);
        geometry.gameObject.layer = HandLayer;
        filter = geometry.gameObject.AddComponent<MeshFilter>();
        modelRenderer = geometry.gameObject.AddComponent<MeshRenderer>();
        modelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        modelRenderer.receiveShadows = false;
        var spriteTemplate = Resources.Load<Material>("FirstPersonMaterial");
        var blockTemplate = Resources.Load<Material>("DroppedBlockMaterial");
        if (spriteTemplate == null || blockTemplate == null)
        {
            Debug.LogError("First-person material resources are missing.");
            enabled = false;
            return;
        }
        spriteMaterial = new Material(spriteTemplate);
        blockMaterial = new Material(blockTemplate);
        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        skin = assets != null ? assets.inventoryPlayerSkin : null;
        if (skin == null)
        {
            var skinMaterial = Resources.Load<Material>("InventoryPlayerMaterial");
            skin = skinMaterial != null ? skinMaterial.mainTexture : null;
        }
        if (skin == null) skin = Texture2D.whiteTexture;
        armMesh = FirstPersonMesh.Arm(skin.width, skin.height);
        Show(ItemType.Nothing);
        Debug.Log("[FirstPersonView] Beta arm pose v2 loaded.");
    }

    private void LateUpdate()
    {
        if (handCamera == null || spriteMaterial == null || player == null || sourceCamera == null) return;
        cameraObject.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        bool visible = player.IsReadyForPlay && player.health > 0 && !WorldLoadingScreen.IsLoading &&
            player.inventory != null && !player.inventory.isOpen && !player.escapeMenuOpen && sourceCamera.enabled;
        handCamera.enabled = visible;
        if (!visible)
        {
            hidden = true;
            swing = 1;
            return;
        }
        // Follow viewport changes (split-screen, resizing), without inheriting sprint FOV zoom.
        handCamera.rect = sourceCamera.rect;
        handCamera.targetTexture = sourceCamera.targetTexture;
        handCamera.targetDisplay = sourceCamera.targetDisplay;
        var held = player.inventory.HeldItem();
        ItemType wanted = held.Empty ? ItemType.Nothing : held.type;
        if (hidden) { Show(wanted); equip = 1; hidden = false; }
        equip = Mathf.MoveTowards(equip, wanted == shown ? 1 : 0, Time.deltaTime * 5);
        if (wanted != shown && equip <= 0) Show(wanted);
        if (!player.IsEating && (Input.GetMouseButton(0) || Input.GetMouseButton(1)) && swing >= 1) swing = 0;
        swing = Mathf.Min(1, swing + Time.deltaTime / .3f);
        Vector3 bob = Vector3.zero;
        bool arm = shown == ItemType.Nothing || !hasModel;
        if(arm)FirstPersonArmPose.Apply(pivot,swing,equip,bob);
        else FirstPersonItemPose.Apply(pivot,isBlock,shown==ItemType.FishingRod,swing,equip,bob);
        if(player.IsEating)
        {
            float chew=Mathf.Abs(Mathf.Sin(player.EatingProgress*Mathf.PI*8));
            pivot.localPosition+=new Vector3(-.35f,.32f-chew*.035f,-.1f);
            pivot.localRotation=Quaternion.Euler(chew*8,0,10)*pivot.localRotation;
        }
        player.ApplyFirstPersonBob(pivot);
        UpdateLight();
    }

    private void Show(ItemType type)
    {
        shown = type;
        isBlock = false;
        hasModel = false;
        Texture icon = null;
        Mesh mesh = null;
        var definition = type == ItemType.Nothing ? null : ItemCatalog.Get(type);
        if (definition != null)
        {
            icon = definition.icon;
            if (!itemMeshes.TryGetValue(type, out mesh))
            {
                BlockType block;
                // Solid block items remain 3D even when they also have an inventory icon.
                bool buildBlock = ItemCatalog.TryPlacedBlock(type, out block) &&
                    !(icon != null && (BlockShapesExtensions.IsPlant(block) || SpecialBlockGeometry.FlatShading(block)));
                if (buildBlock) mesh = ItemVisualMesh.Build(block);
                isBlock = mesh != null && mesh.vertexCount > 0;
                if (!isBlock)
                {
                    if (mesh != null) Destroy(mesh);
                    mesh = icon != null ? FirstPersonMesh.Sprite(icon.width, icon.height,true) : null;
                }
                itemMeshes.Add(type, mesh);
                blockModels.Add(type, isBlock);
            }
            else isBlock = blockModels[type];
            hasModel = mesh != null;
        }
        filter.sharedMesh = hasModel ? mesh : armMesh;
        geometry.localPosition = hasModel ? isBlock?new Vector3(-.5f,-.5f,-.5f):new Vector3(-.9375f,-.0625f,0) : Vector3.zero;
        modelRenderer.sharedMaterial = isBlock ? blockMaterial : spriteMaterial;
        spriteMaterial.mainTexture = hasModel ? icon : skin;
    }

    private void UpdateLight()
    {
        var world = player.CurrentWorld;
        var block = world != null ? world.GetBlock(player.cam.position) : null;
        var levels=new Vector4(block==null?15:block.GetSkyLight(),block==null?0:block.GetBlockLight(),1,0);
        spriteMaterial.SetVector("_LightLevels",levels);
        blockMaterial.SetVector("_LightLevels",levels);
        float brightness = block == null ? 1 : Beta173Lighting.Brightness(block.GetSkyLight(), block.GetBlockLight(), world.betaSkylightSubtracted, 0,world.dimension == WorldDimension.Nether);
        spriteMaterial.SetFloat("_Brightness", brightness);
        blockMaterial.SetFloat("_Brightness", world == null ? 1 : brightness / Mathf.Max(.05f,
            Beta173Lighting.Brightness(14, 0, world.betaSkylightSubtracted, 0,world.dimension == WorldDimension.Nether)));
    }

    private void OnDisable()
    {
        if (handCamera != null) handCamera.enabled = false;
        hidden = true;
    }

    private void OnDestroy()
    {
        if (sourceCamera != null) sourceCamera.cullingMask = sourceMask;
        if (uiCamera != null && uiCamera.depth == ownedUIDepth) uiCamera.depth = uiDepth;
        if (cameraObject != null) Destroy(cameraObject);
        if (armMesh != null) Destroy(armMesh);
        foreach (var mesh in itemMeshes.Values) if (mesh != null) Destroy(mesh);
        itemMeshes.Clear();
        blockModels.Clear();
        if (spriteMaterial != null) Destroy(spriteMaterial);
        if (blockMaterial != null) Destroy(blockMaterial);
    }
}
