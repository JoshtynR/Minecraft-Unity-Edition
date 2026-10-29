using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISlider : MonoBehaviour
{
    public string prefix;
    public string suffix;
    public bool wholeNumbers;
    
    private Slider slider;
    private TextMeshProUGUI text;
    
    public static readonly Dictionary<string, UISlider> sliders = new();
     
    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
        text = GetComponentInChildren<TextMeshProUGUI>();
        sliders[name] = this;
    }

    private void OnDestroy()
    {
        sliders.Remove(name);
    }

    private void OnEnable()
    {
        // Options panels can start inactive and awaken after settings load.
        if(SettingsManager.instance!=null&&SettingsManager.instance.settings.TryGetValue(name,out var value))
            SetValue(value);
    }

    public void SetValue(float value)
    {
        slider.value = value;
        if (wholeNumbers)
        {
            value = Mathf.Round(value);
        }
        
        if (value == Math.Floor(value))
        {
            text.text = prefix + value + suffix;
        }
        else
        {
            text.text = prefix + value.ToString("0.0") + suffix;
        }
    }
    
    public void OnValueChanged(float value)
    {
        if (wholeNumbers)
        {
            value = Mathf.Round(value);
        }
        if (value == Math.Floor(value))
        {
            text.text = prefix + value + suffix;
        }
        else
        {
            text.text = prefix + value.ToString("0.0") + suffix;
        }
        SettingsManager.instance.SetValue(name, value);
    }
}
