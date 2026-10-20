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
        NetworkServer.RegisterHandler<SetBlockMessage>(SetBlockMessageHandler, false);
        NetworkServer.RegisterHandler<SavePlayerMessage>(SavePlayerMessageHandler, false);
        NetworkServer.RegisterHandler<SpawnPlayerMessage>(SpawnPlayerMessageHandler, false);
        NetworkServer.RegisterHandler<StartPlayerMessage>(StartPlayerMessageHandler, false);
        NetworkServer.RegisterHandler<ChestRequest>(ReceiveChestRequest);
        NetworkServer.RegisterHandler<FurnaceRequest>(ReceiveFurnaceRequest);
        NetworkServer.RegisterHandler<DropSyncRequest>((connection,request)=>
        {
            if(connection.identity==null||World.Instance==null)return;
            foreach(var item in World.Instance.CaptureDroppedItems())connection.Send(new DropStateMessage{item=item});
        });

        StartCoroutine(SaveLoop());
    }

    #region Block

    public void SetBlockMessageHandler(NetworkConnectionToClient conn, SetBlockMessage message)
    {
        // Survival removal is authorized by Player's timed mining commands.
        if (message.blockType == BlockType.Air || message.blockType == BlockType.Nothing) return;
        if(!World.Instance.CanReplaceChest(message.position,message.blockType))return;
        World.Instance.SetBlock(message.position, message.blockType, message.stairFacing);
        RpcSetBlock(message.position, message.blockType, message.stairFacing);
    }

    [ClientRpc]
    public void RpcSetBlock(Vector3Int position, BlockType blockType, byte stairFacing)
    {
        if (!NetworkServer.active) World.Instance.SetBlock(position, blockType, stairFacing);
    }
    [Server]
    public void BroadcastWaterUpdates(Vector3Int[] positions, byte[] levels)
    {
        // Legacy name: levels also encodes lava and fluid hardening products.
        RpcWaterUpdates(positions, levels);
    }

    [ClientRpc]
    private void RpcWaterUpdates(Vector3Int[] positions, byte[] levels)
    {
        if (NetworkServer.active || World.Instance == null) return;
        for (int i = 0; i < positions.Length; i++)
            World.Instance.ReceiveWaterState(positions[i], levels[i]);
    }

    public struct SetBlockMessage : NetworkMessage
    {
        public Vector3Int position;
        public BlockType blockType;
        public byte stairFacing;
        
        public SetBlockMessage(Vector3Int position, BlockType blockType, byte stairFacing = 0)
        {
            this.position = position;
            this.blockType = blockType;
            this.stairFacing = (byte)(stairFacing & 15);
        }
    }
    
    #endregion
    
    #region SavePlayer
    
    public void SavePlayerMessageHandler(NetworkConnectionToClient conn, SavePlayerMessage message)
    {
        World.Instance.SavePlayer(message);
    }
    
    public IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(5*60);
            World.Instance.SaveWorld();
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

        public SavePlayerMessage(Vector3 position, Quaternion bodyRotation, Quaternion headRotation, PlayerInventory.Slot[] inventory, ulong steamId,
            PlayerInventory.Slot[] crafting = null, PlayerInventory.Slot cursor = null, PlayerInventory.Slot[] recovery = null, PlayerInventory.Slot[] equipment = null)
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
        }
    }
    
    #endregion
    
    #region SpawnPlayer
    
    public void SpawnPlayerMessageHandler(NetworkConnectionToClient conn, SpawnPlayerMessage message)
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")))
        {
            var playerData =  JsonUtility.FromJson<SavePlayerMessage>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")));
            
            var player = Instantiate(NetworkManager.singleton.playerPrefab, playerData.position, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
            player.GetComponent<Player>().RpcLoadPlayer(conn, playerData);
            conn.identity.AssignClientAuthority(conn);
        }
        else
        {
            var world = World.Instance;
            for (var i = world.worldHeight - 1; i >= 0; i--)
            {
                if (world.GetBlock(new Vector3Int(8, i, 8)).type != BlockType.Air)
                {
                    var player = Instantiate(NetworkManager.singleton.playerPrefab, new Vector3(world.chunkSize/2+0.5f, i + 1, world.chunkSize/2+0.5f), Quaternion.identity);
                    NetworkServer.AddPlayerForConnection(conn, player);
                    conn.identity.AssignClientAuthority(conn);
                    break;
                }
            }
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
    
    public void StartPlayerMessageHandler(NetworkConnectionToClient conn, StartPlayerMessage message)
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")))
        {
            var playerData = JsonUtility.FromJson<SavePlayerMessage>(File.ReadAllText(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")));
            
            conn.Send(new StartWorldMessage(Vector3Int.RoundToInt(playerData.position), World.Instance.betaWorldSeed));
        }
        else
        {
            conn.Send(new StartWorldMessage(Vector3Int.zero, World.Instance.betaWorldSeed));
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
