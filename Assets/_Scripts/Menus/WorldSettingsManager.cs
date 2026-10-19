using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSettingsManager : MonoBehaviour
{
    public static WorldSettingsManager Instance;
    
    public Vector3Int seedOffset;
    public long betaWorldSeed;
    public string worldName;
    private FluidTickSave fluidTicks;
    private string fluidWorldName;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public Task LoadWorldSettings(WorldSaveData worldSaveData)
    {
        return Task.Run(() =>
        {
            worldName = worldSaveData.worldName;
            seedOffset = worldSaveData.seedOffset;
            betaWorldSeed = worldSaveData.betaWorldSeed;
            fluidTicks=worldSaveData.fluidTicks;
            fluidWorldName=worldSaveData.worldName;
        });
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (World.Instance != null)
        {
            World.Instance.mapSeedOffset = seedOffset;
            World.Instance.betaWorldSeed = betaWorldSeed;
            World.Instance.worldName = worldName;
            World.Instance.RestoreFluidTicks(fluidWorldName==worldName?fluidTicks:null);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
