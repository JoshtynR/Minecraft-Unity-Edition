using System;
using System.IO;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldListMenuManager : MonoBehaviour
{
    public static WorldListMenuManager instance;
    public GameObject worldPrefab;
    public Transform worldContainer;
    public Menu joiningMenu;
    public Menu worldCreationMenu;
    
    public bool multiplayerMode;

    private void Awake()
    {
        instance = this;
        ResolveReferences();
    }

    [ContextMenu("Repair World Menu References")]
    private void ResolveReferences()
    {
        // Older/local scene copies can lose serialized links. Resolve only
        // missing references, keeping explicitly assigned custom UI intact.
        if(worldContainer==null)
        {
            var scroll=GetComponentInChildren<ScrollRect>(true);
            if(scroll!=null)worldContainer=scroll.content;
            if(worldContainer==null)worldContainer=transform.Find("Scroll View/Viewport/Content");
        }
        var root=transform.parent!=null?transform.parent:transform;
        if(joiningMenu==null)
        {
            var node=root.Find("JoiningMenu");
            if(node!=null)joiningMenu=node.GetComponent<Menu>();
        }
        if(worldCreationMenu==null)
        {
            var node=root.Find("WorldCreationMenu");
            if(node!=null)worldCreationMenu=node.GetComponent<Menu>();
        }
        if(worldPrefab==null)
        {
            var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
            if(assets!=null)worldPrefab=assets.worldListEntryPrefab;
        }
    }

    public async void GetWorlds()
    {
        ResolveReferences();
        if(worldContainer==null||worldPrefab==null)
        {
            Debug.LogError("World list needs its ScrollRect Content and world-entry prefab. Check the WorldListMenu bindings.",this);
            return;
        }
        WorldButton.currentWorldButton=null;
        for (var i = 0; i < worldContainer.childCount; i++)
        {
            Destroy(worldContainer.GetChild(i).gameObject);
        }
        
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves/");
        
        if (!Directory.Exists(path))
        {
            return;
        }
        
        // Failed older builds can leave a folder without a completed world save.
        var worlds = Directory.GetDirectories(path)
            .Where(folder => File.Exists(Path.Combine(folder, "world.json"))).ToList();
        worlds.Sort((x, y) =>
        
            File.GetLastWriteTime(Path.Combine(y, "world.json")).CompareTo(File.GetLastWriteTime(Path.Combine(x, "world.json")))
        );

        foreach (var world in worlds)
        {
            var worldFolderName = Path.GetFileName(world);
            var worldFilePath = Path.Combine(world, "world.json");
            
            WorldSaveData worldSaveData;
            try
            {
                var worldFile = await File.ReadAllTextAsync(worldFilePath);
                if(!worldFile.TrimStart().StartsWith("{"))worldFile=World.Decompress(worldFile);
                worldSaveData = JsonUtility.FromJson<WorldSaveData>(worldFile);
                if(worldSaveData==null)throw new InvalidDataException("World metadata is empty.");
                if(string.IsNullOrWhiteSpace(worldSaveData.worldName))worldSaveData.worldName=worldFolderName;
            }
            catch(Exception error)
            {
                Debug.LogWarning("Could not read world '"+worldFolderName+"': "+error.Message,this);
                continue;
            }
            

            var worldObject = Instantiate(worldPrefab, worldContainer);

            worldObject.GetComponentsInChildren<TextMeshProUGUI>()[0].text = worldSaveData.worldName;
            worldObject.GetComponentsInChildren<TextMeshProUGUI>()[1].text = worldFolderName + " (" + File.GetLastWriteTime(worldFilePath).ToString("dd/MM/yy HH:mm tt") + ")";
            
            worldObject.GetComponent<WorldButton>().onClick += async () =>
            {
                await WorldSettingsManager.Instance.LoadWorldSettings(worldSaveData);
                joiningMenu.Open();
                if (!multiplayerMode)
                {
                    MinecraftNetworkManager.StartLocalWorld();
                }
                else
                {
                    WorldCreationMenuManager.CreateLobby(worldSaveData.worldName, worldSaveData.seedOffset);
                }
            };
        }

    }

    public void OpenCreationMenu()
    {
        ResolveReferences();
        var creation=worldCreationMenu!=null?worldCreationMenu.GetComponent<WorldCreationMenuManager>():null;
        if(creation==null)
        {
            Debug.LogError("Cannot open world creation: WorldCreationMenu needs its Menu and WorldCreationMenuManager components.",this);
            return;
        }
        creation.multiplayerMode = multiplayerMode;
        worldCreationMenu.Open();
    }

    public void SetMultiplayerMode(bool value)
    {
        multiplayerMode = value;
    }

    public void JoinSelectedWorld()
    {
        WorldButton.currentWorldButton.onClick?.Invoke();
    }
    
    public void DeleteSelectedWorld()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves/");
        var worldFolder = Path.Combine(path, WorldButton.currentWorldButton.GetComponentInChildren<TextMeshProUGUI>().text);
        Directory.Delete(worldFolder, true);

        GetWorlds();
    }

    public void EnableJoinButton()
    {
        transform.Find("Layout/Play").GetComponent<UIButton>().Enable();
        transform.Find("Layout/Delete").GetComponent<UIButton>().Enable();
    }

}
