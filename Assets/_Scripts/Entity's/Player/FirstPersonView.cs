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
    private float equip = 1, swing = 1, bobPhase, bobAmount, uiDepth, ownedUIDepth;
    private int sourceMask;
    private Vector3 lastPosition;

    public void Initialize(Player owner)
    {
        if (player != null || owner == null || owner.cam == null) return;
        player = owner;
        sourceCamera = owner.cam.GetComponent<Camera>();
        if (sourceCamera == null) { enabled = false; return; }
        sourceMask = sourceCamera.cullingMask;
        sourceCamera.cullingMask &= ~(1 << HandLayer);
        cameraObject = new GameObject("First person camera");
        cameraObject.transform.SetParent(owner.cam, false);
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
        lastPosition = owner.transform.position;
        Show(ItemType.Nothing);
    }

    private void LateUpdate()
    {
        if (handCamera == null || spriteMaterial == null || player == null) return;
        bool visible = player.IsReadyForPlay && player.health > 0 && !WorldLoadingScreen.IsLoading &&
            player.inventory != null && !player.inventory.isOpen && !player.escapeMenuOpen && sourceCamera.enabled;
        handCamera.enabled = visible;
        if (!visible)
        {
            hidden = true;
            swing = 1;
            bobAmount = 0;
            lastPosition = player.transform.position;
            return;
        }
        // Follow viewport changes (split-screen, resizing), without inheriting sprint FOV zoom.
        handCamera.rect = sourceCamera.rect;
        handCamera.targetTexture = sourceCamera.targetTexture;
        var held = player.inventory.HeldItem();
        ItemType wanted = held.Empty ? ItemType.Nothing : held.type;
        if (hidden) { Show(wanted); equip = 1; hidden = false; }
        equip = Mathf.MoveTowards(equip, wanted == shown ? 1 : 0, Time.deltaTime * 5);
        if (wanted != shown && equip <= 0) Show(wanted);
        if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) && swing >= 1) swing = 0;
        swing = Mathf.Min(1, swing + Time.deltaTime / .3f);
        Vector3 delta = player.transform.position - lastPosition;
        lastPosition = player.transform.position;
        // Ignore vertical travel and teleports; animate only actual horizontal movement.
        float distance = Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
        if (distance > 1) distance = 0;
        bobAmount = Mathf.MoveTowards(bobAmount, Mathf.Min(1, distance / Mathf.Max(.001f, Time.deltaTime) / 4), Time.deltaTime * 8);
        bobPhase += distance * 7;
        float hit = Mathf.Sin(swing * Mathf.PI);
        Vector3 bob = new Vector3(Mathf.Sin(bobPhase) * .018f, Mathf.Abs(Mathf.Cos(bobPhase)) * .018f, 0) * bobAmount;
        bool arm = shown == ItemType.Nothing || !hasModel;
        Vector3 position = arm ? new Vector3(.55f, -.68f, .68f) : new Vector3(.43f, -.33f, .72f);
        Vector3 rotation = arm ? new Vector3(165, 0, 25) : isBlock ? new Vector3(-15, 45, 0) : new Vector3(-12, -35, -15);
        pivot.localPosition = position + bob + new Vector3(-hit * .18f, -(1 - equip) * .8f + hit * .06f, -hit * .15f);
        pivot.localRotation = Quaternion.Euler(rotation + new Vector3(-hit * 35, -hit * 25, hit * 15));
        pivot.localScale = Vector3.one * (arm ? .8f : isBlock ? .38f : .55f);
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
                    mesh = icon != null ? FirstPersonMesh.Sprite(icon.width, icon.height) : null;
                }
                itemMeshes.Add(type, mesh);
                blockModels.Add(type, isBlock);
            }
            else isBlock = blockModels[type];
            hasModel = mesh != null;
        }
        filter.sharedMesh = hasModel ? mesh : armMesh;
        geometry.localPosition = hasModel ? new Vector3(-.5f, -.5f, isBlock ? -.5f : 0) : Vector3.zero;
        modelRenderer.sharedMaterial = isBlock ? blockMaterial : spriteMaterial;
        spriteMaterial.mainTexture = hasModel ? icon : skin;
    }

    private void UpdateLight()
    {
        var world = World.Instance;
        var block = world != null ? world.GetBlock(player.cam.position) : null;
        float brightness = block == null ? 1 : Beta173Lighting.Brightness(block.GetSkyLight(), block.GetBlockLight(), world.betaSkylightSubtracted, 0);
        spriteMaterial.SetFloat("_Brightness", brightness);
        blockMaterial.SetFloat("_Brightness", world == null ? 1 : brightness / Mathf.Max(.05f,
            Beta173Lighting.Brightness(14, 0, world.betaSkylightSubtracted, 0)));
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
