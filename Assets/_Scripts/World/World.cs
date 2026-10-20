using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public partial class World : MonoBehaviour
{
    public static World Instance;
    public int renderDistance = 6;
    [Space]
    [Header("Sizes")]
    public byte chunkSize = 16; // 16x16 chunks
    public byte chunkHeight = 16; // 100 blocks high
    [RangeEx(16, 240, 16)]
    public byte worldHeight = 96; // 256 blocks high
    [Space]
    public byte chunksPerFrame = 1;
    public int chunksGenerationPerFrame = 1;
    public WorldRenderer worldRenderer;
    
    public TerrainGenerator terrainGenerator;
    public Vector3Int mapSeedOffset;
    [Tooltip("Exact signed 64-bit Minecraft world seed used by Beta terrain generation.")]
    public long betaWorldSeed;
    
    public bool GenerateMoreChunks = true;
    [Space]
    [Header("Lighting")] 
    [Range(0, 1)]
    public float gamma = 0.0f;
    [Range(0.15f, 1)]
    public float skyLightMultiplier = 0.75f;
    public float blockLightMultiplier = 1.5f;
    [Space]
    public Color dayColor;
    public Color nightColor;
    public string worldName;
    [HideInInspector]
    public bool disabled;
    public bool binaryCompressSaves = true;

    private CancellationTokenSource taskTokenSource = new CancellationTokenSource();
    private CancellationToken worldCancellationToken;

    public UnityEvent OnWorldCreated;
    public UnityEvent OnNewChunksGenerated;
    
    public WorldData worldData;
    public bool IsWorldCreated { get; private set; }
    
    private Stopwatch fullStopwatch = new Stopwatch();
    

    public bool validateDone;
    [HideInInspector] public bool isInPlayMode;


    private void Awake()
    {
        worldCancellationToken = taskTokenSource.Token;
        Instance = this;
        InitializeChests();
        validateDone = false;
        isInPlayMode = Application.isPlaying;
    }

    private void Start()
    {
        OnValidate();
        Clear();
        InitializeDroppedItems();
        NetworkClient.RegisterHandler<FurnaceReply>(message =>
        {
            if(GameManager.Instance!=null&&GameManager.Instance.localPlayer!=null)
                GameManager.Instance.localPlayer.GetComponent<PlayerInventory>().ReceiveFurnaceReply(message);
        });
        NetworkClient.RegisterHandler<ChestReply>(message =>
        {
            if(GameManager.Instance!=null&&GameManager.Instance.localPlayer!=null)
                GameManager.Instance.localPlayer.GetComponent<PlayerInventory>().ReceiveChestReply(message);
        });
        // GenerateWorld();
        
        NetworkClient.RegisterHandler<StartWorldMessage>(message =>
        {
            // Clients must use the server's seed before generating any chunks.
            betaWorldSeed = message.betaWorldSeed;
            if (WorldSettingsManager.Instance != null)
                WorldSettingsManager.Instance.betaWorldSeed = message.betaWorldSeed;
            StartWorld();
            GenerateWorld(message.position);
        });
        
        // NetworkClient.RegisterHandler<WorldServer.ChunkReceiveMessage>(message =>
        // {
        //     Debug.Log("Received chunk");
        //     
        //     var chunkData = ChunkData.Deserialize(message.chunkSaveData);
        //
        //     worldData.chunkDataDict[chunkData.worldPos] = chunkData;
        //
        //     GenerateOnlyMesh();
        // });
    }

    private void OnValidate()
    {
        worldCancellationToken = taskTokenSource.Token;
        Update();
        if (!Application.isPlaying || !validateDone)
        {
            worldData = new WorldData
            {
                chunkDataDict = new Dictionary<Vector3Int, ChunkData>(),
                chunkDict = new Dictionary<Vector3Int, ChunkRenderer>(),
                worldName = worldName,
            };
            Instance = this;
            

            validateDone = true;
        }
        UpadteLightTexture();
    }

    public void UpadteLightTexture()
    {
        UpdateBetaLightingSettings();
        LightTextureCreator.gamma = gamma;
        LightTextureCreator.skyLightMultiplier = skyLightMultiplier;
        LightTextureCreator.blockLightMultiplier = blockLightMultiplier;
        LightTextureCreator.CreateLightTexture();
        Shader.SetGlobalVectorArray("lightColors", LightTextureCreator.lightColors);
    }

    private WorldGenerationData GetPositionsInRenderDistance(Vector3Int playerPos)
    {
        var allChunkPositionsNeeded = WorldDataHelper.GetChunkPositionsInRenderDistance(this, playerPos);
        var allChunkDataPositionsNeeded = WorldDataHelper.GetDataPositionsInRenderDistance(this, playerPos);
        
        var chunkPositionsToCreate = WorldDataHelper.GetPositionsToCreate(worldData, allChunkPositionsNeeded,playerPos);
        var chunkDataPositionsToCreate = WorldDataHelper.GetDataPositionsToCreate(worldData, allChunkDataPositionsNeeded,playerPos);
        
        var chunkPositionsToLoad = WorldDataHelper.GetPositionsToLoad(worldData, allChunkPositionsNeeded,playerPos);
        var chunkDataPositionsToLoad = WorldDataHelper.GetDataPositionsToLoad(worldData, allChunkDataPositionsNeeded,playerPos);
        
        var chunkPositionsToRemove = WorldDataHelper.GetUnneededChunkPositions(worldData, allChunkPositionsNeeded);
        var chunkDataToRemove = WorldDataHelper.GetUnneededDataPositions(worldData, allChunkDataPositionsNeeded);
        

        var data = new WorldGenerationData
        {
            chunkPositionsToCreate = chunkPositionsToCreate,
            chunkDataPositionsToCreate = chunkDataPositionsToCreate,
            chunkPositionsToLoad = chunkPositionsToLoad,
            chunkDataPositionsToLoad = chunkDataPositionsToLoad,
            chunkPositionsToRemove = chunkPositionsToRemove,
            chunkDataToRemove = chunkDataToRemove
        };
        
        return data;
    }
    
    private Task<ConcurrentDictionary<ChunkRenderer, MeshData>> UpdateMeshDataAsync(List<ChunkRenderer> dataToRender, bool markModifiedByPlayer = true)
    {
        var jobs = dataToRender.Distinct().Where(chunk => chunk != null)
            .Select(chunk => new KeyValuePair<ChunkRenderer, ChunkData>(chunk, chunk.ChunkData)).ToArray();
        if (markModifiedByPlayer)
            foreach (var job in jobs) job.Value.modifiedByPlayer = true;
        return Task.Run(() =>
        {
            var dict = new ConcurrentDictionary<ChunkRenderer, MeshData>();
            Parallel.ForEach(jobs, job =>
            {
                worldCancellationToken.ThrowIfCancellationRequested();
                dict.TryAdd(job.Key, job.Value.GetMeshData());
            });
            return dict;
        }, worldCancellationToken);
    }

    // public void UpdateChunk(Vector3Int chunkPos)
    // {
    //     UpdateChunks(new [] {chunkPos});
    // }
    //
    // public async void UpdateChunks(IEnumerable<Vector3Int> chunkPositions)
    // {
    //     var dataToRender = new List<ChunkRenderer>();
    //     foreach (var chunkPos in chunkPositions)
    //     {
    //         worldData.chunkDict.TryGetValue(chunkPos, out var chunk);
    //         if (chunk != null)
    //         {
    //             dataToRender.Add(chunk);
    //         }
    //         else
    //         {
    //             Debug.LogError("Chunk not found at pos: " + chunkPos);
    //         }
    //     }
    //     
    //     var meshData = await UpdateMeshDataAsync(dataToRender);
    //     foreach (var chunkRenderer in dataToRender)
    //     {
    //         meshData.TryGetValue(chunkRenderer, out var data);
    //         chunkRenderer.UpdateChunk(data);
    //     }
    // }

    public void SetBlock(Vector3 pos, BlockType blockType)
    {
        SetBlock(Vector3Int.FloorToInt(pos), blockType);
    }
    
    public void SetBlock(Vector3Int blockPos, BlockType blockType, byte stairFacing = 0)
    {
        var chunkPos = WorldDataHelper.GetChunkPosition(this, blockPos);
        var chunk = WorldDataHelper.GetChunk(this, chunkPos);
        if (chunk == null) return;
        chunk.ModifiedByPlayer = true;
        chunk.ChunkData.modifiedAfterSave = true;
        SetBlock(chunk, blockPos, blockType, stairFacing);
    }

    public void SetBlocks(IEnumerable<Vector3Int> blockPoss, BlockType blockType)
    {
        var chunkPos = WorldDataHelper.GetChunkPosition(this, blockPoss.First());
        var chunk = WorldDataHelper.GetChunk(this, chunkPos);
        SetBlocks(chunk, blockPoss, blockType);
    }

    public async void SetBlocks(ChunkRenderer chunk, IEnumerable<Vector3Int> blockPoss, BlockType blockType, byte stairFacing = 0)
    {
        var neightBourUpdates = new HashSet<ChunkRenderer>();
        
        foreach (var pos in blockPoss)
        {
            if(pos.y<0||pos.y>=worldHeight)continue;
            var previous = GetBlock(pos);
            bool removingChest=previous!=null&&previous.type==BlockType.Chest&&blockType!=BlockType.Chest;
            bool removingFurnace=previous!=null&&IsFurnace(previous.type)&&!IsFurnace(blockType);
            if(!CanReplaceChest(pos,blockType))continue;
            if(previous.type==BlockType.Nothing)continue;
            bool pair = SpecialBlockGeometry.IsDoor(blockType) || blockType == BlockType.Bed;
            // Validate both halves before writing either. Direct calls and remote
            // clients must not overwrite a neighbor when placing a door/bed.
            if (pair && (stairFacing & 8) == 0)
            {
                var partnerPos = pos + SpecialBlockGeometry.PartnerOffset(blockType, stairFacing);
                var partner = GetBlock(partnerPos);
                bool updating = previous.type == blockType && partner != null && partner.type == blockType;
                if (partnerPos.y < 0 || partnerPos.y >= worldHeight || partner == null || (!updating && (partner.type != BlockType.Air || previous.type != BlockType.Air))) continue;
                WorldDataHelper.SetBlock(this, partnerPos, blockType);
                GetBlock(partnerPos).blockShape = SpecialBlockGeometry.Shape(blockType, (byte)(stairFacing|8));
                var partnerChunk = WorldDataHelper.GetChunk(this, WorldDataHelper.GetChunkPosition(this,partnerPos));
                if (partnerChunk != null) { partnerChunk.ChunkData.modifiedAfterSave = true; neightBourUpdates.Add(partnerChunk); }
            }
            if ((SpecialBlockGeometry.IsDoor(previous.type) || previous.type == BlockType.Bed) && previous.type != blockType)
            {
                byte oldState = SpecialBlockGeometry.State(previous.type,previous.blockShape);
                var partnerPos = pos + SpecialBlockGeometry.PartnerOffset(previous.type,oldState);
                var partner = GetBlock(partnerPos);
                if (partner != null && partner.type == previous.type && (SpecialBlockGeometry.State(partner.type,partner.blockShape)^oldState)==8)
                {
                    WorldDataHelper.SetBlock(this,partnerPos,BlockType.Air);
                    var partnerChunk = WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,partnerPos));
                    if (partnerChunk != null) { partnerChunk.ChunkData.modifiedAfterSave = true; neightBourUpdates.Add(partnerChunk); }
                }
            }
            // if (blockType == BlockType.Air)
            // {
            //     WorldDataHelper.SetBlock(chunk.ChunkData.worldRef, pos, Blocks.AIR);
            // }
            // else
            // {
                WorldDataHelper.SetBlock(chunk.ChunkData.worldRef, pos, blockType);
                GetBlock(pos).blockShape = SpecialBlockGeometry.Shape(blockType, stairFacing);
                if(removingChest)chests.Removed(pos);
                if(removingFurnace)RemoveFurnace(pos);
                if(IsFurnace(blockType))TrackFurnace(pos);
                // Track is stateful, unlike purely visual fence/dust joins. Update
                // its saved selection bounds and neighboring slopes before meshing.
                for (int rx=-1;rx<=1;rx++) for(int ry=-1;ry<=1;ry++) for(int rz=-1;rz<=1;rz++)
                {
                    var railPos=pos+new Vector3Int(rx,ry,rz);
                    var rail=GetBlock(railPos);
                    if(rail==null||!SpecialBlockGeometry.IsRail(rail.type))continue;
                    byte state=SpecialBlockGeometry.RailState(rail.type,SpecialBlockGeometry.State(rail.type,rail.blockShape),railPos,p=>GetBlock(p).type);
                    rail.blockShape=SpecialBlockGeometry.Shape(rail.type,state);
                    var railChunk=WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,railPos));
                    if(railChunk!=null){railChunk.ChunkData.modifiedAfterSave=true;neightBourUpdates.Add(railChunk);}
                }
            // }
            
            if (chunk.ChunkData.IsOnEdge(pos))
            {
                List<ChunkData> neighbourChunkData = chunk.ChunkData.GetNeighbourChunk(pos);
                foreach (var neighChunkData in neighbourChunkData)
                {
                    if(neighChunkData == null) continue;
                    
                    ChunkRenderer neighbourChunk = WorldDataHelper.GetChunk(neighChunkData.worldRef, neighChunkData.worldPos);
                    if(neighbourChunk != null)
                    {
                        neightBourUpdates.Add(neighbourChunk);
                    }
                }
            }
        }
        neightBourUpdates.Add(chunk);

        var meshData = await UpdateMeshDataAsync(neightBourUpdates.ToList());
        
        foreach (var chunkRenderer in neightBourUpdates)
        {
            if (chunkRenderer != null && worldData.chunkDict.TryGetValue(chunkRenderer.ChunkData.worldPos, out var current) && current == chunkRenderer)
                chunkRenderer.UpdateChunk(meshData[chunkRenderer]);
        }
    }

    public void SetBlock(ChunkRenderer chunk, Vector3Int blockPos, BlockType blockType, byte stairFacing = 0)
    {
        SetBlocks(chunk, new[] {blockPos}, blockType, stairFacing);
    }
    
    public Vector3Int GetBlockPos(RaycastHit hit)
    {
        var pos = new Vector3(
            GetBlockPosIn(hit.point.x, hit.normal.x),
            GetBlockPosIn(hit.point.y, hit.normal.y),
            GetBlockPosIn(hit.point.z, hit.normal.z)
            );
        
        return Vector3Int.RoundToInt(pos);
    }

    private float GetBlockPosIn(float pos, float normal)
    {
        if (Mathf.Abs(pos % 1) == 0.5f)
        {
            pos -= (normal / 2);
        }
        
        return pos;
    }

    public Block GetBlock(Vector3Int globalPos)
    {
        var chunkPos = Chunk.ChunkPosFromBlockCoords(this, globalPos);

        if (worldData.chunkDataDict.ContainsKey(chunkPos))
        {
            var containerChunk = worldData.chunkDataDict[chunkPos];

            var blockPos = containerChunk.GetLocalBlockCoords(new Vector3Int(globalPos.x, globalPos.y, globalPos.z));
            return containerChunk.GetBlock(blockPos);
        }
        return BlockHelper.NOTHING;
    }

    public Block GetBlock(Vector3 globalPos)
    {
        return GetBlock(Vector3Int.FloorToInt(globalPos));
    }

    public void LoadAdditionalChunks(GameObject localPlayer)
    {
        if (!GenerateMoreChunks) return;
        
        // Debug.Log("Loading additional chunks");
        GenerateWorld(Vector3Int.RoundToInt(localPlayer.transform.position), () =>
        {
            OnNewChunksGenerated?.Invoke();
        });
    }
    
    private void Clear()
    {
        ResetWater();
        ResetDroppedItems();
        furnaceClock=0;
        ResetTreeMeshChanges();
        worldData = new WorldData
        {
            chunkDataDict = new Dictionary<Vector3Int, ChunkData>(),
            chunkDict = new Dictionary<Vector3Int, ChunkRenderer>(),
            worldName = worldName
        };
        worldData.chunkDataDict?.Clear();
        worldData.chunkDict?.Clear();
        worldRenderer.chunkPool?.Clear();
        isSaving = false;
        
        ResetChunkUpdates();
        

        foreach (var chunk in FindObjectsOfType<ChunkRenderer>())
        {
            DestroyImmediate(chunk.gameObject);
        }
    }

    public void OnDisable()
    {
        taskTokenSource.Cancel();
        disabled = true;
        ResetChunkUpdates();
    }

    private void OnDestroy()
    {
        taskTokenSource.Cancel();
        taskTokenSource.Dispose();
        ResetDroppedItems(true);
        if (Instance == this)
        {
            NetworkClient.UnregisterHandler<StartWorldMessage>();
            NetworkClient.UnregisterHandler<ChestReply>();
            NetworkClient.UnregisterHandler<FurnaceReply>();
            NetworkClient.UnregisterHandler<DropStateMessage>();
            Instance = null;
        }
    }

    public struct WorldGenerationData
    {
        public HashSet<Vector3Int> chunkPositionsToCreate;
        public HashSet<Vector3Int> chunkDataPositionsToCreate;
        public HashSet<Vector3Int> chunkPositionsToLoad;
        public HashSet<Vector3Int> chunkDataPositionsToLoad;
        public HashSet<Vector3Int> chunkPositionsToRemove;
        public HashSet<Vector3Int> chunkDataToRemove;
    }



#if UNITY_EDITOR
    [CustomEditor(typeof(World))]
    public class WorldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var world = target as World;
            if (GUILayout.Button("Generate World"))
            {
                world.Clear();
                world.GenerateWorld();
            }

            if (GUILayout.Button("Clear World"))
            {
                world.Clear();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            if (GUILayout.Button("Save World"))
            {
                world.SaveWorld();
            }

            base.OnInspectorGUI();
        }
    }
#endif
}

public struct StartWorldMessage : NetworkMessage
{
    public Vector3Int position;
    public long betaWorldSeed;
    
    public StartWorldMessage(Vector3Int position, long betaWorldSeed)
    {
        this.position = position;
        this.betaWorldSeed = betaWorldSeed;
    }
}

public struct WorldData
{
    public string worldName;
    public Dictionary<Vector3Int, ChunkData> chunkDataDict;
    public Dictionary<Vector3Int, ChunkRenderer> chunkDict;
}
