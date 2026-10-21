using System;using UnityEngine;using Mirror;
namespace UnityEngine {
 public struct Quaternion {public static Quaternion identity=>new Quaternion();}
 public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float seconds){}}
}
namespace Mirror {
 public interface NetworkMessage {}
 public class ServerAttribute:Attribute {}
 public class ClientRpcAttribute:Attribute {}
 public class NetworkBehaviour:MonoBehaviour {
  public virtual void OnStartServer(){}
  public void StartCoroutine(System.Collections.IEnumerator r){}
  public static GameObject Instantiate(GameObject source,Vector3 p,Quaternion q)=>new GameObject("spawned");
 }
 public class NetworkManager {public static NetworkManager singleton=new NetworkManager();public GameObject playerPrefab;}
}
namespace Steamworks {public struct SteamId {public ulong value;public static implicit operator ulong(SteamId id)=>id.value;}}
public struct ChestRequest {}
public struct FurnaceRequest {}
public struct DropSyncRequest {}
public struct CowSyncRequest {}
public partial class WorldServer {
 public void ReceiveChestRequest(NetworkConnectionToClient c,ChestRequest r){}
 public void ReceiveFurnaceRequest(NetworkConnectionToClient c,FurnaceRequest r){}
 public void CloseChestForDisconnect(NetworkConnectionToClient c){}
 public void CloseFurnaceForDisconnect(NetworkConnectionToClient c){}
}

public static class WheatCropRules {public static bool IsCrop(BlockType t)=>false;}
