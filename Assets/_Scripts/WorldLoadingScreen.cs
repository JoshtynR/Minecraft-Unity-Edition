using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Survives the menu scene. Scene activation alone does not mean the voxel world is playable.
public sealed class WorldLoadingScreen : MonoBehaviour
{
    private static WorldLoadingScreen instance;
    public static bool IsLoading => instance != null && instance.visible;
    private bool visible, enteredWorld;
    private Canvas canvas;
    private TextMeshProUGUI status;
    private RawImage dirt;
    private RectTransform progressFill;
    private float progress;
    private int lastWidth,lastHeight;
    private Material textMaterial;
    private WorldLoadReadiness readiness;

    public static void Begin()
    {
        if (Application.isBatchMode) return;
        SuppressPreview();
        if (instance != null) return;
        var root = new GameObject("WorldLoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        instance = root.AddComponent<WorldLoadingScreen>();
        instance.Build();
    }
    // Also enforce this at runtime for locally saved scenes that still have
    // the original isometric camera enabled. The UI camera remains active.
    private static void SuppressPreview()
    {
        if(!IsWorldScene(SceneManager.GetActiveScene().name))return;
        var preview=GameObject.Find("Rendering/Camera");
        var camera=preview!=null?preview.GetComponent<Camera>():null;
        if(camera!=null)camera.enabled=false;
    }
    public static void ReportProgress(string message,float value)
    {
        if(instance==null)return;
        instance.progress=Mathf.Max(instance.progress,Mathf.Clamp01(value));
        if(instance.status!=null)instance.status.text=message;
        if(instance.progressFill!=null)instance.progressFill.sizeDelta=new Vector2(Mathf.FloorToInt(instance.progress*100),2);
    }
    public static void ResetProgress()
    {if(instance!=null){instance.progress=0;ReportProgress("Building terrain",0);}}
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
        if (IsWorldScene(scene.name)) {enteredWorld = true;SuppressPreview();}
        else if (enteredWorld) Finish();
    }
    private void Build()
    {
        visible = true;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; canvas.pixelPerfect = true;
        GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var background = Rect("Background", transform, 0, 0, 0, 0);
        background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
        // Always opaque and use the default UI material. Never inherit the
        // old transparent/blurred menu backdrop that exposed the world camera.
        background.gameObject.AddComponent<Image>().color=Color.black;
        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if(assets!=null&&assets.loadingBackground!=null)
        {
            var tiles=Rect("Dirt",background,0,0,0,0);tiles.anchorMin=Vector2.zero;tiles.anchorMax=Vector2.one;
            dirt=tiles.gameObject.AddComponent<RawImage>();dirt.texture=assets.loadingBackground;
            dirt.color=new Color(64/255f,64/255f,64/255f,1);dirt.raycastTarget=false;
        }
        var font=assets!=null&&assets.levelFont!=null?assets.levelFont:TMP_Settings.defaultFontAsset;
        textMaterial=MinecraftUIText.CreateMaterial(font,8,true);
        Text("Title",transform,16,300,"Loading level",font);
        status=Text("Status",transform,-8,300,"Building terrain",font);
        var bar=Rect("ProgressBar",transform,0,-17,100,2);
        bar.gameObject.AddComponent<Image>().color=new Color(128/255f,128/255f,128/255f,1);
        progressFill=Rect("Progress",bar,-50,0,0,2);progressFill.pivot=new Vector2(0,.5f);
        progressFill.gameObject.AddComponent<Image>().color=new Color(128/255f,1,128/255f,1);
        var cancel = Rect("Cancel", transform, 0, -65, 200, 20);
        RawImage buttonImage=null;
        if(assets!=null&&assets.guiWidgets!=null){buttonImage=cancel.gameObject.AddComponent<RawImage>();buttonImage.texture=assets.guiWidgets;buttonImage.color=Color.white;}
        else cancel.gameObject.AddComponent<Image>().color=new Color(.4f,.4f,.4f,1);
        var button=cancel.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
        button.onClick.AddListener(CancelLoad);
        var label=Text("Label",cancel,0,200,"Cancel",font);
        label.color=new Color(224/255f,224/255f,224/255f,1);
        if(buttonImage!=null)cancel.gameObject.AddComponent<LoadingCancelHover>().Initialize(buttonImage,label);
        UpdateLayout();
    }
    private TextMeshProUGUI Text(string name,Transform parent,float y,float width,string value,TMP_FontAsset font)
    {
        var text=Rect(name,parent,0,y,width,8).gameObject.AddComponent<TextMeshProUGUI>();
        text.font=font;text.fontSize=8;text.text=value;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;
        text.enableWordWrapping=false;text.extraPadding=true;text.raycastTarget=false;
        if(textMaterial!=null)text.fontSharedMaterial=textMaterial;
        return text;
    }
    private void UpdateLayout()
    {
        if(lastWidth==Screen.width&&lastHeight==Screen.height)return;
        lastWidth=Screen.width;lastHeight=Screen.height;
        float scale=Mathf.Max(1,Mathf.FloorToInt(Mathf.Min(Screen.width/320f,Screen.height/240f)));
        GetComponent<CanvasScaler>().scaleFactor=scale;
        if(dirt!=null)dirt.uvRect=new Rect(0,0,Screen.width/scale/32f,Screen.height/scale/32f);
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
        UpdateLayout();
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
        if(mapReady)ReportProgress(!playerReady?"Loading player":!spawnReady?"Building terrain":"Done",!playerReady?.96f:!spawnReady?.98f:1);
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

public sealed class LoadingCancelHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    private RawImage image;
    private TextMeshProUGUI label;
    public void Initialize(RawImage background,TextMeshProUGUI text){image=background;label=text;SetHover(false);}
    private void SetHover(bool hover)
    {
        image.uvRect=new Rect(0,1-((hover?86:66)+20)/256f,200/256f,20/256f);
        label.color=hover?new Color(1,1,160/255f,1):new Color(224/255f,224/255f,224/255f,1);
    }
    public void OnPointerEnter(PointerEventData data){SetHover(true);}
    public void OnPointerExit(PointerEventData data){SetHover(false);}
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
