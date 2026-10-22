using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSettingsManager : MonoBehaviour
{
    public static WorldSettingsManager Instance;
    
    public Vector3Int seedOffset;
    public long betaWorldSeed;
    public string worldName;
    public double worldTimeTicks=1000;
    private FluidTickSave fluidTicks;
    private DroppedItemState[] droppedItems;
    private MobSaveData mobs;
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
            worldTimeTicks=worldSaveData.hasWorldTime?worldSaveData.worldTimeTicks:1000;
            fluidTicks=worldSaveData.fluidTicks;
            droppedItems=worldSaveData.droppedItems;
            mobs=worldSaveData.SavedMobs;
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
            World.Instance.worldTimeTicks=worldTimeTicks;
            World.Instance.RestoreFluidTicks(fluidWorldName==worldName?fluidTicks:null);
            World.Instance.RestoreDroppedItems(fluidWorldName==worldName?droppedItems:null);
            World.Instance.RestoreMobs(fluidWorldName==worldName?mobs:null);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
