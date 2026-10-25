using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldNetworkManager : MonoBehaviour
{
    private void Start()
    {
        if (NetworkClient.active) return;

        PlayerIdentity.TryInitializeSteam(480);
        MinecraftNetworkManager.StartGameHost();

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name.Contains("World")) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}
