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
    public int lanEnabled;

    public static bool LanEnabled => instance != null
        ? instance.lanEnabled != 0
        : PlayerPrefs.GetInt("lanEnabled", 0) != 0;
    
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
            EnsureLanToggle();
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
        PlayerPrefs.SetFloat("brightness", brightness);
        PlayerPrefs.SetInt("renderDistance", renderDistance);
        PlayerPrefs.SetInt("guiScale", guiScale);
        PlayerPrefs.SetInt("fov", fov);
        PlayerPrefs.SetInt("lanEnabled", lanEnabled);
        
        PlayerPrefs.Save();
        
        SetDictionary();
    }
    
    public void LoadSettings()
    {
        brightness = PlayerPrefs.GetFloat("brightness", 0f);
        renderDistance = PlayerPrefs.GetInt("renderDistance", 4);
        guiScale = PlayerPrefs.GetInt("guiScale", 2);
        fov = PlayerPrefs.GetInt("fov", 90);
        lanEnabled = PlayerPrefs.GetInt("lanEnabled", 0);
        
        SetDictionary();
    }

    public void SetDictionary()
    {
        settings["brightness"] = brightness;
        settings["renderDistance"] = renderDistance;
        settings["guiScale"] = guiScale;
        settings["fov"] = fov;
        settings["lanEnabled"] = lanEnabled;
    }
    
    public void SetValue(string name, float value)
    {
        switch (name)
        {
            case "brightness":
                brightness = value;
                break;
            case "renderDistance":
                renderDistance = (int)value;
                break;
            case "guiScale":
                guiScale = (int)value;
                break;
            case "fov":
                fov = (int)value;
                break;
            case "lanEnabled":
                lanEnabled = (int)value;
                break;
        }
    }
    
    public void ApplySettings()
    {
        SaveSettings();
        if (World.Instance != null) World.Instance.gamma = brightness/10f;
        if (World.Instance != null) World.Instance.renderDistance = renderDistance;
        foreach (var canvas in FindObjectsOfType<CanvasScaler>())
        {
            canvas.scaleFactor = guiScale;
        }
        if (World.Instance != null && Camera.main != null) Camera.main.fieldOfView = fov;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.ExecuteAfterFrames(1, () =>
        {
            EnsureLanToggle();
            foreach (var slider in UISlider.sliders.Values)
                if (settings.TryGetValue(slider.name, out var sliderValue)) slider.SetValue(sliderValue);
            foreach (var toggleButton in UIToggleButton.toggleButtons)
                if (settings.TryGetValue(toggleButton.name, out var toggleValue)) toggleButton.SetValue((int)toggleValue);
            ApplySettings();
        });
    }

    private void EnsureLanToggle()
    {
        UIToggleButton source = null;
        foreach (var toggle in FindObjectsOfType<UIToggleButton>(true))
        {
            if (toggle.name == "lanEnabled")
            {
                toggle.SetValue(lanEnabled);
                return;
            }
            if (toggle.name == "guiScale") source = toggle;
        }
        if (source == null || source.transform.parent == null) return;

        var clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = "lanEnabled";
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        var scaleHandler = clone.GetComponent<guiScaleButton>();
        if (scaleHandler != null) Destroy(scaleHandler);
        var toggleButton = clone.GetComponent<UIToggleButton>();
        toggleButton.visualNames = new[] { "LAN: OFF", "LAN: ON" };
        toggleButton.values = new[] { 0, 1 };
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(toggleButton.NextValue);
        button.onClick.AddListener(SaveSettings);
        toggleButton.SetValue(lanEnabled);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
