using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;

namespace UnityEngine {
 public class MonoBehaviour {public bool enabled=true;public void Invoke(string name,float delay){}}
 public class HelpURLAttribute:Attribute {public HelpURLAttribute(string value){}}
 public class SerializeField:Attribute {}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string value){}}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string value){}}
 public static class Debug {public static List<string> errors=new List<string>();public static void Log(object o){}public static void LogWarning(object o){errors.Add(o.ToString());}public static void LogError(object o){errors.Add(o.ToString());}public static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}}
}
namespace Mirror {
 public static class Channels {public const int Reliable=0,Unreliable=1;}
 public class NetworkManager {public static NetworkManager singleton=new NetworkManager();public int maxConnections=8;}
}
// Legacy P2P entry points are unchanged; only the SteamSockets implementation is executed.
namespace Mirror.FizzySteam {
 public static class LegacyClient {public static IClient CreateClient(FizzyFacepunch t,string host)=>throw new NotSupportedException();}
 public static class LegacyServer {public static IServer CreateServer(FizzyFacepunch t,int max)=>throw new NotSupportedException();}
}
#if !SDK
namespace Steamworks {
 public enum Result {OK,InvalidParam,NoConnection,InvalidState,LimitExceeded,Ignored,Fail}
 public enum P2PSend {Reliable,Unreliable,UnreliableNoDelay,ReliableWithBuffering}
 public enum ConnectionState {None,Connecting,FindingRoute,Connected,ClosedByPeer,ProblemDetectedLocally}
 public enum NetConnectionEnd {None}
 public struct SteamId {public ulong Value;public static implicit operator SteamId(ulong v)=>new SteamId{Value=v};public static implicit operator ulong(SteamId v)=>v.Value;public override string ToString()=>Value.ToString();}
 public static class SteamClient {public static bool IsValid=true;public static SteamId SteamId=10;public static void Init(uint id,bool async){}}
 public static class SteamNetworkingUtils {public static void InitRelayNetworkAccess(){}}
 public static class SteamNetworking {public static void AllowP2PPacketRelay(bool value){}}
 public class SocketManager {
  public virtual void OnConnecting(Connection c,ConnectionInfo i){c.Accept();}
  public virtual void OnConnected(Connection c,ConnectionInfo i){}
  public virtual void OnDisconnected(Connection c,ConnectionInfo i){c.Close(false,0,"SDK close");}
  public virtual void OnMessage(Connection c,NetIdentity n,IntPtr p,int s,long m,long t,int ch){}
  public bool Close()=>true;public void Receive(int max){}
 }
 public class ConnectionManager {
  public Connection Connection;public bool Connected,Connecting;
  public virtual void OnConnected(ConnectionInfo i){Connected=true;}
  public virtual void OnDisconnected(ConnectionInfo i){Connected=false;}
  public virtual void OnMessage(IntPtr p,int s,long m,long t,int ch){}
  public void Receive(int max){}public void Close(){Connection.Close(false,0,"SDK close");}
 }
 public static class SteamNetworkingSockets {
  public static SocketManager socket;public static ConnectionManager client;public static bool throwConnect;
  public static T CreateRelaySocket<T>() where T:SocketManager,new(){var manager=new T();socket=manager;return manager;}
  public static T ConnectRelay<T>(SteamId id) where T:ConnectionManager,new(){if(throwConnect)throw new Exception("native connect failed");var manager=new T();manager.Connection=new Connection{Id=900};client=manager;return manager;}
 }
}
namespace Steamworks.Data {
 public enum SendType {Reliable,Unreliable}
 public struct NetIdentity {public SteamId SteamId;}
 public struct ConnectionInfo {public ConnectionState State;public NetIdentity Identity;public NetConnectionEnd EndReason;}
 public sealed class Wire {
  public List<byte[]> sent=new List<byte[]>();public List<SendType> flags=new List<SendType>();public Queue<Result> results=new Queue<Result>();public int accepts,closes,flushes;public bool closed;
 }
 public struct Connection {
  public uint Id;public static Dictionary<uint,Wire> wires=new Dictionary<uint,Wire>();
  public Wire Wire {get {if(!wires.ContainsKey(Id))wires[Id]=new Wire();return wires[Id];}}
  public Result Accept(){Wire.accepts++;return Result.OK;}
  public bool Close(bool linger,int reason,string message){Wire.closes++;Wire.closed=true;return true;}
  public Result SendMessage(IntPtr p,int size,SendType mode){var w=Wire;if(w.closed)return Result.NoConnection;if(size>512*1024)return Result.InvalidParam;var result=w.results.Count>0?w.results.Dequeue():Result.OK;if(result==Result.OK){var data=new byte[size];Marshal.Copy(p,data,0,size);w.sent.Add(data);w.flags.Add(mode);}return result;}
  public Result Flush(){Wire.flushes++;return Wire.closed?Result.NoConnection:Result.OK;}
 }
}
#endif
