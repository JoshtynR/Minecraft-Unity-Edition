using System;
using System.Collections;
using System.IO;
using Mirror;
using Steamworks;
using UnityEngine;

public partial class WorldServer : NetworkBehaviour
{
    public static WorldServer instance;

    private void Start()
    {
        instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeWorldClock();
        NetworkServer.RegisterHandler<SetBlockMessage>(SetBlockMessageHandler, false);
        NetworkServer.RegisterHandler<SavePlayerMessage>(SavePlayerMessageHandler, false);
        NetworkServer.RegisterHandler<SpawnPlayerMessage>(SpawnPlayerMessageHandler, false);
        NetworkServer.RegisterHandler<StartPlayerMessage>(StartPlayerMessageHandler, false);
        instance = this;
        NetworkServer.RegisterHandler<ChestRequest>(ReceiveChestRequest);
        NetworkServer.RegisterHandler<FurnaceRequest>(ReceiveFurnaceRequest);
        NetworkServer.RegisterHandler<DropSyncRequest>((connection,request)=>
        {
            if(connection.identity==null)return;
            var world=connection.identity.GetComponent<Player>().CurrentWorld;
            foreach(var item in world.CaptureDroppedItems())connection.Send(new DropStateMessage{item=item,dimension=world.dimension});
        });

        NetworkServer.RegisterHandler<MobSyncRequest>((connection,request)=>
        {
            if(connection.identity!=null)connection.identity.GetComponent<Player>().CurrentWorld.SendMobSnapshot(connection);
        });

        StartCoroutine(SaveLoop());
    }

    #region Block

    public void SetBlockMessageHandler(NetworkConnectionToClient conn, SetBlockMessage message)
    {
        if(conn.identity==null)return;
        var player=conn.identity.GetComponent<Player>();
        if(player==null||player.DimensionTransferInProgress||message.dimension!=player.dimension)return;
        var world=player.CurrentWorld;
        if(world==null||world.isSaving)return;
        if (message.blockType == BlockType.Air || message.blockType == BlockType.Nothing) return;
        if(WheatCropRules.IsCrop(message.blockType))return;
        if((player.transform.position-((Vector3)message.position+Vector3.one*.5f)).sqrMagnitude>64)return;
        if(!world.CanReplaceChest(message.position,message.blockType))return;
        byte state=BlockSurvivalRules.IsLeaves(message.blockType)?(byte)4:message.stairFacing;
        if(!world.CanPlaceSurvivalBlock(message.position,message.blockType,state))return;
        world.SetBlock(message.position,message.blockType,state);
        BroadcastBlock(world,message.position,message.blockType,state);
        GameAudio.BroadcastBlock(message.blockType,message.position,false,world);
    }
    public void BroadcastBlock(World world,Vector3Int position,BlockType type,byte state)
    {RpcDimensionBlock(world.dimension,position,type,state);}
    // Retained for integrations that explicitly target the visible world.
    public void RpcSetBlock(Vector3Int position,BlockType blockType,byte stairFacing)
    {BroadcastBlock(World.Instance,position,blockType,stairFacing);}
    [ClientRpc] private void RpcDimensionBlock(WorldDimension dimension,Vector3Int position,BlockType type,byte state)
    {if(!NetworkServer.active){var world=World.FindDimension(dimension);if(world!=null)world.SetBlock(position,type,state);}}

    [Server] public void BroadcastWaterUpdates(WorldDimension dimension,Vector3Int[] positions,byte[] levels)
    {RpcWaterUpdates(dimension,positions,levels);}
    [ClientRpc] private void RpcWaterUpdates(WorldDimension dimension,Vector3Int[] positions,byte[] levels)
    {
        if(NetworkServer.active)return;var world=World.FindDimension(dimension);if(world==null)return;
        for(int i=0;i<positions.Length;i++)world.ReceiveWaterState(positions[i],levels[i]);
    }
    public struct SetBlockMessage : NetworkMessage
    {
        public Vector3Int position;public BlockType blockType;public byte stairFacing;public WorldDimension dimension;
        public SetBlockMessage(Vector3Int position,BlockType blockType,byte stairFacing=0)
        {
            this.position=position;this.blockType=blockType;this.stairFacing=(byte)(stairFacing&15);
            this.dimension=World.Instance!=null?World.Instance.dimension:WorldDimension.Overworld;
        }
    }
    
    #endregion
    
    #region SavePlayer
    
    public void SavePlayerMessageHandler(NetworkConnectionToClient conn, SavePlayerMessage message)
    {
        if(conn.identity==null)return;
        var player=conn.identity.GetComponent<Player>();
        if(player!=null)player.CurrentWorld.SavePlayer(player.SavePlayer());
    }
    
    public IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(5*60);
            World.SaveAllDimensions();
        }
    }
    
    [Serializable]
    public struct SavePlayerMessage : NetworkMessage
    {
        public Vector3 position;
        public Quaternion bodyRotation;
        public Quaternion headRotation;
        public PlayerInventory.Slot[] inventory;
        public PlayerInventory.Slot[] crafting;
        public PlayerInventory.Slot cursor;
        public PlayerInventory.Slot[] recovery;
        public PlayerInventory.Slot[] equipment;
        public ulong steamId;
        public WorldDimension dimension;
        public bool hasBedSpawn;
        public Vector3Int bedSpawn;

        public SavePlayerMessage(Vector3 position, Quaternion bodyRotation, Quaternion headRotation, PlayerInventory.Slot[] inventory, ulong steamId,
            PlayerInventory.Slot[] crafting = null, PlayerInventory.Slot cursor = null, PlayerInventory.Slot[] recovery = null, PlayerInventory.Slot[] equipment = null,
            WorldDimension dimension = WorldDimension.Overworld, bool hasBedSpawn = false, Vector3Int bedSpawn = default(Vector3Int))
        {
            this.position = position;
            this.bodyRotation = bodyRotation;
            this.headRotation = headRotation;
            this.inventory = inventory;
            this.crafting = crafting;
            this.cursor = cursor;
            this.recovery = recovery;
            this.equipment = equipment;
            this.steamId = steamId;
            this.dimension = DimensionRules.Valid(dimension);
            this.hasBedSpawn = hasBedSpawn;
            this.bedSpawn = bedSpawn;
        }
    }
    
    #endregion
    
    #region SpawnPlayer
    
    public void SpawnPlayerMessageHandler(NetworkConnectionToClient conn, SpawnPlayerMessage message)
    {
        if (conn.identity != null) return;
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + World.Overworld.worldName + $"/playerdata/{message.steamId}.json")))
        {
            var playerData =  JsonUtility.FromJson<SavePlayerMessage>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                ".minecraftUnity/saves/" + World.Overworld.worldName + $"/playerdata/{message.steamId}.json")));
            
            var player = Instantiate(NetworkManager.singleton.playerPrefab, playerData.position, Quaternion.identity);
            // Included in the initial spawn state: the loading UI must wait even
            // if the saved-player TargetRpc arrives several frames after spawn.
            player.GetComponent<Player>().dimension = DimensionRules.Valid(playerData.dimension);
            player.GetComponent<Player>().expectsSavedPlayerData = true;
            player.GetComponent<Player>().SteamId = message.steamId;
            player.GetComponent<Player>().PrepareDimensionJoin();
            NetworkServer.AddPlayerForConnection(conn, player);
            player.GetComponent<Player>().RpcLoadPlayer(conn, playerData);
            player.GetComponent<Player>().SynchronizeDimensionOnJoin();
            conn.identity.AssignClientAuthority(conn);
        }
        else
        {
            var world = World.Overworld;
            var spawn = WorldSpawnRules.Resolve(world);
            var player = Instantiate(NetworkManager.singleton.playerPrefab, spawn, Quaternion.identity);
            player.GetComponent<Player>().SteamId = message.steamId;
            player.GetComponent<Player>().PrepareDimensionJoin();
            NetworkServer.AddPlayerForConnection(conn, player);
            conn.identity.AssignClientAuthority(conn);
            player.GetComponent<Player>().SynchronizeDimensionOnJoin();
        }
    }
    public struct SpawnPlayerMessage : NetworkMessage
    {
        public SteamId steamId;
        
        public SpawnPlayerMessage(SteamId steamId)
        {
            this.steamId = steamId;
        }
    }


    #endregion
    
    #region StartPlayer
    
    public async void StartPlayerMessageHandler(NetworkConnectionToClient conn, StartPlayerMessage message)
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraftUnity/saves/" + World.Overworld.worldName + $"/playerdata/{message.steamId}.json")))
        {
            var playerData = JsonUtility.FromJson<SavePlayerMessage>(File.ReadAllText(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraftUnity/saves/" + World.Overworld.worldName + $"/playerdata/{message.steamId}.json")));
            
            var destination=World.GetDimension(playerData.dimension);
            await destination.EnsureAreaAsync(Vector3Int.RoundToInt(playerData.position),true);
            await destination.EnsureWorldMetadataAsync();
            conn.Send(new StartWorldMessage(Vector3Int.RoundToInt(playerData.position), destination.betaWorldSeed, destination.dimension));
        }
        else
        {
            await World.Overworld.EnsureAreaAsync(Vector3Int.zero,true);
            await World.Overworld.EnsureWorldMetadataAsync();
            conn.Send(new StartWorldMessage(Vector3Int.zero, World.Overworld.betaWorldSeed));
        }
    }
    
    public struct StartPlayerMessage : NetworkMessage
    {
        public SteamId steamId;
        
        public StartPlayerMessage(SteamId steamId)
        {
            this.steamId = steamId;
        }
    }
    
    #endregion
    
}
