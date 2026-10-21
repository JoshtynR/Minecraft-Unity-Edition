using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Survives the menu scene. Scene activation alone does not mean the voxel world is playable.
public sealed class WorldLoadingScreen : MonoBehaviour
{
    private static WorldLoadingScreen instance;
    public static bool IsLoading => instance != null && instance.visible;
    private bool visible, enteredWorld;
    private Canvas canvas;
    private TextMeshProUGUI status;
    private Material textMaterial;
    private WorldLoadReadiness readiness;

    public static void Begin(Transform menuTemplate = null)
    {
        if (Application.isBatchMode) return;
        if (instance != null) return;
        var root = new GameObject("WorldLoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        instance = root.AddComponent<WorldLoadingScreen>();
        instance.Build(menuTemplate);
    }
    public static void Cancel()
    {
        if (instance != null) instance.Finish();
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneLoaded;
        enteredWorld = IsWorldScene(SceneManager.GetActiveScene().name);
    }
    private static bool IsWorldScene(string name) => name != null && name.Contains("World");
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsWorldScene(scene.name)) enteredWorld = true;
        else if (enteredWorld) Finish();
    }
    private void Build(Transform template)
    {
        visible = true;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; canvas.pixelPerfect = true;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(Screen.width / 320f, Screen.height / 240f)));
        var background = Rect("Background", transform, 0, 0, 0, 0);
        background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
        var image = background.gameObject.AddComponent<Image>(); image.color = Color.black;
        // Reuse the joining screen's existing backdrop without keeping its menu scripts/buttons alive.
        if (template != null)
        {
            var source = template.GetComponentInChildren<Image>(true);
            if (source != null) { image.sprite = source.sprite; image.material = source.material; image.color = source.color; image.type = source.type; }
        }
        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        status = Rect("Status", transform, 0, 8, 300, 24).gameObject.AddComponent<TextMeshProUGUI>();
        status.font = assets != null && assets.levelFont != null ? assets.levelFont : TMP_Settings.defaultFontAsset;
        status.fontSize = 8; status.color = Color.white; status.alignment = TextAlignmentOptions.Center;
        status.enableWordWrapping = false; status.extraPadding = true; status.raycastTarget = false;
        textMaterial = MinecraftUIText.CreateMaterial(status.font, 8, true);
        if (textMaterial != null) status.fontSharedMaterial = textMaterial;
        status.text = "Joining world...";
        var cancel = Rect("Cancel", transform, 0, -28, 150, 20);
        cancel.gameObject.AddComponent<Image>().color = new Color(.2f, .2f, .2f, 1);
        cancel.gameObject.AddComponent<Button>().onClick.AddListener(CancelLoad);
        var label = Rect("Label", cancel, 0, 0, 150, 20).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = status.font; label.fontSize = 8; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.extraPadding = true;
        if (textMaterial != null) label.fontSharedMaterial = textMaterial;
    }
    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(parent, false);
        var rect = (RectTransform)child.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
    private void LateUpdate()
    {
        if (!visible || !enteredWorld) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var world = World.Instance; var manager = GameManager.Instance;
        var player = manager != null ? manager.localPlayer : null;
        bool mapReady = world != null && world.IsWorldCreated && !world.disabled &&
            !NetworkClient.isLoadingScene && !NetworkServer.isLoadingScene;
        bool playerReady = manager != null && manager.playerSpawned && player != null && player.IsReadyForPlay && player.inventory != null;
        bool spawnReady = false;
        if (mapReady && playerReady)
        {
            var position = WorldDataHelper.GetChunkPosition(world, Vector3Int.FloorToInt(player.transform.position));
            ChunkRenderer chunk;
            spawnReady = world.worldData.chunkDict.TryGetValue(position, out chunk) && chunk != null &&
                chunk.ChunkData != null && chunk.gameObject.activeInHierarchy;
        }
        status.text = !mapReady ? "Building terrain..." : !playerReady ? "Loading player..." : !spawnReady ? "Loading spawn area..." : "Entering world...";
        if (readiness.Advance(mapReady, playerReady, spawnReady, Time.frameCount)) Finish(true);
    }
    private void CancelLoad()
    {
        var network = NetworkManager.singleton;
        bool inWorld = IsWorldScene(SceneManager.GetActiveScene().name);
        Cancel();
        if (network != null)
        {
            if (NetworkServer.active) network.StopHost(); else network.StopClient();
        }
        if (inWorld && (network == null || string.IsNullOrEmpty(network.offlineScene))) SceneManager.LoadScene("MainMenu");
        else if (!inWorld && Menu.currentMenu != null && Menu.lastMenu != null) Menu.CloseCurrentMenu();
    }
    private void Finish(bool enterGame = false)
    {
        visible = false;
        if (enterGame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (canvas != null) canvas.enabled = false;
        if (instance == this) instance = null;
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (textMaterial != null) Destroy(textMaterial);
        if (instance == this) instance = null;
    }
}

// Require two completed frames after all conditions hold, including deferred player-save restoration.
public struct WorldLoadReadiness
{
    private int readyFrame;
    private bool waiting;
    public bool Advance(bool mapReady, bool playerReady, bool spawnReady, int frame)
    {
        if (!mapReady || !playerReady || !spawnReady) { waiting = false; return false; }
        if (!waiting) { waiting = true; readyFrame = frame; }
        return frame - readyFrame >= 2;
    }
}
