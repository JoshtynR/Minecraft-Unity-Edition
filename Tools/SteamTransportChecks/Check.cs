using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using Steamworks.Data;

class Probe : NextCommon {
 public Result Send(Connection c,byte[] data,int ch)=>SendSocket(c,data,ch);
 public Result Flush(Connection c)=>FlushSocket(c);
 public void Forget(Connection c)=>ForgetSocket(c);
 public (byte[],int) Read(byte[] data){var ptr=Marshal.AllocHGlobal(Math.Max(1,data.Length));try{Marshal.Copy(data,0,ptr,data.Length);return ProcessMessage(ptr,data.Length);}finally{Marshal.FreeHGlobal(ptr);}}
}
class Check {
 static int checks;
 static void Assert(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static ConnectionInfo State(ConnectionState state,ulong steam=20)=>new ConnectionInfo{State=state,Identity=new NetIdentity{SteamId=steam}};
 static void Background(Action action)=>Task.Run(action).GetAwaiter().GetResult();
 static void Main(){
  var transport=new FizzyFacepunch();
  Assert(transport.GetMaxPacketSize(Channels.Reliable)+1==512*1024,"Steam reliable limit reserves channel framing");
  Assert(transport.GetMaxPacketSize(Channels.Unreliable)+1==1200,"unreliable payload reserves channel framing");
  Assert(transport.GetBatchThreshold(Channels.Reliable)==1199,"ordinary reliable batches don't grow to 1 MiB");
  transport.UseNextGenSteamNetworking=false;Assert(transport.GetMaxPacketSize(0)==1048576&&transport.GetMaxPacketSize(1)==1200,"legacy packet limits preserved");transport.UseNextGenSteamNetworking=true;
  var probe=new Probe();var conn=new Connection{Id=1};var payload=new byte[]{4,5};
  Assert(probe.Send(conn,payload,0)==Result.OK&&conn.Wire.sent[0].SequenceEqual(new byte[]{4,5,0})&&payload.Length==2,"framing appends correct channel without mutating caller");
  Assert(probe.Send(conn,new byte[NextCommon.ReliablePayloadLimit],0)==Result.OK&&conn.Wire.sent.Last().Length==524288,"maximum legal reliable payload reaches Steam without InvalidParam");
  int sent=conn.Wire.sent.Count;
  Assert(probe.Send(conn,new byte[524288],0)==Result.InvalidParam&&conn.Wire.sent.Count==sent,"oversize reliable packets rejected before native send");
  Assert(probe.Send(conn,new byte[1199],1)==Result.OK&&conn.Wire.flags.Last()==SendType.Unreliable,"unreliable flag and boundary correct");
  Assert(probe.Send(conn,new byte[1200],1)==Result.InvalidParam&&probe.Send(conn,payload,9)==Result.InvalidParam,"oversize unreliable and unsupported channel rejected");
  var decoded=probe.Read(new byte[]{4,5,1});Assert(decoded.Item2==1&&decoded.Item1.SequenceEqual(payload),"receive strips framing");
  Assert(probe.Read(new byte[0]).Item1==null&&probe.Read(new byte[]{0}).Item1==null&&probe.Read(new byte[]{1,9}).Item1==null,"empty/truncated/invalid-channel frames are safe");
  var oversizedUnreliable=new byte[1201];oversizedUnreliable[1200]=1;Assert(probe.Read(oversizedUnreliable).Item1==null,"oversize unreliable frame rejected on receive");
  conn=new Connection{Id=2};conn.Wire.results.Enqueue(Result.LimitExceeded);
  Assert(probe.Send(conn,new byte[]{1},0)==Result.OK&&probe.Send(conn,new byte[]{2},0)==Result.OK&&conn.Wire.sent.Count==0,"temporary send pressure queues reliable messages");
  conn.Wire.results.Enqueue(Result.LimitExceeded);Assert(probe.Flush(conn)==Result.OK&&conn.Wire.sent.Count==0,"pressure survives a flush without losing queue");
  probe.Flush(conn);Assert(conn.Wire.sent.Select(p=>p[0]).SequenceEqual(new byte[]{1,2}),"queued batches retry in original order");
  conn=new Connection{Id=3};conn.Wire.results.Enqueue(Result.LimitExceeded);probe.Send(conn,payload,0);probe.Forget(conn);probe.Flush(conn);Assert(conn.Wire.sent.Count==0,"disconnect releases pending reliable batches");
  conn=new Connection{Id=4};conn.Wire.results.Enqueue(Result.LimitExceeded);Result queued=Result.OK;for(int i=0;i<9;i++){queued=probe.Send(conn,new byte[NextCommon.ReliablePayloadLimit],0);if(queued!=Result.OK)break;}Assert(queued==Result.LimitExceeded,"reliable queue is bounded instead of growing without limit");

  var mirror=new HashSet<int>();int connected=0,disconnected=0,errors=0,mainThread=Thread.CurrentThread.ManagedThreadId,callbackThread=0;
  transport.OnServerConnected=id=>{mirror.Add(id);connected++;callbackThread=Thread.CurrentThread.ManagedThreadId;};
  transport.OnServerDisconnected=id=>{mirror.Remove(id);disconnected++;};
  transport.OnServerError=(id,error)=>{Assert(mirror.Contains(id),"send error is delivered before Mirror disconnect");errors++;};
  var server=NextServer.CreateServer(transport,2);var socket=(FizzySocketManager)SteamNetworkingSockets.socket;conn=new Connection{Id=10};
  Background(()=>socket.OnConnecting(conn,State(ConnectionState.Connecting)));
  Assert(conn.Wire.accepts==0,"Steam callback doesn't accept or call Mirror off the Unity thread");server.ReceiveData();socket.OnConnecting(conn,State(ConnectionState.Connecting));server.ReceiveData();Assert(conn.Wire.accepts==1,"connection is accepted exactly once by this listener");
  Background(()=>socket.OnConnected(conn,State(ConnectionState.Connected)));
  Assert(mirror.Count==0,"background connection callback doesn't modify Mirror");server.ReceiveData();
  Assert(mirror.SetEquals(new[]{1})&&callbackThread==mainThread,"connection published on polling thread");
  socket.OnConnected(conn,State(ConnectionState.Connected));server.ReceiveData();Assert(connected==1,"duplicate connected event is ignored");
  conn.Wire.results.Enqueue(Result.InvalidParam);server.Send(1,payload,0);
  Assert(mirror.Count==0&&disconnected==1&&errors==1,"InvalidParam retires transport and Mirror connection together");
  int logCount=UnityEngine.Debug.errors.Count;for(int i=0;i<10;i++)server.Send(1,payload,0);
  Assert(UnityEngine.Debug.errors.Count==logCount&&errors==1,"already-built Mirror batches after disconnect don't cause unknown-connection spam");
  socket.OnDisconnected(conn,State(ConnectionState.ClosedByPeer));server.ReceiveData();Assert(disconnected==1,"late Steam closure cannot double-disconnect");
  conn=new Connection{Id=11};socket.OnConnected(conn,State(ConnectionState.Connected,21));server.ReceiveData();
  Background(()=>socket.OnDisconnected(conn,State(ConnectionState.ProblemDetectedLocally,21)));server.ReceiveData();Assert(mirror.Count==0&&disconnected==2,"local Steam failures retire Mirror connection");
  conn=new Connection{Id=12};socket.OnConnected(conn,State(ConnectionState.Connected,22));server.ReceiveData();
  transport.OnServerDisconnected=id=>{server.Send(id,payload,0);server.Disconnect(id);mirror.Remove(id);disconnected++;};server.Disconnect(3);
  Assert(mirror.Count==0&&disconnected==3,"disconnect callback can reenter transport safely");server.Shutdown();
  socket.OnConnected(new Connection{Id=13},State(ConnectionState.Connected));server.ReceiveData();Assert(connected==3,"late callbacks on stopped server don't create peers");

  server=NextServer.CreateServer(transport,1);socket=(FizzySocketManager)SteamNetworkingSockets.socket;
  var pendingConn=new Connection{Id=30};var extra=new Connection{Id=31};
  socket.OnConnecting(pendingConn,State(ConnectionState.Connecting));socket.OnConnecting(extra,State(ConnectionState.Connecting,21));server.ReceiveData();
  Assert(pendingConn.Wire.accepts==1&&extra.Wire.closed,"pending handshakes count towards listener capacity");
  server.Shutdown();Assert(pendingConn.Wire.closed,"shutdown closes pending as well as established sockets");

  transport=new FizzyFacepunch();int clientConnected=0,clientDisconnected=0;transport.OnClientConnected=()=>{clientConnected++;callbackThread=Thread.CurrentThread.ManagedThreadId;};transport.OnClientDisconnected=()=>clientDisconnected++;
  var client=NextClient.CreateClient(transport,"20");var manager=(FizzyConnectionManager)SteamNetworkingSockets.client;
  Background(()=>manager.OnConnected(State(ConnectionState.Connected)));Assert(!client.Connected&&clientConnected==0,"client connection callback is deferred");client.ReceiveData();Assert(client.Connected&&clientConnected==1&&callbackThread==mainThread,"client connect occurs on polling thread");
  manager.OnConnected(State(ConnectionState.Connected));client.ReceiveData();Assert(clientConnected==1,"duplicate client connected callback ignored");
  manager.Connection.Wire.results.Enqueue(Result.NoConnection);client.Send(payload,0);client.ReceiveData();client.FlushData();Assert(!client.Connected&&client.Error&&clientDisconnected==1,"failed send closes client exactly once without null-manager polling");
  manager.OnDisconnected(State(ConnectionState.ClosedByPeer));client.ReceiveData();Assert(clientDisconnected==1,"late client callback after failure ignored");
  Connection.wires.Remove(900);client=NextClient.CreateClient(transport,"20");manager=(FizzyConnectionManager)SteamNetworkingSockets.client;manager.OnConnected(State(ConnectionState.Connected));client.ReceiveData();
  manager.OnDisconnected(State(ConnectionState.None));client.ReceiveData();Assert(clientDisconnected==2&&!client.Connected,"client handles SDK disconnection with an invalidated handle");
  client=NextClient.CreateClient(transport,"20");manager=(FizzyConnectionManager)SteamNetworkingSockets.client;client.Disconnect();manager.OnConnected(State(ConnectionState.Connected));client.ReceiveData();Assert(clientConnected==2&&clientDisconnected==2,"cancelled connect cannot later notify Mirror");
  transport.OnClientDisconnected=()=>{clientDisconnected++;transport.ClientDisconnect();};transport.ClientConnect("bad address");Assert(transport.ClientActive(),"startup failure waits until client field is assigned");transport.ClientEarlyUpdate();Assert(!transport.ClientActive()&&clientDisconnected==3,"invalid address cleanup cannot resurrect stale client");
  transport.Timeout=1;client=NextClient.CreateClient(transport,"20");Thread.Sleep(1100);client.ReceiveData();client.ReceiveData();Assert(clientDisconnected==4&&client.Error,"timeout notifies once and cancels pending connection");
  SteamNetworkingSockets.throwConnect=true;client=NextClient.CreateClient(transport,"20");client.ReceiveData();client.Disconnect();Assert(clientDisconnected==5&&client.Error,"native initialization failure cleans up once");SteamNetworkingSockets.throwConnect=false;
  transport=new FizzyFacepunch();transport.ServerStart();var unrelated=new FizzyFacepunch();unrelated.Shutdown();Assert(transport.ServerActive(),"destroying a scene-local duplicate cannot stop the persistent transport");transport.Shutdown();
  Console.WriteLine(checks+" production Steam transport checks passed (native Steam and Unity mocked).");
 }
}
