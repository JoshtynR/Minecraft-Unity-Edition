using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;

    public float brightness;
    public int renderDistance;
    public int guiScale;
    public int fov;
    public float musicVolume=100f;
    private World lastSettingsWorld;
    private bool distanceRefreshPending,savePending;
    private float distanceRefreshAt,saveAt;
    
    public readonly Dictionary<string,float> settings = new Dictionary<string, float>();
    
    public void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        instance = this;
    }
    
    public void Start()
    {
        this.ExecuteAfterFrames(1, () =>
        {
            LoadSettings();
            foreach (var slider in UISlider.sliders.Values)
            {
                slider.SetValue(settings[slider.name]);
            }

            foreach (var toggleButton in UIToggleButton.toggleButtons)
            {
                toggleButton.SetValue((int)settings[toggleButton.name]);
            }

            ApplySettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        });
    }
    
    public void SaveSettings()
    {
        savePending=false;
        PlayerPrefs.SetFloat("brightness", brightness);
        PlayerPrefs.SetInt("renderDistance", renderDistance);
        PlayerPrefs.SetInt("guiScale", guiScale);
        PlayerPrefs.SetInt("fov", fov);
        PlayerPrefs.SetFloat("musicVolume",musicVolume);
        
        PlayerPrefs.Save();
        
        SetDictionary();
    }
    
    public void LoadSettings()
    {
        brightness = Mathf.Clamp(PlayerPrefs.GetFloat("brightness", 0f),0,10);
        renderDistance = Mathf.Clamp(PlayerPrefs.GetInt("renderDistance", 4),1,16);
        guiScale = PlayerPrefs.GetInt("guiScale", 2);
        fov = PlayerPrefs.GetInt("fov", 90);
        musicVolume=Mathf.Clamp(PlayerPrefs.GetFloat("musicVolume",100f),0,100);
        
        SetDictionary();
    }

    public void SetDictionary()
    {
        settings["brightness"] = brightness;
        settings["renderDistance"] = renderDistance;
        settings["guiScale"] = guiScale;
        settings["fov"] = fov;
        settings["musicVolume"]=musicVolume;
    }
    
    public void SetValue(string name, float value)
    {
        if(float.IsNaN(value)||float.IsInfinity(value))return;
        if(settings.TryGetValue(name,out var previous)&&previous==value)return;
        switch (name)
        {
            case "musicVolume":
                musicVolume=Mathf.Clamp(value,0,100);
                if(MusicManager.Instance!=null)MusicManager.Instance.SetVolume(musicVolume/100f);
                break;
            case "brightness":
                brightness = Mathf.Clamp(value,0,10);
                ApplyWorldSettings();
                break;
            case "renderDistance":
                renderDistance = Mathf.Clamp(Mathf.RoundToInt(value),1,16);
                ApplyWorldSettings();
                break;
            case "guiScale":
                guiScale = (int)value;
                break;
            case "fov":
                fov = (int)value;
                break;
            default:return;
        }
        SetDictionary();
        savePending=true;saveAt=Time.unscaledTime+.5f;
    }

    private void ApplyToWorld(World world)
    {
        if(world==null)return;
        world.gamma=brightness/10f;
        if(world.renderDistance==renderDistance)return;
        world.renderDistance=renderDistance;
        distanceRefreshPending=true;distanceRefreshAt=Time.unscaledTime+.15f;
    }
    private void ApplyWorldSettings()
    {
        foreach(var world in World.LoadedDimensions)ApplyToWorld(world);
        // Also cover startup before the dimension registry has been populated.
        ApplyToWorld(World.Instance);
    }
    private void Update()
    {
        if(World.Instance!=lastSettingsWorld)
        {
            lastSettingsWorld=World.Instance;
            ApplyToWorld(lastSettingsWorld);
        }
        if(distanceRefreshPending&&Time.unscaledTime>=distanceRefreshAt)
        {
            var world=World.Instance;var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;
            if(world!=null&&world.IsWorldCreated&&world.GenerateMoreChunks&&!world.disabled&&!world.isSaving&&player!=null&&player.CurrentWorld==world&&!player.DimensionTransferInProgress)
            {
                distanceRefreshPending=false;
                world.RequestChunkStreaming(Vector3Int.FloorToInt(player.transform.position),true);
            }
        }
        if(savePending&&Time.unscaledTime>=saveAt)SaveSettings();
    }
    private void OnApplicationPause(bool paused){if(paused&&savePending)SaveSettings();}
    private void OnApplicationQuit(){if(savePending)SaveSettings();}
    
    public void ApplySettings()
    {
        SaveSettings();
        if(MusicManager.Instance!=null)MusicManager.Instance.SetVolume(musicVolume/100f);
        ApplyWorldSettings();
        foreach (var canvas in FindObjectsOfType<CanvasScaler>())
        {
            canvas.scaleFactor = guiScale;
        }
        if (World.Instance != null && Camera.main != null) Camera.main.fieldOfView = fov;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var slider in UISlider.sliders.Values)
            slider.SetValue(settings[slider.name]);
        foreach (var toggleButton in UIToggleButton.toggleButtons)
            toggleButton.SetValue((int)settings[toggleButton.name]);
        ApplySettings();
    }

    private void OnDestroy()
    {
        if(instance==this&&savePending)SaveSettings();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
